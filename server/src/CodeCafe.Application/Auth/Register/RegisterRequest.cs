using System.ComponentModel.DataAnnotations;

namespace CodeCafe.Application.Auth.Register;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, StringLength(40, MinimumLength = 1)] string DisplayName);
