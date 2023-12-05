using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TodoApp.Models;

public class RefreshTokens
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Token { get; set; }
    public string JwtId { get; set; }
    public bool isUsed { get; set; }
    public bool isRevoked { get; set; }
    public DateTime AddedDate { get; set; }
    public DateTime ExpiredDate { get; set; }
    [ForeignKey(nameof(UserId))] public IdentityUser User { get; set; }
}