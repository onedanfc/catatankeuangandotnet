using System.Globalization;
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

var aiSection = configuration.GetSection("Ai");
var aiOptions = new AiOptions
{
    Provider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? aiSection["Provider"] ?? "gemini",
    BaseUrl = Environment.GetEnvironmentVariable("AI_BASE_URL") ?? aiSection["BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta",
    ApiKey = Environment.GetEnvironmentVariable("AI_API_KEY") ?? aiSection["ApiKey"] ?? string.Empty,
    Model = Environment.GetEnvironmentVariable("AI_MODEL") ?? aiSection["Model"] ?? "gemini-2.0-flash",
    ApiKeyHeaderName = Environment.GetEnvironmentVariable("AI_API_KEY_HEADER") ?? aiSection["ApiKeyHeaderName"] ?? "X-goog-api-key",
    Organization = Environment.GetEnvironmentVariable("AI_ORGANIZATION") ?? aiSection["Organization"],
    UseBearerPrefix = bool.TryParse(Environment.GetEnvironmentVariable("AI_USE_BEARER"), out var useBearer)
        ? useBearer
        : (bool.TryParse(aiSection["UseBearerPrefix"], out var configBearer) ? configBearer : false)
};

if (double.TryParse(Environment.GetEnvironmentVariable("AI_TEMPERATURE"), NumberStyles.Float, CultureInfo.InvariantCulture, out var envTemperature))
{
    aiOptions.Temperature = envTemperature;
}
else if (double.TryParse(aiSection["Temperature"], NumberStyles.Float, CultureInfo.InvariantCulture, out var configTemperature))
{
    aiOptions.Temperature = configTemperature;
}

if (int.TryParse(Environment.GetEnvironmentVariable("AI_MAX_TOKENS"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var envMaxTokens))
{
    aiOptions.MaxTokens = envMaxTokens;
}
else if (int.TryParse(aiSection["MaxTokens"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var configMaxTokens))
{
    aiOptions.MaxTokens = configMaxTokens;
}

builder.Services.Configure<AiOptions>(options =>
{
    options.Provider = aiOptions.Provider;
    options.BaseUrl = aiOptions.BaseUrl;
    options.ApiKey = aiOptions.ApiKey;
    options.Model = aiOptions.Model;
    options.Temperature = aiOptions.Temperature;
    options.MaxTokens = aiOptions.MaxTokens;
    options.Organization = aiOptions.Organization;
    options.ApiKeyHeaderName = aiOptions.ApiKeyHeaderName;
    options.UseBearerPrefix = aiOptions.UseBearerPrefix;
});

builder.Services.AddHttpClient<IAiClient, AiClient>();
builder.Services.AddScoped<IAiService, AiService>();

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

var passwordResetSection = configuration.GetSection("PasswordReset");
var passwordResetOptions = new PasswordResetOptions
{
    LinkBase = Environment.GetEnvironmentVariable("PASSWORD_RESET_LINK_BASE") ?? passwordResetSection["LinkBase"] ?? string.Empty
};

builder.Services.Configure<PasswordResetOptions>(options =>
{
    options.LinkBase = passwordResetOptions.LinkBase;
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
