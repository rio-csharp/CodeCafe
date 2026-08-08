using CodeCafe.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CodeCafe.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>, IAuditableEntity
{
    public required string DisplayName { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
