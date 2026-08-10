using test_deploy.DTOs;

namespace test_deploy.Services;

public interface IAuthService
{
    /// <summary>Đăng ký tài khoản mới</summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>Đăng nhập và lấy JWT token</summary>
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
