using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NPOI.SS.Formula.Functions;

namespace ReportHelper
{
    public class CompareDate
    {
        public static async Task Main(string[] args)
        {
            using IHost host = Host.CreateApplicationBuilder(args).Build();

            IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

            string HostPackageName = config["PackageName"] ?? "PackageName";
            string language = config["Language"] ?? "Language";
            string owner = config["Owner"] ?? "Owner";
            string repo = config["Repo"] ?? "Repo";
            string githubToken = config["GitHubToken"] ?? "GitHubToken";

            string rootDirectory = ConstData.ReportsDirectory;

            //Results of the last data summary data (maintenance data)
            string allPackagePath = ConstData.LastPipelineAllPackageJsonFilePath!;
            List<TPackage4Json> allPackageList = new List<TPackage4Json>();

            //Data results for the aim package
            List<TResult4Json> oldDataList = new List<TResult4Json>();

            //Results of this time.
            string newDataPath = Path.Combine(rootDirectory, ConstData.TotalIssuesJsonFileName);
            List<TResult4Json> newDataList = new List<TResult4Json>();

            Console.WriteLine($"Comparing data for {HostPackageName} in {language}...");
            Console.WriteLine($"Owner: {owner}, Repo: {repo}, GitHub Token: {githubToken}");
            Console.WriteLine($"Root Directory: {rootDirectory}");
            Console.WriteLine($"New Data Path: {newDataPath}");
            Console.WriteLine($"All Package Path: {allPackagePath}");
            Console.WriteLine($"Host Package Name: {HostPackageName}");

            if (File.Exists(newDataPath))
            {
                Console.WriteLine($"Reading new data from {newDataPath}");
                newDataList = JsonSerializer.Deserialize<List<TResult4Json>>(File.ReadAllText(newDataPath)) ?? new List<TResult4Json>();
            }

            if (allPackagePath != null && File.Exists(allPackagePath))
            {
                allPackageList = JsonSerializer.Deserialize<List<TPackage4Json>>(File.ReadAllText(allPackagePath)) ?? new List<TPackage4Json>();
                // Finding the target package
                foreach (var package in allPackageList)
                {
                    if (package.PackageName == HostPackageName)
                    {
                        oldDataList = package.ResultList ?? new List<TResult4Json>();
                        continue;
                    }
                }
            }

            Console.WriteLine($"Found {oldDataList.Count} old data items for {HostPackageName} in {language}.");
            Console.WriteLine($"Found {newDataList.Count} new data items for {HostPackageName} in {language}.");
            // Compare the two lists
            List<TResult4Json> differentList = new List<TResult4Json>();
            differentList = CompareLists(oldDataList, newDataList);

            Console.WriteLine($"Found {differentList.Count} different items for {HostPackageName} in {language}.");
            Console.WriteLine("Comparison completed.");

            if (differentList.Count != 0)
            {
                // Save the different results to json and excel files
                string differentDataFileName = ConstData.DiffIssuesJsonFileName;
                JsonHelper4Test.AddTestResult(differentList, differentDataFileName);

                string excelFileName = ConstData.DiffIssuesExcelFileName;
                string differentSheetName = "DiffSheet";
                ExcelHelper4Test.AddTestResult(differentList, excelFileName, differentSheetName);

                // Update github issues
                await GithubHelper.CreateOrUpdateGitHubIssue(owner, repo, githubToken, HostPackageName, language);
            }
            else if (newDataList.Count != 0)
            {
                // Create github issues
                await GithubHelper.CreateOrUpdateGitHubIssue(owner, repo, githubToken, HostPackageName, language);

            }
            else
            {
                Console.WriteLine($"There are no content validation issue with {HostPackageName} in {language}.");
            }
        }
        public static List<TResult4Json> CompareLists(List<TResult4Json> oldDataList, List<TResult4Json> newDataList)
        {
            List<TResult4Json> differentList = new List<TResult4Json>();

            Console.WriteLine($"Comparing {oldDataList.Count} old items with {newDataList.Count} new items...");
            foreach (var newItem in newDataList)
            {
                var matchedOldItem = oldDataList.FirstOrDefault(oldItem =>
                        oldItem.TestCase == newItem.TestCase &&
                        oldItem.ErrorInfo == newItem.ErrorInfo &&
                        oldItem.ErrorLink == newItem.ErrorLink
                    );

                    Console.WriteLine($"Checking new item: {newItem.TestCase} - {newItem.ErrorInfo}");
                //  new TResult is diffrent
                if (matchedOldItem == null)
                {
                    differentList.Add(newItem);
                    Console.WriteLine($"New item added: {newItem.TestCase} - {newItem.ErrorInfo}");
                    continue;
                }
                Console.WriteLine($"Matched old item found: {matchedOldItem.TestCase} - {matchedOldItem.ErrorInfo}");
                // new TResult is same , but locations of error is diffrent
                List<string> differentLocationsList = CompareOfLocations(matchedOldItem.LocationsOfErrors!, newItem.LocationsOfErrors!);
                Console.WriteLine($"Comparing locations for item: {newItem.TestCase} - {newItem.ErrorInfo}");
                if (differentLocationsList.Count > 0)
                {
                    Console.WriteLine($"Different locations found for item: {newItem.TestCase} - {newItem.ErrorInfo}");
                    newItem.LocationsOfErrors = differentLocationsList;
                    differentList.Add(newItem);
                    continue;
                }
            }
            return differentList;
        }
        public static List<string> CompareOfLocations(List<string> oldLocationsList, List<string> newLocationsList)
        {
            List<string> differentLocationsList = new List<string>();
            if (newLocationsList == null && oldLocationsList == null) { return differentLocationsList; }

            if (oldLocationsList == null || newLocationsList == null) { return differentLocationsList; }

            var processedOldLocationsList = oldLocationsList
                    .Select(location => location.Contains(".") ? location.Substring(location.IndexOf(".") + 1) : location)
                    .ToList();

            int count = 0;

            foreach (var location in newLocationsList)
            {
                string str = location.Contains(".") ? location.Substring(location.IndexOf(".") + 1) : location;
                // If new location is not in old locations, they are different
                if (!processedOldLocationsList.Contains(str))
                {
                    differentLocationsList.Add($"{++count}.{str}");
                }
            }
            return differentLocationsList;
        }
    }
}