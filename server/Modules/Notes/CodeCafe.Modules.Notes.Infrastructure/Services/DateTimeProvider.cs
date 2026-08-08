using CodeCafe.Application.Common;

namespace CodeCafe.Modules.Notes.Infrastructure.Services;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
