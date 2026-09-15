using System.ComponentModel.DataAnnotations;

namespace CodeCafe.Application.Auth.Models;

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword);
