using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;
using TodoApp.Configuration;
using TodoApp.Data;
using TodoApp.Data.V1;
using TodoApp.Data.V2;

namespace TodoApp.Extensions;

public static class TodoExtensions
{
    public static IServiceCollection AddTodoServices(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        var dbConnectionString = configuration.GetConnectionString("DefaultConnection");
        serviceCollection.Configure<JwtConfig>(configuration.GetSection("JwtConfig"));
        var key = Encoding.ASCII.GetBytes(configuration["JwtConfig:Secret"] ?? string.Empty);

        var tokenValidationParam = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            RequireExpirationTime = false,

        };
        serviceCollection.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }
    );

    serviceCollection.AddSingleton<TokenValidationParameters>(tokenValidationParam);
        serviceCollection.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(option =>
        {
            option.SaveToken = true;
            option.TokenValidationParameters = tokenValidationParam;
        });
        serviceCollection.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<ApiDbContext>();
        serviceCollection.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<ApiDbContextV2>();
        serviceCollection.AddControllers();
        serviceCollection.AddEndpointsApiExplorer();
        serviceCollection.AddSwaggerGen();
        serviceCollection.AddDbContext<ApiDbContext>(options => options.UseNpgsql(dbConnectionString));
        serviceCollection.AddDbContext<ApiDbContextV2>(options => options.UseNpgsql(dbConnectionString));

        return serviceCollection;
    }
}