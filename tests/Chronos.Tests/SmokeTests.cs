using System.Net;
using Chronos.Tests.Infrastructure;

namespace Chronos.Tests;

public class SmokeTests : IClassFixture<ChronosFactory>
{
    private readonly ChronosFactory _factory;

    public SmokeTests(ChronosFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task T1_AC1_SmokeHomeReturns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task T1_AC1_UnknownPageReturns404WithMessage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page not found", await response.Content.ReadAsStringAsync());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }
}
