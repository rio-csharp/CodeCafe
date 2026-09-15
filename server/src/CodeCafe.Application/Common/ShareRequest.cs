using System.ComponentModel.DataAnnotations;

namespace CodeCafe.Application.Common;

public sealed record ShareRequest(
    [Required, EmailAddress] string Email,
    CollaboratorRole Role);
