using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using CMRL.API.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// =========================
// DATABASE
// =========================

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

string connectionString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    var npgsqlBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Username = userInfo[0],
        Password = userInfo[1],
        Database = uri.AbsolutePath.Trim('/'),
        SslMode = SslMode.Require
    };

    connectionString = npgsqlBuilder.ConnectionString;
}
else
{
    connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")!;
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// =========================
// JWT AUTHENTICATION
// =========================

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey!))
    };
});

// =========================
// CORS
// =========================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy
            .WithOrigins(
                "https://cmrl-frontend.onrender.com",
                "http://localhost:4200"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// =========================
// SERVICES
// =========================

builder.Services.AddScoped<CMRL.API.Services.EmailService>();

// =========================
// CONTROLLERS
// =========================

builder.Services.AddControllers();

// =========================
// SWAGGER
// =========================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Disabled temporarily
// builder.Services.AddHostedService<CMRL.API.Services.AbsentMarkerService>();

var app = builder.Build();

// =========================
// AUTO MIGRATION FOR RENDER
// =========================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        Console.WriteLine("=================================");
        Console.WriteLine("APPLYING DATABASE MIGRATIONS...");
        Console.WriteLine("=================================");

        db.Database.Migrate();

        Console.WriteLine("MIGRATIONS APPLIED SUCCESSFULLY");

        var userCount = db.Users.Count();

        Console.WriteLine($"USERS COUNT = {userCount}");
    }
    catch (Exception ex)
    {
        Console.WriteLine("=================================");
        Console.WriteLine("DATABASE ERROR");
        Console.WriteLine("=================================");
        Console.WriteLine(ex.ToString());
    }
}

// =========================
// MIDDLEWARE
// =========================

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// =========================
// HEALTH CHECKS
// =========================

app.MapGet("/", () => "CMRL API Running");
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();