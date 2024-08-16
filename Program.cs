using dotenv.net;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using QuestPDF.Infrastructure;
using SheetScraper;

QuestPDF.Settings.License = LicenseType.Community;

if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.WriteLine("Missing url list. Exiting.");
    return;
}

var vars = ValidateEnv();

var urls = args[0].Trim().Split(';');

var saveDir = Directory.CreateDirectory(vars.OutputDir!);

var driver = new FirefoxDriver(vars.GeckoDriverPath);

try
{
    foreach (var url in urls)
    {
        await GetPdf(url);
    }
}
finally
{
    driver.Quit();
}

return;

async Task GetPdf(string url)
{
    await driver.Navigate().GoToUrlAsync(url);

    var title = new WebDriverWait(driver, TimeSpan.FromSeconds(20))
        .Until(_ =>
            driver
                .FindElement(By.Id("aside-container-unique"))
                .FindElement(By.XPath("./h1"))
                .FindElement(By.XPath("./span"))
                .Text);
    
    if (string.IsNullOrWhiteSpace(title))
    {
        title = url;
    }
    
    var elements = driver
        .FindElement(By.Id("jmuse-scroller-component"))
        .FindElements(By.XPath("./div"))
        .SkipLast(1);

    var filePaths = new List<string>();
    var directory = Directory.CreateTempSubdirectory();

    var skipPage = false;
    
    foreach (var (containerDiv, index) in elements.Select((item, index) => (item, index)))
    {
        driver.ExecuteScript("arguments[0].scrollIntoView(true);", containerDiv);

        string? src;

        try
        {
            src = new WebDriverWait(driver, TimeSpan.FromSeconds(20))
                .Until(_ =>
                {
                    var srcStr = containerDiv.FindElement(By.XPath("./img")).GetDomProperty("src");
                    return string.IsNullOrWhiteSpace(srcStr) ? null : srcStr;
                });

            if (string.IsNullOrWhiteSpace(src))
            {
                throw new Exception();
            }
        }
        catch (Exception)
        {
            skipPage = true;
            Console.WriteLine($"Encountered an error while trying to find src for page {index} of {url}, skipping.");
            break;
        }

        try
        {
            filePaths.Add(await Downloader.DownloadFileAsync(directory, $"{index}.png", src));
        }
        catch (Exception)
        {
            skipPage = true;
            Console.WriteLine($"Encountered an error while trying to download page {index} of {url}, skipping.");
            break;
        }
    }

    if (skipPage)
    {
        directory.Delete(true);
        return;
    }

    PdfUtils.CreatePdf(filePaths, saveDir, $"{title}.pdf");
    
    directory.Delete(true);
}

Vars ValidateEnv()
{
    var envVars = DotEnv.Read(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 4));

    if (envVars.TryGetValue("GECKO_PATH", out var geckoPath))
    {
        if (string.IsNullOrWhiteSpace(geckoPath))
        {
            throw new Exception("Missing path to gecko driver.");
        }
    }
    else
    {
        throw new Exception("Missing path to gecko driver.");
    }

    if (envVars.TryGetValue("OUTPUT_DIR", out var outputDir))
    {
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            throw new Exception("Missing output directory.");
        }

        if (outputDir.LastIndexOf('\\') == outputDir.Length - 1)
        {
            outputDir = outputDir[..^1];
        }
    }
    else
    {
        throw new Exception("Missing output directory.");
    }

    return new Vars
    {
        OutputDir = outputDir,
        GeckoDriverPath = geckoPath
    };
}

