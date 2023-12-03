using System.ComponentModel.DataAnnotations;

namespace TodoApp.Models.DTO.Requests;

public class UserLogin
{
    public UserLogin(string email, string password)
    {
        Email = email;
        Password = password;
    }

    [Required] public string Email { get; set; }
    [Required] public string Password { get; set; }
}