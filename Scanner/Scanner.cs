using System.Net;
using System.Xml;
using Dotcore.FileSystem;
using File = Dotcore.FileSystem.File;
using Directory = Dotcore.FileSystem.Directory;

namespace ESCLScan.Scanner;

public static class Scanner
{
    public record Config(string Ip, int Quality);
    private static DateTime? LastScan = null;

    public static async IAsyncEnumerable<File.Info> ScanMultiplePages(Config config, Directory.Info destination, Func<bool> shouldScanAnotherPage)
    {
        var currentPage = 0;
        while(currentPage == 0 || shouldScanAnotherPage())
        {
            var currentFile = destination.CombineFile($"page_{currentPage}.pdf");
            currentPage++;
            await Scan(config, currentFile);
            yield return currentFile;
        }
    }

    public static async Task Scan(Config config, File.Info destination) 
    {
        var retry = 0;
        while (retry < 100)
        {
            try
            {
                await DoScan(config.Ip, config.Quality, destination.Path);
                LastScan = DateTime.Now;
                break;
            }
            catch (WebException webException)
            {
                var statusCode = (webException.Response as HttpWebResponse)?.StatusCode;
                if (statusCode != HttpStatusCode.ServiceUnavailable) throw;
                if (LastScan == null) throw;
                if (LastScan + TimeSpan.FromSeconds(20) < DateTime.Now) throw;
                Console.Write($"\rWaiting for scanner service to be available                                              ");
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }
        if(retry == 100) throw new WebException("Scanner service unavailable (504)");
    }

    private static async Task DoScan(
        string printerUrl,
        int quality,
        string fileName)
    {
        var scanRequest = await GetScanRequest(
                printerUrl,
                Path.GetExtension(fileName),
                quality);
        using var response1 = WebRequests.SendXMLPOSTRequest($"http://{printerUrl}:8080/eSCL/ScanJobs", scanRequest);
        Console.Write($"\rScanning file {fileName}                                                   ");

        var responseLocation = response1.Headers["Location"] ?? throw new ArgumentNullException();

        var jobGuid = Path.GetFileName(responseLocation);
        var scannedDocumentUrl = responseLocation + "/NextDocument";

        await Task.Delay(TimeSpan.FromSeconds(2));


        await WaitUntilScanReady(printerUrl, jobGuid);

        using var documentResponse = WebRequests.SendRequest(scannedDocumentUrl, "GET");

        using Stream output = System.IO.File.OpenWrite(fileName);
        using Stream input = documentResponse.GetResponseStream();
        input.CopyTo(output);
    }

    private static Task<string> GetScanRequest(
        string printerUrl,
        string fileExtension,
        int quality) => Task.Run(() =>
    {
        var xmlCapabilities = WebRequests.SendXMLGETRequest($"http://{printerUrl}:8080/eSCL/ScannerCapabilities");

        var ns = new XmlNamespaceManager(xmlCapabilities.NameTable);
        ns.AddNamespace("scan", "http://schemas.hp.com/imaging/escl/2011/05/03");

        var maxWidth = xmlCapabilities.SelectSingleNode("//scan:ScannerCapabilities/scan:Platen/scan:PlatenInputCaps/scan:MaxWidth", ns)!.InnerText;
        var maxHeight = xmlCapabilities.SelectSingleNode("//scan:ScannerCapabilities/scan:Platen/scan:PlatenInputCaps/scan:MaxHeight", ns)!.InnerText;

        var scanXResolutionslist = new List<int>();
        var scanYResolutionslist = new List<int>();
        foreach (var node in xmlCapabilities.SelectNodes("//scan:ScannerCapabilities/scan:Platen/scan:PlatenInputCaps/scan:SettingProfiles/scan:SettingProfile/scan:SupportedResolutions/scan:DiscreteResolutions/scan:DiscreteResolution/scan:XResolution", ns)!)
        {
            scanXResolutionslist.Add(int.Parse(((XmlNode)node).InnerText));
        }
        foreach (var node in xmlCapabilities.SelectNodes("//scan:ScannerCapabilities/scan:Platen/scan:PlatenInputCaps/scan:SettingProfiles/scan:SettingProfile/scan:SupportedResolutions/scan:DiscreteResolutions/scan:DiscreteResolution/scan:YResolution", ns)!)
        {
            scanYResolutionslist.Add(int.Parse(((XmlNode)node).InnerText));
        }
        scanXResolutionslist.Sort();
        scanYResolutionslist.Sort();

        var maxXScanRes = scanXResolutionslist.Skip(quality).First();
        var maxYScanRes = scanYResolutionslist.Skip(quality).First();

        var documentFormatExt = fileExtension == ".pdf" ? "application/pdf" : "image/jpeg";


        return $@"<?xml version='1.0' encoding='utf-8'?>
            			<escl:ScanSettings xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:pwg=""http://www.pwg.org/schemas/2010/12/sm"" xmlns:escl=""http://schemas.hp.com/imaging/escl/2011/05/03"">
                            <pwg:Version>2.63</pwg:Version>
            				<pwg:ScanRegions pwg:MustHonor=""false"">
            					<pwg:ScanRegion>
            						<pwg:ContentRegionUnits>escl:ThreeHundredthsOfInches</pwg:ContentRegionUnits>
            						<pwg:XOffset>0</pwg:XOffset>
            						<pwg:YOffset>0</pwg:YOffset>
                                    <pwg:Width>{maxWidth}</pwg:Width>
                                    <pwg:Height>{maxHeight}</pwg:Height>
            					</pwg:ScanRegion>
            				</pwg:ScanRegions>
            				<escl:DocumentFormatExt>{documentFormatExt}</escl:DocumentFormatExt>
            				<pwg:InputSource>Platen</pwg:InputSource>
            				<escl:XResolution>{maxXScanRes}</escl:XResolution>
            				<escl:YResolution>{maxYScanRes}</escl:YResolution>
            				<escl:ColorMode>RGB24</escl:ColorMode>
            			</escl:ScanSettings>";
    });

    private static async Task WaitUntilScanReady(string printerUrl, string jobGuid)
    {
        var xmlScannerStatusUrl = $"http://{printerUrl}:8080/eSCL/ScannerStatus";
        var scanIsCompleted = false;

        do
        {
            var xmlScannerStatus = WebRequests.SendXMLGETRequest(xmlScannerStatusUrl);

            var ns2 = new XmlNamespaceManager(xmlScannerStatus.NameTable);
            ns2.AddNamespace("scan", "http://schemas.hp.com/imaging/escl/2011/05/03");
            ns2.AddNamespace("pwg", "http://www.pwg.org/schemas/2010/12/sm");

            var scanJobs = xmlScannerStatus.SelectNodes("//scan:ScannerStatus/scan:Jobs/scan:JobInfo", ns2) ?? throw new NullReferenceException("scan jobs null");
            

            foreach (XmlNode jobInfoNode in scanJobs)
            {
                var scanJobUriNode = jobInfoNode.SelectSingleNode("pwg:JobUri", ns2) ?? throw new NullReferenceException();
                var scanJobGuid = Path.GetFileName(scanJobUriNode.InnerText);

                if (scanJobGuid.Equals(jobGuid))
                {
                    var imagesToTransferNode = jobInfoNode.SelectSingleNode("pwg:ImagesToTransfer", ns2) ?? throw new NullReferenceException();
                    if (imagesToTransferNode.InnerText == "1")
                    {
                        scanIsCompleted = true;
                    }
                    else
                    {
                        Console.Write(".");
                    }
                }
            }

            if (!scanIsCompleted) await Task.Delay(TimeSpan.FromMilliseconds(300));
        } while (!scanIsCompleted);
    }
}
