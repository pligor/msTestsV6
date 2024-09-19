using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using UglyToad.PdfPig;

namespace MyTests.test_files;

[TestClass]
public class PdfContentTests
{
    [TestMethod]
    [TestCategory("wip")]
    public async Task VerifyPdfContentAsync()
    {
        // URL of the PDF file
        string pdfUrl = "https://dagrs.berkeley.edu/sites/default/files/2020-01/sample.pdf";

        // Download the PDF and extract text
        string pdfText = await DownloadAndExtractPdfTextAsync(pdfUrl);

        // string lowerPdfText = pdfText.ToLower();

        Console.WriteLine(pdfText);

        // Assertions to verify the PDF content
        Assert.IsTrue(pdfText.Contains("Sample PDF Document"), "Expected text not found in PDF.");
        Assert.IsTrue(pdfText.Contains("Contents"), "Expected text not found in PDF.");
        Assert.IsTrue(pdfText.Contains("Chapter 1"), "Expected text not found in PDF.");
        Assert.IsTrue(pdfText.Contains("CHAPTER 1. TEMPLATE"), "Expected text not found in PDF.");
    }

    private static async Task<string> DownloadAndExtractPdfTextAsync(string url)
    {
        using HttpClient httpClient = new();
        // Download PDF data as byte array
        byte[] pdfBytes = await httpClient.GetByteArrayAsync(url);

        using MemoryStream memoryStream = new(pdfBytes);
        // Open the PDF document
        using PdfDocument pdfDocument = PdfDocument.Open(memoryStream);
        // Extract text from all pages
        string extractedText = string.Empty;
        foreach (var page in pdfDocument.GetPages()) {
          var words = page.GetWords();

          extractedText += string.Join(" ", words.Select(word => word.Text)) + "\n";
          // Console.WriteLine("Words on page {0}: {1}", page.Number, words.ToList().Count);
          // extractedText += words.Aggregate(string.Empty, (current, word) => current + word.Text + " ") + "\n";
        }
        return extractedText;
    }
}
