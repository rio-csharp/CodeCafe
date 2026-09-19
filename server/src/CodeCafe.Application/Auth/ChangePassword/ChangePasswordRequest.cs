using System.ComponentModel.DataAnnotations;

namespace CodeCafe.Application.Auth.ChangePassword;

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword);
