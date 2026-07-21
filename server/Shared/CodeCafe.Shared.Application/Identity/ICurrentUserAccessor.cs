namespace CodeCafe.Shared.Application.Identity;

public interface ICurrentUserAccessor
{
    Guid? GetCurrentUserId();
}
