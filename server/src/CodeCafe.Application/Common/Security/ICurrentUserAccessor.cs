namespace CodeCafe.Application.Common.Security;

public interface ICurrentUserAccessor
{
    CurrentUser? User { get; }
}
