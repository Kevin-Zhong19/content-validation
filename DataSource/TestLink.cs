using System.Text.Json;

public class TestLinkDataObject
{
    public required string Language { get; set; }
    public List<TestLinkItem> TestLink { get; set; } = new List<TestLinkItem>();

    public TestLinkDataObject() { }

    public TestLinkDataObject(string language, List<TestLinkItem> testLink)
    {
        Language = language;
        TestLink = testLink;
    }
}

public class TestLinkItem
{
    public required string PackageName { get; set; }
    public required string Version { get; set; }
    public List<string> Url { get; set; } = new List<string>();

    public TestLinkItem() { }

    public TestLinkItem(string packageName, string version, List<string> url)
    {
        PackageName = packageName;
        Version = version;
        Url = url;
    }
}

public class TestLinkData
{
    public static List<TestLinkDataObject> TestLinkDataObjectList { get; set; } = new();
    private static readonly string FilePath = "testlink.json";

    static TestLinkData()
    {
        if (!File.Exists(FilePath))
        {
            throw new FileNotFoundException("File not found", FilePath);
        }

        string jsonContent = File.ReadAllText(FilePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        TestLinkDataObjectList = JsonSerializer.Deserialize<List<TestLinkDataObject>>(jsonContent, options) ?? new List<TestLinkDataObject>();
    }

    public static List<string> GetUrls(string language, string packageName, string version)
    {
        try
        {
            var testLinkObject = TestLinkDataObjectList.Find(x => x.Language.Equals(language, StringComparison.OrdinalIgnoreCase));
            var package = testLinkObject?.TestLink.Find(x => x.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase) && x.Version.Equals(version, StringComparison.OrdinalIgnoreCase));
            return package?.Url ?? new List<string>();
        }
        catch
        {
            Console.WriteLine("There is no testLink.");
            return new List<string>();
        }
        
    }

    public static void AddUrls(string language, string packageName, string version, List<string> urls)
    {
        ClearUrls(language, packageName, version);
        
        var testLinkObject = TestLinkDataObjectList.Find(x => x.Language.Equals(language, StringComparison.OrdinalIgnoreCase));

        if (testLinkObject == null)
        {
            testLinkObject = new TestLinkDataObject
            {
                Language = language,
                TestLink = new List<TestLinkItem>()
            };
            TestLinkDataObjectList.Add(testLinkObject);
        }

        var package = testLinkObject.TestLink.Find(x => x.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

        if (package == null)
        {
            package = new TestLinkItem
            {
                PackageName = packageName,
                Version = version,
                Url = new List<string>()
            };
            testLinkObject.TestLink.Add(package);
        }

        package.Url.AddRange(urls);

        SaveToFile();
    }

    public static void ClearUrls(string language, string packageName, string version)
    {
        var testLinkObject = TestLinkDataObjectList.Find(x => x.Language.Equals(language, StringComparison.OrdinalIgnoreCase));
        var package = testLinkObject?.TestLink.Find(x => x.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase) && x.Version.Equals(version, StringComparison.OrdinalIgnoreCase));

        if (package != null)
        {
            package.Url.Clear();
            SaveToFile();
        }
    }

    private static void SaveToFile()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string jsonContent = JsonSerializer.Serialize(TestLinkDataObjectList, options);
        File.WriteAllText(FilePath, jsonContent);
    }
}