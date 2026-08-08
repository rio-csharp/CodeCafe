using System.Reflection;

namespace CodeCafe.Shared.Infrastructure.Persistence;

/// <summary>
/// Identifies the assembly that physically holds the EF migrations. Callers configure
/// <c>MigrationsAssembly</c> with this instead of a hardcoded string so a rename cannot silently
/// point EF at an assembly with no migrations in it.
/// </summary>
public static class ApplicationDbContextAssembly
{
    public static readonly Assembly Value = typeof(ApplicationDbContextAssembly).Assembly;

    public static string Name => Value.GetName().Name!;
}
