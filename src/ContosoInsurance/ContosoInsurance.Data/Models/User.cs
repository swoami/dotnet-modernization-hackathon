namespace ContosoInsurance.Data.Models;

public class User
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
}
