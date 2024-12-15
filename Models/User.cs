using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class User
{
    public int Id { get; set; }
    
    [Required]
    public string Username { get; set; }
    
    [Required]
    public string Password { get; set; }
    
    [JsonIgnore]
    public bool IsAdmin { get; set; } = false;
    [JsonIgnore]
    public bool IsSuperAdmin { get; set; } = false;
}
