using System.ComponentModel.DataAnnotations;

namespace CodeCafe.Application.Auth.UpdateProfile;

public sealed record UpdateProfileRequest([Required] string DisplayName);
