using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace SheetScraper;

public static class PdfUtils
{
    public static void CreatePdf(List<string> imagePaths, DirectoryInfo dir, string fileName)
    {
        fileName = string
            .Join("_", fileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.');
        
        var filePath = $"{dir.FullName}\\{fileName}";
        
        Document.Create(container =>
        {
            foreach (var path in imagePaths)
            {
                container.Page(page =>
                {
                    using var fileStream = new FileStream(path, FileMode.Open);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.Content().Container().Image(fileStream);
                });
            }
        }).GeneratePdf(filePath);
    }
}