using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiTests;

public class WebApiTestFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.AppHost>();

        _app = await appHost.BuildAsync();
        var resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();
        await _app.StartAsync();

        await resourceNotificationService.WaitForResourceAsync("webapi", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(60));

        HttpClient = _app.CreateHttpClient("webapi");
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        HttpClient?.Dispose();
    }
}
