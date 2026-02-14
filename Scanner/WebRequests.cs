using System.Net;
using System.Text;
using System.Xml;

namespace HpScan.Scanner;

internal static class WebRequests
{
    public static XmlDocument SendXMLGETRequest(string url)
    {
        using var response = SendRequest(url, "GET");
        using var reader = new StreamReader(response.GetResponseStream());
        var xml = new XmlDocument();
        xml.LoadXml(reader.ReadToEnd());
        return xml!;
    }
    public static HttpWebResponse SendXMLPOSTRequest(string url, string xml) => SendRequest(url, "POST", xml);
    public static HttpWebResponse SendRequest(string url, string method, string? postRequest = null)
    {
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = method;
        request.ContentType = "text/xml";
        request.ContentLength = postRequest == null ? 0 : postRequest.Length;

        if (postRequest != null)
        {
            request.ContentType = "text/xml";

            var postData = Encoding.ASCII.GetBytes(postRequest);
            using var stream = request.GetRequestStream();
            stream.Write(postData, 0, postRequest.Length);
            //Console.WriteLine(postRequest);
        }

        return (HttpWebResponse)request.GetResponse();
    }
}
