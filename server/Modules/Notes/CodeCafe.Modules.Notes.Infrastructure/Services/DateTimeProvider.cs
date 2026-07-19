using CodeCafe.Shared.Application.Common.Interfaces;

namespace CodeCafe.Modules.Notes.Infrastructure.Services;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
