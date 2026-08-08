namespace CodeCafe.Application.Common;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
