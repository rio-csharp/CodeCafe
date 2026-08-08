using CodeCafe.Application.Common;

namespace CodeCafe.Infrastructure.Common;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
