using DocumentFormat.OpenXml.Packaging;
using System.IO;
using System.Text;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Wordprocessing;

namespace CaseBridge_AI.Services
{
    public static class DocumentExtractionService
    {
        public static string ExtractText(Stream fileStream, string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => ExtractFromPdf(fileStream),
                ".docx" => ExtractFromDocx(fileStream),
                ".txt" or ".csv" => ExtractFromText(fileStream),
                _ => throw new NotSupportedException($"File extension {extension} is not supported for text extraction.")
            };
        }

        private static string ExtractFromPdf(Stream pdfStream)
        {
            var parsingOptions = new ParsingOptions { UseLenientParsing = true };
            using var document = PdfDocument.Open(pdfStream, parsingOptions);
            var textBuilder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }

            return textBuilder.ToString();
        }

        private static string ExtractFromDocx(Stream docxStream)
        {
            using var wordDocument = WordprocessingDocument.Open(docxStream, false);
            var body = wordDocument.MainDocumentPart?.Document.Body;
            
            if (body == null) return string.Empty;

            var textBuilder = new StringBuilder();
            
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                textBuilder.AppendLine(paragraph.InnerText);
            }

            return textBuilder.ToString().Trim();
        }

        private static string ExtractFromText(Stream textStream)
        {
            using var reader = new StreamReader(textStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            return reader.ReadToEnd();
        }
    }
}
