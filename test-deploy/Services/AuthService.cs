using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using test_deploy.Data;
using test_deploy.DTOs;
using test_deploy.Models;

namespace test_deploy.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Kiểm tra email đã tồn tại chưa
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (emailExists)
            throw new InvalidOperationException($"Email '{request.Email}' đã được sử dụng.");

        // Kiểm tra username đã tồn tại chưa
        var usernameExists = await _context.Users
            .AnyAsync(u => u.Username.ToLower() == request.Username.ToLower());

        if (usernameExists)
            throw new InvalidOperationException($"Username '{request.Username}' đã được sử dụng.");

        // Hash password bằng BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        // Tạo user mới
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Đăng ký thành công cho user: {Email}", user.Email);

        // Tạo và trả về JWT token
        return GenerateAuthResponse(user);
    }

    /// <inheritdoc/>
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Tìm user theo email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user is null)
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        // Xác minh mật khẩu bằng BCrypt
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        _logger.LogInformation("Đăng nhập thành công cho user: {Email}", user.Email);

        return GenerateAuthResponse(user);
    }

    // ─────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────

    private AuthResponse GenerateAuthResponse(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secret      = jwtSettings["Secret"]!;
        var issuer      = jwtSettings["Issuer"]!;
        var audience    = jwtSettings["Audience"]!;
        var expiryMins  = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "1440");

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt   = DateTime.UtcNow.AddMinutes(expiryMins);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name,  user.Username),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                      DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                      ClaimValueTypes.Integer64),
            new Claim("userId", user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expiresAt,
            signingCredentials: credentials
        );

        return new AuthResponse
        {
            Token     = new JwtSecurityTokenHandler().WriteToken(token),
            UserId    = user.Id,
            Username  = user.Username,
            Email     = user.Email,
            ExpiresAt = expiresAt
        };
    }
}
