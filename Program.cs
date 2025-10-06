using System.Text;
using CatatanKeuanganDotnet.Data;
using CatatanKeuanganDotnet.Models;
using CatatanKeuanganDotnet.Options;
using CatatanKeuanganDotnet.Services;
using CatatanKeuanganDotnet.Services.Interfaces;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string database tidak ditemukan di .env ataupun appsettings.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

var jwtOptions = new JwtOptions
{
    Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? configuration["Jwt:Issuer"] ?? string.Empty,
    Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? configuration["Jwt:Audience"] ?? string.Empty,
    Key = Environment.GetEnvironmentVariable("JWT_KEY") ?? configuration["Jwt:Key"] ?? string.Empty,
    ExpiresMinutes = configuration.GetValue<int?>("Jwt:ExpiresMinutes") ?? 60
};

var expiresSetting = Environment.GetEnvironmentVariable("JWT_EXPIRES_MINUTES");
if (int.TryParse(expiresSetting, out var expiresMinutes))
{
    jwtOptions.ExpiresMinutes = expiresMinutes;
}

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException("JWT key tidak ditemukan. Pastikan JWT_KEY di .env." );
}

builder.Services.Configure<JwtOptions>(options =>
{
    options.Issuer = jwtOptions.Issuer;
    options.Audience = jwtOptions.Audience;
    options.Key = jwtOptions.Key;
    options.ExpiresMinutes = jwtOptions.ExpiresMinutes;
});

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<INotificationService, EmailNotificationService>();

var smtpSection = configuration.GetSection("Smtp");
var smtpOptions = new SmtpOptions
{
    Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? smtpSection["Host"] ?? string.Empty,
    Username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? smtpSection["Username"] ?? string.Empty,
    Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? smtpSection["Password"] ?? string.Empty,
    FromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? smtpSection["FromEmail"] ?? string.Empty,
    FromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? smtpSection["FromName"] ?? string.Empty
};

if (int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var smtpPort))
{
    smtpOptions.Port = smtpPort;
}
else if (int.TryParse(smtpSection["Port"], out var configPort))
{
    smtpOptions.Port = configPort;
}

var enableSslSetting = Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL") ?? smtpSection["EnableSsl"];
if (bool.TryParse(enableSslSetting, out var enableSsl))
{
    smtpOptions.EnableSsl = enableSsl;
}

builder.Services.Configure<SmtpOptions>(options =>
{
    options.Host = smtpOptions.Host;
    options.Port = smtpOptions.Port;
    options.EnableSsl = smtpOptions.EnableSsl;
    options.Username = smtpOptions.Username;
    options.Password = smtpOptions.Password;
    options.FromEmail = smtpOptions.FromEmail;
    options.FromName = smtpOptions.FromName;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Catatan Keuangan API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Masukkan token JWT tanpa awalan \"Bearer\"."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
