using Microsoft.Playwright;

namespace UtilityLibraries;

public class MissingContentValidation : IValidation
{
    private readonly IPlaywright _playwright;

    public MissingContentValidation(IPlaywright playwright)
    {
        _playwright = playwright ?? throw new ArgumentNullException(nameof(playwright));
    }

    public List<IgnoreItem> ignoreList = IgnoreData.GetIgnoreList("MissingContentValidation", "subset");

    public async Task<TResult> Validate(string testLink)
    {
        var res = new TResult();
        var errorList = new List<string>();

        // Create a browser instance.
        var browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await PlaywrightHelper.GotoageWithRetriesAsync(page, testLink);

        await page.Locator("td").First.WaitForAsync(new() { Timeout = 10000 });

        // Get all td and th elements
        var cellElements = await page.Locator("th,td").ElementHandlesAsync();
        var cellElements2 = await page.Locator("td[colspan='2']").ElementHandlesAsync();

        // Process all cells
        foreach (var cell in cellElements)
        {
            await ProcessCellAsync(cell, page, testLink, errorList, ignoreList, isColspan2: false);
        }

        foreach (var cell in cellElements2)
        {
            await ProcessCellAsync(cell, page, testLink, errorList, ignoreList, isColspan2: true);
        }

        // Format the error list
        var formattedList = errorList
            .GroupBy(item => item)
            .Select((group, index) => $"{index + 1}. Appears {group.Count()} times , location : {group.Key}")
            .ToList();

        // Populate result object
        if (errorList.Count > 0)
        {
            res.Result = false;
            res.ErrorLink = testLink;
            res.ErrorInfo = "Some cells in the table are missing content";
            res.NumberOfOccurrences = errorList.Count;
            res.LocationsOfErrors = formattedList;
        }

        await browser.CloseAsync();
        return res;
    }

    public bool isIgnore = false;
    private async Task ProcessCellAsync(
        IElementHandle cell,
        IPage page,
        string testLink,
        List<string> errorList,
        List<IgnoreItem> ignoreList,
        bool isColspan2 = false)
    {
        // var cellText = (await cell.InnerTextAsync()).Trim();
        var rawText = await cell.EvaluateAsync<string>("el => el.textContent");
        var cellText = rawText?.Trim() ?? "";

        // Console.WriteLine($"Processing cell: {cellText}");

        // Skip ignored text
        if (ignoreList.Any(item => cellText.Equals(item.IgnoreText, StringComparison.OrdinalIgnoreCase)))
        {
            isIgnore = true;
            return;
        }

        if (!isColspan2)
        {
            if (!string.IsNullOrEmpty(cellText))
            {
                isIgnore = false;
                return;
            }
            else
            {
                if (isIgnore)
                {
                    isIgnore = false;
                    return; // Skip if the cell is ignored
                }
            }
        }

        var anchorLink = await GetAnchorLinkForCellAsync(cell, page, testLink);

        if (!anchorLink.Contains("#packages", StringComparison.OrdinalIgnoreCase) &&
            !anchorLink.Contains("#modules", StringComparison.OrdinalIgnoreCase))
        {
            errorList.Add(anchorLink);
        }
    }

    private async Task<string> GetAnchorLinkForCellAsync(IElementHandle cell, IPage page, string testLink)
    {
        string anchorLink = "No anchor link found, need to manually search for empty cells on the page.";

        var nearestHTagText = await cell.EvaluateAsync<string?>(@"element => {
            function findNearestHeading(startNode) {
                let currentNode = startNode;
                while (currentNode && currentNode.tagName !== 'BODY' && currentNode.tagName !== 'HTML') {
                    let sibling = currentNode.previousElementSibling;
                    while (sibling) {
                        if (['H2', 'H3'].includes(sibling.tagName)) {
                            return sibling.textContent?.trim() || '';
                        }
                        let childHeading = findNearestHeadingInChildren(sibling);
                        if (childHeading) {
                            return childHeading;
                        }
                        sibling = sibling.previousElementSibling;
                    }
                    currentNode = currentNode.parentElement;
                }
                return null;
            }

            function findNearestHeadingInChildren(node) {
                for (let child of node.children) {
                    if (['H2', 'H3'].includes(child.tagName)) {
                        return child.textContent?.trim() || '';
                    }
                    let grandChildHeading = findNearestHeadingInChildren(child);
                    if (grandChildHeading) {
                        return grandChildHeading;
                    }
                }
                return null;
            }

            return findNearestHeading(element);
        }");

        if (!string.IsNullOrEmpty(nearestHTagText))
        {
            nearestHTagText = nearestHTagText.Replace("\n", "").Replace("\t", "");

            // Skip heading if it's in the ignore list
            if (ignoreList.Any(item => nearestHTagText.Equals(item.IgnoreText, StringComparison.OrdinalIgnoreCase)))
                return anchorLink;

            var aLocators = page.Locator("#side-doc-outline a");
            var aElements = await aLocators.ElementHandlesAsync();

            foreach (var elementHandle in aElements)
            {
                var linkText = (await elementHandle.InnerTextAsync())?.Trim();
                if (linkText == nearestHTagText)
                {
                    var href = await elementHandle.GetAttributeAsync("href");
                    if (!string.IsNullOrEmpty(href))
                    {
                        anchorLink = testLink + href;
                        break;
                    }
                }
            }
        }

        return anchorLink;
    }
}
