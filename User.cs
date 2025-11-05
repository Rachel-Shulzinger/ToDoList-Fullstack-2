using System;
using System.ComponentModel.DataAnnotations;

namespace TodoApi;

public partial class User
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string Password { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}