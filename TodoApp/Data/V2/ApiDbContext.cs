using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApp.Models;

namespace TodoApp.Data.V2;

public class ApiDbContextV2 : IdentityDbContext
{
    public virtual DbSet<ItemDataV2>? ItemsV2 { get; set; }
    public virtual DbSet<RefreshTokens> RefreshTokensV2 { get; set; }

    public ApiDbContextV2(DbContextOptions<ApiDbContextV2> options)
        : base(options)
    {
    }
}