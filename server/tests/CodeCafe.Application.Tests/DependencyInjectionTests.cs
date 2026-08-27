using CodeCafe.Application.Identity;
using CodeCafe.Application.Identity.Commands.RegisterUser;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Notes.Commands.CreateNotebook;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void ModuleApplicationRegistration_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddIdentityApplication().AddNotesApplication();

        Assert.Same(services, result);
    }

    [Fact]
    public void ModuleApplicationRegistration_RegistersMediator_And_Validators()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityApplication();
        services.AddNotesApplication();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<ISender>());
        Assert.Single(serviceProvider.GetServices<IValidator<CreateNotebookCommand>>());
        Assert.Single(serviceProvider.GetServices<IValidator<RegisterUserCommand>>());
    }
}
