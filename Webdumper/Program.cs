using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace WebDumper
{
    public class Config
    {
        public string Url { get; }
        public string OutputFolder { get; }
        public List<string> AssetTypes { get; }

        public Config(string url, string outputFolder, List<string> assetTypes)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            OutputFolder = outputFolder ?? throw new ArgumentNullException(nameof(outputFolder));
            AssetTypes = assetTypes ?? throw new ArgumentNullException(nameof(assetTypes));
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                PrintBanner();

                if (args.Length < 1)
                {
                    Console.WriteLine("Usage: WebDumper <URL> [Depth]");
                    return;
                }

                string url = args[0];
                int depth = args.Length > 1 ? int.Parse(args[1]) : 1;

                var config = new Config(url, "dumps", new List<string> { "img", "link", "script" });

                var dumper = new WebsiteDumper(config);
                await dumper.SWS();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void PrintBanner()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("   WebDumper - Website Scraper  ");
            Console.WriteLine("   Dev : rbp               ");
            Console.WriteLine("=========================================");
        }
    }

    public class WebsiteDumper
    {
        private readonly Config _config;
        private readonly HashSet<string> _visited;
        private readonly object _lock = new object();
        private int _totalItems = 100;
        private int _progressCount = 0;

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        static WebsiteDumper()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public WebsiteDumper(Config config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _visited = new HashSet<string>();
        }

        public async Task SWS()
        {
            try
            {
                await DownloadPage(_config.Url, 0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error during download: {ex}");
            }
            finally
            {
                Console.WriteLine("Download complete.");
            }
        }

        private async Task DownloadPage(string currentUrl, int currentDepth)
        {
            if (currentDepth > _config.AssetTypes.Count || _visited.Contains(currentUrl))
                return;

            lock (_lock)
            {
                _visited.Add(currentUrl);
            }

            try
            {
                Uri parsedUri = new Uri(currentUrl);
                string domain = parsedUri.Host.Replace('.', '_');
                string dumpsFolder = Path.Combine(_config.OutputFolder, domain);
                if (!Directory.Exists(dumpsFolder))
                    Directory.CreateDirectory(dumpsFolder);

                HttpResponseMessage response = await _httpClient.GetAsync(currentUrl);
                response.EnsureSuccessStatusCode();
                string htmlContent = await response.Content.ReadAsStringAsync();

                string filePath = Path.Combine(dumpsFolder, $"{parsedUri.AbsolutePath.Replace('/', '_')}.html");
                if (string.IsNullOrEmpty(parsedUri.AbsolutePath))
                    filePath = Path.Combine(dumpsFolder, "index.html");

                await File.WriteAllTextAsync(filePath, htmlContent);

                Console.WriteLine($"Website source code saved to {filePath}");
                _progressCount += 10;
                PrintProgress();

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(htmlContent);
                await DASSETS(htmlDocument, currentUrl, dumpsFolder);

                List<Task> tasks = new List<Task>();
                foreach (var link in htmlDocument.DocumentNode.SelectNodes("//a[@href]") ?? new HtmlNodeCollection(null))
                {
                    string nextUrl = new Uri(new Uri(currentUrl), link.GetAttributeValue("href", "")).ToString();
                    if (new Uri(nextUrl).Host == parsedUri.Host)
                    {
                        tasks.Add(Task.Run(() => DownloadPage(nextUrl, currentDepth + 1)));
                    }
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch the website {currentUrl}: {ex.Message}");
            }
        }

        private async Task DASSETS(HtmlDocument doc, string baseUrl, string dumpsFolder)
        {
            var assetTags = new Dictionary<string, string>
            {
                { "img", "src" },
                { "link", "href" },
                { "script", "src" }
            };

            foreach (var tag in assetTags)
            {
                var elements = doc.DocumentNode.SelectNodes($"//{tag.Key}[@{tag.Value}]");
                if (elements != null)
                {
                    foreach (var element in elements)
                    {
                        string assetUrl = element.GetAttributeValue(tag.Value, "");
                        if (string.IsNullOrEmpty(assetUrl)) continue;

                        string fullUrl = new Uri(new Uri(baseUrl), assetUrl).ToString();
                        try
                        {
                            HttpResponseMessage assetResponse = await _httpClient.GetAsync(fullUrl);
                            assetResponse.EnsureSuccessStatusCode();

                            byte[] assetData = await assetResponse.Content.ReadAsByteArrayAsync();
                            Uri assetUri = new Uri(fullUrl);
                            string assetFilePath = Path.Combine(dumpsFolder, Path.GetFileName(assetUri.AbsolutePath));

                            await File.WriteAllBytesAsync(assetFilePath, assetData);

                            Console.WriteLine($"Asset saved to {assetFilePath}");
                            _progressCount += 5;
                            PrintProgress();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to download asset {fullUrl}: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void PrintProgress()
        {
            Console.WriteLine($"Progress: {_progressCount}/{_totalItems} items downloaded.");
        }
    }
}
