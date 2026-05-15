using CodeCafe.Application;
using CodeCafe.Application.Notes;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.UnitTests.Application;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddCodeCafeApplication_registers_notes_services()
    {
        var services = new ServiceCollection();

        services.AddCodeCafeApplication();

        var notesService = Assert.Single(services, service => service.ServiceType == typeof(INotesService));
        var settingsService = Assert.Single(services, service => service.ServiceType == typeof(INotesSettingsService));

        Assert.Equal(ServiceLifetime.Scoped, notesService.Lifetime);
        Assert.Equal(typeof(NotesService), notesService.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, settingsService.Lifetime);
        Assert.Equal(typeof(NotesSettingsService), settingsService.ImplementationType);
    }
}
