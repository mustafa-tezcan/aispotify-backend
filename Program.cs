using System.Text;
using backend.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using spotifyapp.Data;
using spotifyapp.Interfaces;
using spotifyapp.Mappers;
using spotifyapp.Models;
using spotifyapp.Repositories;
using spotifyapp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SpotifyApp API",
        Version = "v1"
    });

    // ✅ JWT Authorization ekle
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Bearer {token} formatında JWT girin.",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{}
        }
    });
});



builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});


// Authentication (JWT)

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme =
    options.DefaultForbidScheme =
    options.DefaultScheme =
    options.DefaultSignInScheme =
    options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(options =>
{
        // ✅ Debug ekleyin
    var signingKey = builder.Configuration["JWT:SigningKey"];
    Console.WriteLine($"🔐 [Validation] SigningKey: {signingKey}");
    Console.WriteLine($"🔐 [Validation] SigningKey Length: {signingKey?.Length}");
    Console.WriteLine($"🔐 [Validation] Issuer: {builder.Configuration["JWT:Issuer"]}");
    Console.WriteLine($"🔐 [Validation] Audience: {builder.Configuration["JWT:Audience"]}");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"])),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
     // ✅ Hata yakalama
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"❌ Authentication FAILED: {context.Exception.Message}");
            Console.WriteLine($"❌ Exception Type: {context.Exception.GetType().Name}");
            if (context.Exception.InnerException != null)
            {
                Console.WriteLine($"❌ Inner Exception: {context.Exception.InnerException.Message}");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("✅ Token validated successfully!");
            var claims = context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}");
            Console.WriteLine($"✅ Claims: {string.Join(", ", claims ?? new string[0])}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"⚠️ OnChallenge: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

// Repository injection
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddTransient<ISpotifyService, SpotifyService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IGptService, GptService>();



builder.Services.AddHttpClient();

// ✅ CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
var app = builder.Build();
app.UseCors();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();

// ✅ Token'ı kontrol et (authentication'dan ÖNCE)
app.Use(async (context, next) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    Console.WriteLine($"Authorization Header: {authHeader}");
    
    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
    {
        var token = authHeader.Substring(7);
        Console.WriteLine($"Token alındı: {token.Substring(0, Math.Min(50, token.Length))}...");
    }
    else
    {
        Console.WriteLine("❌ Bearer token bulunamadı!");
    }
    
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// ✅ Authentication sonrası kontrol
app.Use(async (context, next) =>
{
    Console.WriteLine($"IsAuthenticated: {context.User.Identity?.IsAuthenticated}");
    Console.WriteLine($"Claim count: {context.User.Claims.Count()}");
    
    foreach (var claim in context.User.Claims)
    {
        Console.WriteLine($"  - {claim.Type}: {claim.Value}");
    }
    
    await next();
});
app.MapControllers();


app.Run();
