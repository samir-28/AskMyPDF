using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace PdfChatApp.Services;

public interface IPdfProcessingService
{
    Task<(string content, int pageCount)> ExtractTextFromPdfAsync(Stream pdfStream);
}

public class PdfProcessingService : IPdfProcessingService
{
    private readonly ILogger<PdfProcessingService> _logger;

    public PdfProcessingService(ILogger<PdfProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task<(string content, int pageCount)> ExtractTextFromPdfAsync(Stream pdfStream)
    {
        try
        {
            await using var memoryStream = new MemoryStream();
            await pdfStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var pdfReader = new PdfReader(memoryStream);
            using var pdfDocument = new PdfDocument(pdfReader);

            var pageCount = pdfDocument.GetNumberOfPages();
            var textBuilder = new System.Text.StringBuilder();

            for (int i = 1; i <= pageCount; i++)
            {
                var page = pdfDocument.GetPage(i);
                var strategy = new SimpleTextExtractionStrategy();
                var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);

                textBuilder.AppendLine($"\n--- Page {i} ---\n");
                textBuilder.AppendLine(pageText);
            }

            var extractedText = textBuilder.ToString();

        
            extractedText = CleanExtractedText(extractedText);

            _logger.LogInformation("Successfully extracted text from PDF. Pages: {PageCount}, Characters: {CharCount}",
                pageCount, extractedText.Length);

            return (extractedText, pageCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF");
            throw new InvalidOperationException("Failed to extract text from PDF. Please ensure the file is a valid PDF.", ex);
        }
    }

    private string CleanExtractedText(string text)
    {
       
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

        text = System.Text.RegularExpressions.Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");

        return text.Trim();
    }
}