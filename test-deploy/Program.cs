using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using test_deploy.Data;
using test_deploy.Services;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────
// 1. Database - PostgreSQL + Entity Framework Core
// ─────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─────────────────────────────────────────────
// 2. JWT Authentication
// ─────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtSecret   = jwtSettings["Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret chưa được cấu hình.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidAudience            = jwtSettings["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew                = TimeSpan.Zero // Không cho phép trễ thêm giờ
    };
});

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────
// 3. Services (Dependency Injection)
// ─────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();

// ─────────────────────────────────────────────
// 4. Controllers & OpenAPI
// ─────────────────────────────────────────────
builder.Services.AddControllers();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title   = "Auth API";
        document.Info.Version = "v1";
        document.Info.Description =
            "API xác thực người dùng - Đăng ký và Đăng nhập với JWT Bearer Token";
        return Task.CompletedTask;
    });
});

// ─────────────────────────────────────────────
// 5. CORS (cho phép mọi origin trong dev)
// ─────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ─────────────────────────────────────────────
// 6. Tự động migrate database khi khởi động
// ─────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Đang migrate database...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrate thành công!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi khi migrate database");
        throw;
    }
}

// ─────────────────────────────────────────────
// 7. Middleware Pipeline
// ─────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title    = "Auth API - Scalar UI";
        options.DarkMode = true;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint (dùng cho Docker healthcheck)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .ExcludeFromDescription();

// Redirect root → Scalar UI
app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

app.Run();
