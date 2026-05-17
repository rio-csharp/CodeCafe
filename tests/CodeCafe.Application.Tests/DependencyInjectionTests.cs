using CodeCafe.Application;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        Assert.Same(services, result);
    }
}
