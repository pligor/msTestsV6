using Microsoft.Playwright;

namespace MyTests;

using static libs.GenericHelper;
using System.Threading;
using static Microsoft.Playwright.Assertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

//dotnet test --filter FullyQualifiedName~HelloPlaywrightTest
[TestClass]
public class HelloPlaywrightTest {
  private IPage? page { get; set; }
  private IPlaywright? playwright { get; set; }
  private IBrowser? browser { get; set; }
  /*
    private readonly IPlaywright _playwright;

    public HelloPlaywrightTest()
    {
      _playwright = await Playwright.CreateAsync();
    }
  */
  //before every method/function execution
  [TestInitialize]
  public async Task InitializeAsync() {
    playwright = await Playwright.CreateAsync();
    browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
    page = await browser.NewPageAsync();
  }

  [TestCleanup]
  //after every method/function execution
  public async Task DisposeAsync() {
    Assert.IsNotNull(playwright);
    Assert.IsNotNull(browser);
    Assert.IsNotNull(page);

    await page.CloseAsync();
    await browser.CloseAsync();
    playwright.Dispose();
  }

  //before every method/function execution
  [TestMethod]
  public void example_dummy_test() {
    Assert.IsNotNull(page);
  }

  //dotnet test --filter FullyQualifiedName~HelloPlaywrightTest.example_com_test
  [TestMethod]
  public async Task example_com_test() {
    Assert.IsNotNull(page);

    await page.GotoAsync("https://example.com");
    Thread.Sleep(2000);
    assert(await page.TitleAsync() == "Example Domain");

    // var h1 = await Page.QuerySelectorAsync("h1");
    var h1 = page.Locator("h1");
    await Expect(h1).ToHaveTextAsync("Example Domain");
  }

  /*
    // Destructor
    ~HelloPlaywrightTest()
    {
      // Cleanup code

    }
    */
}