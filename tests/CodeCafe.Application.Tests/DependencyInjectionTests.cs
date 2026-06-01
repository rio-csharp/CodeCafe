using CodeCafe.Application.Notes.Commands.CreateNotebook;
using FluentValidation;
using MediatR;
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

    [Fact]
    public void AddApplication_RegistersMediator_And_Validators()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<ISender>());
        Assert.Single(serviceProvider.GetServices<IValidator<CreateNotebookCommand>>());
    }
}
