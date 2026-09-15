using System.ComponentModel.DataAnnotations;

namespace CodeCafe.Application.Auth.Models;

public sealed record UpdateProfileRequest([Required] string DisplayName);
