using System.Configuration;
using System.Reflection;
using PuppeteerSharp;


namespace ScamIvan.TradingViewOpertors
{

    /// <summary>
    /// Класс для работы с TradingView через Selenium и Firefox
    /// </summary>
    public class BrowserWorker
    {
        private readonly string _pathToScreenshot = string.Empty;
        private readonly string _urlTimeInterval = string.Empty;
        private readonly string baseAdress;
        private readonly string addUrl;
        private readonly string location;
        private readonly LaunchOptions launchOptions;
        private readonly ViewPortOptions pageOptions;

        /// <summary>
        /// Конструктор, прописывающий путь до места сохранения скриншота
        /// </summary>
        /// <param Name="pathToScreenshot">Путь до скриншота</param>
        /// <param Name="screenshotName">Имя м расширение скриншота</param>
        public BrowserWorker(string pathToScreenshot, string screenshotName)
        {
            _pathToScreenshot = Path.Combine(pathToScreenshot, screenshotName);
            _urlTimeInterval = $"&interval={ConfigurationManager.AppSettings["screenshotTimeInterval"]}";
            baseAdress = ConfigurationManager.AppSettings["screenshotAddress"];
            addUrl = ConfigurationManager.AppSettings["screenshotChart"];
            location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            launchOptions = new LaunchOptions
            {
                Headless = true, // = false for testing
                //Args = ["--start-maximized"]
            };
            pageOptions = new ViewPortOptions
            {
                Width = 1920,
                Height = 1080
            };

            if (!Path.Exists(pathToScreenshot))
            {
                Console.WriteLine($"Папка {_pathToScreenshot} не найдена." +
                $"Создаю такую папку...");

                Directory.CreateDirectory(pathToScreenshot);
            }
        }

        /// <summary>
        /// Открывает браузер без окна, делает скриншот графика и сохраняет
        /// </summary>
        public async Task MakeSkreenshot(string tradePair)
        {
            const string tradingElementClassName = "layout__area--center";
            if (!Path.Exists(Path.Combine($"{location}", "ChromeHeadlessShell")))
            {
                Console.WriteLine($"Не найден хром по пути:{Path.Combine($"{location}", "ChromeHeadlessShell")}\n" +
                $"Пытаюсь скачать браузер:");

                PuppeteerSharp.BrowserData.InstalledBrowser browser = await new BrowserFetcher().DownloadAsync();
                Console.WriteLine($"Браузер: {browser.Browser}\n" +
                $"Платформа:{browser.Platform}\n" +
                $"Билд№:{browser.BuildId}");
            }

            using (var browser = await Puppeteer.LaunchAsync(launchOptions))
            {
                using (var page = await browser.NewPageAsync())
                {
                    await page.SetViewportAsync(pageOptions);
                    await page.GoToAsync($"{baseAdress}{addUrl}{tradePair}{_urlTimeInterval}", WaitUntilNavigation.DOMContentLoaded);
                    var tradingGraph = await page.WaitForSelectorAsync($".{tradingElementClassName}");

                    Console.WriteLine($"Готовлю скриншот в {_pathToScreenshot}");
                    await tradingGraph.ScreenshotAsync(_pathToScreenshot, new ElementScreenshotOptions { OptimizeForSpeed = true, Type = ScreenshotType.Png });
                    await page.CloseAsync();
                }
                await browser.CloseAsync();
                Console.WriteLine($"Скриншот графика сохранён в {_pathToScreenshot}");
            }
        }
    }
}