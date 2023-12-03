using System.ComponentModel.DataAnnotations;

namespace TodoApp.Models.DTO.Requests;

public class UserRegistration
{
    public UserRegistration(string email, string username, string password)
    {
        Email = email;
        Username = username;
        Password = password;
    }

    [Required] [EmailAddress] public string Email { get; set; }
    [Required] public string Username { get; set; }
    [Required] public string Password { get; set; }
}