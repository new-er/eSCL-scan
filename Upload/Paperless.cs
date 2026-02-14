using FluentFTP;

namespace HpScan.Upload;

public static class Paperless
{
    public record Config(string Ip, Credential Credential);

    public static async Task UploadDocument(Config config, FileInfo document)
    {
        var ftpClient = new AsyncFtpClient(config.Ip, config.Credential.UserName, config.Credential.Password);
        ftpClient.ValidateCertificate += Client_ValidateCertificate;
        var _ = await ftpClient.AutoConnect();
        var status = await ftpClient.UploadFile(document.FullName, $"/{document.Name}");
        if (status != FtpStatus.Success) throw new Exception($"ftp status '{status}' for document {document.FullName}");
    }

    private static void Client_ValidateCertificate(FluentFTP.Client.BaseClient.BaseFtpClient control, FtpSslValidationEventArgs e)
    {
        if (e.PolicyErrors == System.Net.Security.SslPolicyErrors.None)
        {
            e.Accept = true;
        }
        else
        {
            // add logic to test if certificate is valid here
            // lookup the "Certificate" and "Chain" properties
            if (e.Certificate.Subject == "CN=localhost") e.Accept = true;
            else e.Accept = false;
        }
    }

}
