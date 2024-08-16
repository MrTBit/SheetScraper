using System.Drawing.Imaging;
using Svg;

namespace SheetScraper;

public static class Downloader
{
    public static async Task<string> DownloadFileAsync(DirectoryInfo directory, string fileName, string url)
    {
        var fullPath = directory.FullName + "\\" + fileName;
        
        using var client = new HttpClient();
        using var httpResponse = await client.GetAsync(url).ConfigureAwait(false);

        if ((int)httpResponse.StatusCode < 200 || (int)httpResponse.StatusCode > 299)
        {
            throw new Exception("Got bad status code: " + (int)httpResponse.StatusCode);
        }
        
        await using var stream = await httpResponse.Content.ReadAsStreamAsync();
        
        if (httpResponse.Content.Headers.ContentType?.MediaType?.Contains("svg") ?? false)
        {
            SaveSvgAsPng(stream, fullPath);
        }
        else
        {
            await using var fileStream = new FileStream(fullPath, FileMode.OpenOrCreate);
            await stream.CopyToAsync(fileStream);
        }

        return fullPath;
    }

    private static void SaveSvgAsPng(Stream stream, string filePath)
    {
        var svgDoc = SvgDocument.Open<SvgDocument>(stream);
        var bitmap = svgDoc.Draw(2480, 3508);
        bitmap.Save(filePath, ImageFormat.Png);
    }
}