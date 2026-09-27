using EnglishLearning.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using EnglishLearning.Domain;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy("AdminWeb", policy => policy
    .WithOrigins(builder.Configuration["AdminWeb:Origin"] ?? "http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));
builder.Services.AddAuthorization(options => options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin")));
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ContentEditor", policy => policy.RequireRole("Admin", "Editor", "Reviewer"));
    options.AddPolicy("ContentReviewer", policy => policy.RequireRole("Admin", "Reviewer"));
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRateLimiter(options => options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
    _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
var jwtKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtKey) && builder.Environment.IsDevelopment())
    jwtKey = "local-development-key-change-me-please-1234567890";
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:SigningKey must be supplied through environment/secret configuration with at least 32 characters.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EnglishLearning.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EnglishLearning.Mobile";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true, ValidIssuer = jwtIssuer,
        ValidateAudience = true, ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EnglishLearning.Infrastructure.Persistence.EnglishLearningDbContext>("postgres");

var app = builder.Build();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EnglishLearning.Infrastructure.Persistence.EnglishLearningDbContext>();
    db.Database.Migrate();
    if (builder.Configuration.GetValue<bool>("Database:SeedDevelopmentUser"))
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        EnglishLearning.Infrastructure.Persistence.DevelopmentDataSeeder.SeedAsync(db, hasher).GetAwaiter().GetResult();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

app.UseRateLimiter();
app.UseCors("AdminWeb");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
