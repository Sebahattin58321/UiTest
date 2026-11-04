using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium.Chrome;

namespace UITest;

// Server og browser deles af testene. Hver test genindlæser siden for at nulstille UI'et.
public sealed class FrontendFixture : IAsyncLifetime
{
    private WebApplication? _server;
    private ChromeDriver? _driver;

    public ChromeDriver Driver => _driver ?? throw new InvalidOperationException("Browseren er ikke startet.");
    public string BaseUrl { get; private set; } = "";

    public async Task InitializeAsync()
    {
        try
        {
            var frontendDirectory = Path.Combine(AppContext.BaseDirectory, "TestFrontend");
            var appScript = await File.ReadAllTextAsync(Path.Combine(frontendDirectory, "App.js"));
            const string apiUrl = "https://localhost:7295/api/MusicRecord";
            if (!appScript.Contains(apiUrl, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Frontendens API-adresse er ændret. Opdater testserverens API-omdirigering.");
            }

            // Kun svaret fra testserveren ændres i hukommelsen; kildefilerne ændres aldrig.
            appScript = appScript.Replace(apiUrl, "/api/MusicRecord", StringComparison.Ordinal);

            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                ContentRootPath = AppContext.BaseDirectory,
                EnvironmentName = "Testing"
            });
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

            _server = builder.Build();
            _server.MapGet("/", () => Results.File(Path.Combine(frontendDirectory, "index.html"), "text/html; charset=utf-8"));
            _server.MapGet("/App.js", () => Results.Text(appScript, "application/javascript; charset=utf-8"));
            _server.MapGet("/api/MusicRecord", () => Results.Json(new[]
            {
                new { title = "Northern Lights", artist = "Aurora", duration = 180, publicationYear = 2020 },
                new { title = "Summer Nights", artist = "Aurora", duration = 200, publicationYear = 2021 },
                new { title = "Midnight Train", artist = "The Travelers", duration = 240, publicationYear = 2020 },
                new { title = "Ocean Drive", artist = "Coastal", duration = 210, publicationYear = 2022 },
                new { title = "Summer Rain", artist = "Coastal", duration = 190, publicationYear = 2021 },
                new { title = "Blå Himmel", artist = "Søren Østergaard", duration = 220, publicationYear = 2022 }
            }));
            await _server.StartAsync();
            BaseUrl = _server.Urls.Single();

            var options = new ChromeOptions();
            if (!string.Equals(Environment.GetEnvironmentVariable("UITEST_HEADLESS"), "false", StringComparison.OrdinalIgnoreCase))
            {
                options.AddArgument("--headless=new");
            }
            options.AddArgument("--window-size=1440,1000");

            // Kan angives, hvis en driver allerede er installeret. Ellers bruges Selenium Manager.
            var driverDirectory = Environment.GetEnvironmentVariable("CHROMEDRIVER_DIRECTORY");
            var service = string.IsNullOrWhiteSpace(driverDirectory)
                ? ChromeDriverService.CreateDefaultService()
                : ChromeDriverService.CreateDefaultService(driverDirectory);
            service.HideCommandPromptWindow = true;
            _driver = new ChromeDriver(service, options);
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            _driver?.Dispose();
            _driver = null;
        }
        finally
        {
            if (_server is not null)
            {
                await _server.DisposeAsync();
                _server = null;
            }
        }
    }
}
