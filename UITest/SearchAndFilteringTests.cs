using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace UITest;

public sealed class SearchAndFilteringTests : IClassFixture<FrontendFixture>
{
    private static readonly string[] AllTitles =
    [
        "Northern Lights", "Summer Nights", "Midnight Train",
        "Ocean Drive", "Summer Rain", "Blå Himmel"
    ];

    private readonly ChromeDriver _driver;
    private readonly WebDriverWait _wait;

    public SearchAndFilteringTests(FrontendFixture fixture)
    {
        _driver = fixture.Driver;
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        _wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));
        _driver.Navigate().GoToUrl(fixture.BaseUrl);
        AssertVisibleTitles(AllTitles);
        Assert.Empty(_driver.FindElements(By.CssSelector("[role='alert']")));
    }

    [Fact]
    public void PageInitiallyShowsAllRecordsAndTheirColumns()
    {
        Assert.Equal("Musikoversigt", _driver.Title);
        Assert.Equal(new[] { "Northern Lights", "Aurora", "180", "2020" },
            RecordRows().First().FindElements(By.TagName("td")).Select(cell => cell.Text));
        Assert.Equal("", SearchInput.GetDomProperty("value"));
        Assert.Equal("", YearFilter.SelectedOption.GetDomProperty("value"));
    }

    [Theory]
    [InlineData("Northern", "Northern Lights")]
    [InlineData("lights", "Northern Lights")]
    [InlineData("  nOrThErN  ", "Northern Lights")]
    [InlineData("AURORA", "Northern Lights", "Summer Nights")]
    [InlineData("travel", "Midnight Train")]
    [InlineData("sØrEn", "Blå Himmel")]
    [InlineData("BLÅ", "Blå Himmel")]
    public void SearchMatchesTitleOrArtist(string search, params string[] expectedTitles)
    {
        SearchInput.SendKeys(search);

        AssertVisibleTitles(expectedTitles);
    }

    [Fact]
    public void WhitespaceOnlySearchShowsAllRecords()
    {
        SearchInput.SendKeys("   ");

        AssertVisibleTitles(AllTitles);
    }

    [Fact]
    public void YearOptionsAreUniqueAndSortedNewestFirst()
    {
        Assert.Equal(new[] { "", "2022", "2021", "2020" },
            YearFilter.Options.Select(option => option.GetDomProperty("value")));
    }

    [Theory]
    [InlineData("2020", "Northern Lights", "Midnight Train")]
    [InlineData("2021", "Summer Nights", "Summer Rain")]
    [InlineData("2022", "Ocean Drive", "Blå Himmel")]
    public void SelectingYearShowsOnlyRecordsFromThatYear(string year, params string[] expectedTitles)
    {
        YearFilter.SelectByValue(year);

        AssertVisibleTitles(expectedTitles);
        Assert.All(RecordRows(), row => Assert.Equal(year, row.FindElements(By.TagName("td"))[3].Text));
    }

    [Fact]
    public void SearchAndYearFilterCanBeCombinedAndChanged()
    {
        SearchInput.SendKeys("aurora");
        AssertVisibleTitles("Northern Lights", "Summer Nights");

        YearFilter.SelectByValue("2020");
        AssertVisibleTitles("Northern Lights");

        YearFilter.SelectByValue("2021");
        AssertVisibleTitles("Summer Nights");
    }

    [Fact]
    public void SearchWithoutMatchesShowsEmptyMessage()
    {
        SearchInput.SendKeys("findes-ikke-12345");

        AssertNoMatches();
    }

    [Fact]
    public void CombinedFiltersWithNoSharedMatchShowEmptyMessage()
    {
        SearchInput.SendKeys("summer");
        AssertVisibleTitles("Summer Nights", "Summer Rain");

        YearFilter.SelectByValue("2020");

        AssertNoMatches();
    }

    [Fact]
    public void ClearingSearchKeepsYearFilter()
    {
        YearFilter.SelectByValue("2020");
        SearchInput.SendKeys("northern");
        AssertVisibleTitles("Northern Lights");

        // Tastaturinput udløser input-eventet, som Vue bruger til v-model.
        SearchInput.SendKeys(Keys.Control + "a");
        SearchInput.SendKeys(Keys.Backspace);

        AssertVisibleTitles("Northern Lights", "Midnight Train");
        Assert.Equal("2020", YearFilter.SelectedOption.GetDomProperty("value"));
    }

    [Fact]
    public void SelectingAllYearsKeepsSearch()
    {
        SearchInput.SendKeys("aurora");
        YearFilter.SelectByValue("2020");
        AssertVisibleTitles("Northern Lights");

        YearFilter.SelectByValue("");

        AssertVisibleTitles("Northern Lights", "Summer Nights");
        Assert.Equal("aurora", SearchInput.GetDomProperty("value"));
    }

    [Fact]
    public void ResetClearsBothFiltersAndRestoresAllRecords()
    {
        SearchInput.SendKeys("summer");
        YearFilter.SelectByValue("2020");
        AssertNoMatches();

        _driver.FindElement(By.CssSelector("button[type='button']")).Click();

        AssertVisibleTitles(AllTitles);
        Assert.Equal("", SearchInput.GetDomProperty("value"));
        Assert.Equal("", YearFilter.SelectedOption.GetDomProperty("value"));
        Assert.Empty(_driver.FindElements(By.CssSelector("tbody td[colspan]")));
    }

    private IWebElement SearchInput => _driver.FindElement(By.Id("search"));
    private SelectElement YearFilter => new(_driver.FindElement(By.Id("year")));

    private IEnumerable<IWebElement> RecordRows() => _driver.FindElements(By.CssSelector("tbody tr"))
        .Where(row => row.FindElements(By.TagName("td")).Count == 4);

    private void AssertVisibleTitles(params string[] expectedTitles)
    {
        // Vent på Vue-rendering og de konkrete rækker, også når resultatantallet er uændret.
        _wait.Message = $"Forventede titler: [{string.Join(", ", expectedTitles)}]. Kontrollér også, at Vue og Axios kan hentes fra CDN.";
        _wait.Until(driver =>
            RecordRows().Select(row => row.FindElements(By.TagName("td"))[0].Text).SequenceEqual(expectedTitles)
            && driver.FindElement(By.CssSelector("[role='status']")).Text == $"Viser {expectedTitles.Length} af {AllTitles.Length} numre");
    }

    private void AssertNoMatches()
    {
        AssertVisibleTitles();
        Assert.Empty(RecordRows());
        Assert.Equal("Ingen musik matcher din søgning.",
            _driver.FindElement(By.CssSelector("tbody td[colspan='4']")).Text);
    }
}
