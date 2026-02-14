using Dotcore.FileSystem;
using Dotcore.FileSystem.Directory;
using ESCLScan;
using ESCLScan.Scanner;
using ESCLScan.Upload;
using Sharprompt;

var workingDirectory = Prompt.Input<string>("Enter working directory", defaultValue: "./").ToDirectoryInfo();
var printerIp = Prompt.Input<string>("Enter printer IP address", defaultValue: "192.168.1.100");
var quality = Prompt.Input<int>("Enter scan quality (0: lowest, x: highest)", defaultValue: 3);
var shouldUpload = Prompt.Confirm("Upload to FTP Server?", defaultValue: true);
Uploader.Config? uploadConfig = null;
if (shouldUpload){
  var ftpServerIp = Prompt.Input<string>("Enter FTP Server IP address", defaultValue: "192.168.1.200");
  var ftpServerUserName = Prompt.Input<string>("Enter FTP Server FTP user name", defaultValue: "ftpuser");
  var ftpServerPassword = Prompt.Password("Enter FTP Server FTP password");
  uploadConfig = new Uploader.Config(ftpServerIp, new Credential(ftpServerUserName, ftpServerPassword));
}

var scannerConfig = new Scanner.Config(printerIp, quality);

while (true)
{
    Console.WriteLine("ESCLScan - Main menu");
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

    if (!shouldUpload || uploadConfig == null) continue;

    await Uploader.UploadDocument(uploadConfig, mergedFile);
    //documentWorkingDirectory.Delete();
    Console.WriteLine($"document {documentName} uploaded successfully");
}
