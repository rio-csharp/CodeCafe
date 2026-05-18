namespace CodeCafe.Domain.Common.Interfaces;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; set; }
    DateTimeOffset? UpdatedAtUtc { get; set; }
}
