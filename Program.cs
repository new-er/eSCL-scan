using Dotcore.FileSystem;
using Dotcore.FileSystem.Directory;
using HpAutoscan;
using HpAutoscan.PDFs;
using Sharprompt;

var workingDirectory = Prompt.Input<string>("Enter working directory", defaultValue: "./").ToDirectoryInfo();
var printerIp = Prompt.Input<string>("Enter printer IP address", defaultValue: "10.11.100.10");
var quality = Prompt.Input<int>("Enter scan quality (0: lowest, x: highest)", defaultValue: 3);
var shouldUpload = Prompt.Confirm("Upload to paperless?", defaultValue: true);
var paperlessIp = "";
var paperlessUserName = "";
var paperlessPassword = "";
Paperless.Paperless.Config? paperlessConfig = null;
if (shouldUpload){
  paperlessIp = Prompt.Input<string>("Enter paperless IP address", defaultValue: "10.11.20.125");
  paperlessUserName = Prompt.Input<string>("Enter paperless FTP user name", defaultValue: "ftpuser");
  paperlessPassword = Prompt.Password("Enter paperless FTP password");
  paperlessConfig = new Paperless.Paperless.Config(paperlessIp, new Paperless.Credential(paperlessUserName, paperlessPassword));
}

var scannerConfig = new Scanner.Config(printerIp, quality);

while (true)
{
    Console.WriteLine("HPAutoscan - Main menu");
    var mainMenuAction = Prompt.Select("Select action", new[] { "Scan document", "Quit" });
    if(mainMenuAction == "Quit") break;
    
    var documentName = Prompt.Input<string>("Enter document name");
    var documentWorkingDirectory = workingDirectory.CombineDirectory(documentName);
    documentWorkingDirectory.EnsureExists();

    var pages = await Scanner.ScanMultiplePages(
        scannerConfig,
        documentWorkingDirectory,
        () =>
        {
            return Prompt.Confirm("Scan another page?", defaultValue: true);
        })
    .ToListAsync();

    var mergedFile = documentWorkingDirectory.CombineFile($"{documentName}.pdf");
    Merge.Pdfs(pages, mergedFile);

    if (!shouldUpload || paperlessConfig == null) continue;

    await Paperless.Paperless.UploadDocument(paperlessConfig, mergedFile);
    //documentWorkingDirectory.Delete();
    Console.WriteLine($"document {documentName} uploaded successfully");
}
