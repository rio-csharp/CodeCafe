using System.ComponentModel.DataAnnotations;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Common;

public sealed record ShareRequest(
    [Required, EmailAddress] string Email,
    CollaboratorRole Role);
