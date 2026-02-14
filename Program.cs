using Dotcore.FileSystem;
using Dotcore.FileSystem.Directory;
using HpScan;
using HpScan.Scanner;
using HpScan.Upload;
using Sharprompt;

var workingDirectory = Prompt.Input<string>("Enter working directory", defaultValue: "./").ToDirectoryInfo();
var printerIp = Prompt.Input<string>("Enter printer IP address", defaultValue: "10.11.100.10");
var quality = Prompt.Input<int>("Enter scan quality (0: lowest, x: highest)", defaultValue: 3);
var shouldUpload = Prompt.Confirm("Upload to paperless?", defaultValue: true);
Paperless.Config? paperlessConfig = null;
if (shouldUpload){
  var paperlessIp = Prompt.Input<string>("Enter paperless IP address", defaultValue: "10.11.20.125");
  var paperlessUserName = Prompt.Input<string>("Enter paperless FTP user name", defaultValue: "ftpuser");
  var paperlessPassword = Prompt.Password("Enter paperless FTP password");
  paperlessConfig = new Paperless.Config(paperlessIp, new Credential(paperlessUserName, paperlessPassword));
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
        () => Prompt.Confirm("Scan another page?", defaultValue: true))
    .ToListAsync();

    var mergedFile = documentWorkingDirectory.CombineFile($"{documentName}.pdf");
    PDFMerge.Merge(pages, mergedFile);

    if (!shouldUpload || paperlessConfig == null) continue;

    await Paperless.UploadDocument(paperlessConfig, mergedFile);
    //documentWorkingDirectory.Delete();
    Console.WriteLine($"document {documentName} uploaded successfully");
}
