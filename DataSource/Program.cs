﻿using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using System.Net;
using Newtonsoft.Json;

namespace DataSource
{
    public class GetDataSource
    {
        private static readonly string SDK_API_URL_BASIC = "https://learn.microsoft.com/en-us/";
        private static readonly string SDK_API_REVIEW_URL_BASIC = "https://review.learn.microsoft.com/en-us/";
        static async Task Main(string[] args)
        {
            // Default Configuration
            using IHost host = Host.CreateApplicationBuilder(args).Build();

            IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

            string? package = config["ReadmeName"];
            string? language = config["Language"];
            string branch = config["Branch"]!;
            string? cookieName = config["CookieName"];
            string? cookieValue = config["CookieValue"];
            
            string? overviewUrl = GetLanguagePageOverview(language, branch);

            List<string> pages = new List<string>();
            List<string> allPages = new List<string>();
            string pagelink = $"{overviewUrl}{package}?branch={branch}";

            await GetAllChildPage(pages, allPages, pagelink, branch, cookieName, cookieValue);

            ExportData(allPages);
        }

        static string GetLanguagePageOverview(string? language, string branch)
        {
            language = language?.ToLower();
            if(branch == "main")
            {
                return $"{SDK_API_URL_BASIC}{language}/api/overview/azure/";
            }
            else{
                return $"{SDK_API_REVIEW_URL_BASIC}{language}/api/overview/azure/";
            }
        }

        static async Task GetAllChildPage(List<string> pages, List<string> allPages, string pagelink, string branch, string? cookieName, string? cookieVal)
        {
            // Launch a browser
            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            
            var context = await browser.NewContextAsync();

            if(branch != "main"){
                var cookie = new[]
                {
                    new Microsoft.Playwright.Cookie
                    {
                        Name = cookieName,
                        Value = cookieVal,
                        Domain = "review.learn.microsoft.com",
                        Path = "/"
                    }
                };

                await context.AddCookiesAsync(cookie);
            }

            var page = await context.NewPageAsync();

            IReadOnlyList<ILocator> links = new List<ILocator>();

            // Retry 5 times to get the child pages if cannot get pagelinks.
            int i = 0;
            while (links.Count == 0)
            {
                // If the page does not contain the specified content, break the loop
                if (i == 5)
                {
                    break;
                }

                await page.GotoAsync(pagelink, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });
                // Get all child pages
                links = await page.Locator("li.tree-item.is-expanded ul.tree-group a").AllAsync();
                
                i++;
            }

            if (links.Count != 0)
            {
                // Get all href attributes of the child pages
                foreach (var link in links)
                {
                    var href = await link.GetAttributeAsync("href");
                    // TODO
                    // if(branch != "main")
                    // {
                    //     href = href + "&branch=" + branch;
                    // }
                    pages.Add(href);
                }

                await browser.CloseAsync();

                // Recursively get all pages of the API reference document
                foreach (var pa in pages)
                {
                    int lastSlashIndex = pa.LastIndexOf('/');
                    string baseUri = pa.Substring(0, lastSlashIndex + 1);
                    allPages.Add(pa);
                    GetAllPages(pa, baseUri, allPages);
                }
            }
        }

        static void GetAllPages(string apiRefDocPage, string? baseUri, List<string> links)
        {
            HtmlWeb web = new HtmlWeb();
            var doc = web.Load(apiRefDocPage);

            // TODO
            // var handler = new HttpClientHandler
            // {
            //     CookieContainer = new CookieContainer()
            // };
    
            // var cookie = new System.Net.Cookie(cookieName, cookieVal)
            // {
            //     Domain = "review.learn.microsoft.com",
            // };
            // handler.CookieContainer.Add(cookie);

            // var httpClient = new HttpClient(handler);
            // httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
            // var response = httpClient.GetAsync(apiRefDocPage).Result;
            // response.EnsureSuccessStatusCode();

            // var htmlContent = response.Content.ReadAsStringAsync().Result;

            // var doc = new HtmlDocument();
            // doc.LoadHtml(htmlContent);

            //The recursion terminates when there are no valid sub pages in the page or when all package links have been visited.
            if (IsTrue(apiRefDocPage))
            {
                var aNodes = doc.DocumentNode.SelectNodes("//td/a | //td/span/a");

                if (aNodes != null)
                {
                    foreach (var node in aNodes)
                    {
                        string href = $"{baseUri}/" + node.Attributes["href"].Value;

                        if (!links.Contains(href))
                        {
                            links.Add(href);

                            //Call GetAllLinks method recursively for each new link.
                            GetAllPages(href, baseUri, links);
                        }
                    }
                }
            }
        }

        static bool IsTrue(string link)
        {
            var web = new HtmlWeb();
            var doc = web.Load(link);

            //TODO
            // var handler = new HttpClientHandler
            // {
            //     CookieContainer = new CookieContainer()
            // };
    
            // var cookie = new System.Net.Cookie(cookieName, cookieVal)
            // {
            //     Domain = "review.learn.microsoft.com",
            // };
            // handler.CookieContainer.Add(cookie);

            // var httpClient = new HttpClient(handler);
            // httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
            // var response = httpClient.GetAsync(link).Result;
            // response.EnsureSuccessStatusCode();

            // var htmlContent = response.Content.ReadAsStringAsync().Result;

            // var doc = new HtmlDocument();
            // doc.LoadHtml(htmlContent);

            var checks = new[]
            {
                new { XPath = "//h1", Content = "Package" },
                new { XPath = "//h2[@id='classes']", Content = "Classes" },
                new { XPath = "//h2[@id='interfaces']", Content = "Interfaces" },
                new { XPath = "//h2[@id='structs']", Content = "Structs" },
                new { XPath = "//h2[@id='typeAliases']", Content = "Type Aliases" },
                new { XPath = "//h2[@id='functions']", Content = "Functions" },
                new { XPath = "//h2[@id='enums']", Content = "Enums" }
            };

            foreach (var check in checks)
            {
                string? hNode = doc.DocumentNode.SelectSingleNode(check.XPath)?.InnerText;
                if (!string.IsNullOrEmpty(hNode) && hNode.Contains(check.Content))
                {
                    return true;
                }
            }

            return false;
        }

        static void ExportData(List<string> pages)
        {
            string jsonString = JsonConvert.SerializeObject(pages, new JsonSerializerSettings
            {
                StringEscapeHandling = StringEscapeHandling.Default
            });

            Console.WriteLine(jsonString);
            File.WriteAllText("../ContentValidation.Test/appsettings.json", jsonString);
        }
    }
}