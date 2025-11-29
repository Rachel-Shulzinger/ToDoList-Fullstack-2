using System.ComponentModel.DataAnnotations;

namespace TodoApi;

public class RegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = null!;
}

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}