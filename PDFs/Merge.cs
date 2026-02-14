using File = Dotcore.FileSystem.File;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace HpAutoscan.PDFs;

public static class Merge
{
    public static void Pdfs(IEnumerable<File.Info> pdfs, File.Info destination)
    {
        using var destinationDocument = new PdfDocument();

        foreach (var pdf in pdfs)
        {
            using var source = PdfReader.Open(pdf.Path, PdfDocumentOpenMode.Import);
            CopyPages(source, destinationDocument);
        }

        destinationDocument.Save(destination.Path);
    }

    private static void CopyPages(PdfDocument from, PdfDocument to)
    {
        for (int i = 0; i < from.PageCount; i++)
        {
            to.AddPage(from.Pages[i]);
        }
    }
}