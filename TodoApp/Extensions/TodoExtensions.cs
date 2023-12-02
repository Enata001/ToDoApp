using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;
using TodoApp.Configuration;
using TodoApp.Data;

namespace TodoApp.Extensions;

public static class TodoExtensions
{
    public static IServiceCollection AddTodoServices(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        var dbConnectionString = configuration.GetConnectionString("DefaultConnection");
        serviceCollection.Configure<JwtConfig>(configuration.GetSection("JwtConfig"));
        serviceCollection.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(option =>
        {
            var key = Encoding.ASCII.GetBytes(configuration["JwtConfig:Secret"] ?? string.Empty);
            option.SaveToken = true;
            option.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                RequireExpirationTime = false,
                    
            };
        });
        serviceCollection.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<ApiDbContext>();
        serviceCollection.AddControllers();
        serviceCollection.AddEndpointsApiExplorer();
        serviceCollection.AddSwaggerGen();
        serviceCollection.AddDbContext<ApiDbContext>(options => options.UseNpgsql(dbConnectionString));

        return serviceCollection;
    }
}