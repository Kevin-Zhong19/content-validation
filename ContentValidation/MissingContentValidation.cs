using Microsoft.Playwright;

namespace UtilityLibraries;

public class MissingContentValidation : IValidation
{
    private IPlaywright _playwright;

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

        // Fetch all th and td tags in the test page.
        var cellElements = await page.Locator("td,th").AllAsync();
        var cellElements2 = await page.Locator("td[colspan='2']").AllAsync();
        // var cellElements = await page.Locator("td,th").AllAsync();

        // Flag for ignore method clear, copy, items, keys, values
        bool skipFlag = false;

        // Check if the cell is empty. If it is, retrieve the href attribute of the anchor tag above it for positioning.
        foreach (var cell in cellElements)
        {
            if (skipFlag)
            {
                skipFlag = false;
                continue;
            }

            var cellText = (await cell.InnerTextAsync()).Trim();

            // Skip cells that match the ignore list
            if (ignoreList.Any(item => cellText.Equals(item.IgnoreText, StringComparison.OrdinalIgnoreCase)))
            {
                skipFlag = true;
                continue;
            }

            // Usage: Check if it is an empty cell and get the href attribute of the nearest <a> tag with a specific class name before it. Finally, group and format these errors by position and number of occurrences.
            // Example: The Description column of the Parameter table is Empty.
            // Link: https://learn.microsoft.com/en-us/python/api/azure-ai-textanalytics/azure.ai.textanalytics.aio.asyncanalyzeactionslropoller?view=azure-python
            if (string.IsNullOrEmpty(cellText))
            {
                string anchorLink = "No anchor link found, need to manually search for empty cells on the page.";

                // Find the nearest heading text
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

                    if (ignoreList.Any(item => nearestHTagText.Equals(item.IgnoreText, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue; // Skip if the nearest heading text is in the ignore list
                    }

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
                                break; // Exit loop once the matching link is found
                            }
                        }
                    }
                }

                // Add the anchor link to the error list if it doesn't match excluded patterns
                if (!anchorLink.Contains("#packages", StringComparison.OrdinalIgnoreCase) &&
                    !anchorLink.Contains("#modules", StringComparison.OrdinalIgnoreCase))
                {
                    errorList.Add(anchorLink);
                }
            }
        }
        foreach (var cell in cellElements2)
        {
            if (skipFlag)
            {
                skipFlag = false;
                continue;
            }

            var cellText = (await cell.InnerTextAsync()).Trim();

            // Skip cells that match the ignore list
            if (ignoreList.Any(item => cellText.Equals(item.IgnoreText, StringComparison.OrdinalIgnoreCase)))
            {
                skipFlag = true;
                continue;
            }

            // Usage: Check if it is an empty cell and get the href attribute of the nearest <a> tag with a specific class name before it. Finally, group and format these errors by position and number of occurrences.
            // Example: The Description column of the Parameter table is Empty.
            // Link: https://learn.microsoft.com/en-us/python/api/azure-ai-textanalytics/azure.ai.textanalytics.aio.asyncanalyzeactionslropoller?view=azure-python
            string anchorLink = "No anchor link found, need to manually search for empty cells on the page.";

            // Find the nearest heading text
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

                if (ignoreList.Any(item => nearestHTagText.Equals(item.IgnoreText, StringComparison.OrdinalIgnoreCase)))
                {
                    continue; // Skip if the nearest heading text is in the ignore list
                }

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
                            break; // Exit loop once the matching link is found
                        }
                    }
                }
            }

            // Add the anchor link to the error list if it doesn't match excluded patterns
            if (!anchorLink.Contains("#packages", StringComparison.OrdinalIgnoreCase) &&
                !anchorLink.Contains("#modules", StringComparison.OrdinalIgnoreCase))
            {
                errorList.Add(anchorLink);
            }
            
        }

        // Format the error list
        var formattedList = errorList
            .GroupBy(item => item)
            .Select((group, index) => $"{index + 1}. Appears {group.Count()} times , location : {group.Key}")
            .ToList();

        // Update the result object
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
}