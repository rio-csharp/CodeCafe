using System.ComponentModel.DataAnnotations;

namespace CodeCafe.Application.Auth.Login;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
