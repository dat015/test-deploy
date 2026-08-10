using Microsoft.AspNetCore.Mvc;
using test_deploy.DTOs;
using test_deploy.Services;

namespace test_deploy.Controllers;

/// <summary>
/// API xác thực người dùng - Đăng ký và Đăng nhập
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Đăng ký tài khoản mới</summary>
    /// <remarks>
    /// Tạo tài khoản mới với username, email và mật khẩu. Trả về JWT token sau khi đăng ký thành công.
    /// </remarks>
    /// <param name="request">Thông tin đăng ký</param>
    /// <returns>JWT token và thông tin user</returns>
    /// <response code="201">Đăng ký thành công, trả về JWT token</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc email/username đã tồn tại</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authService.RegisterAsync(request);
            return CreatedAtAction(nameof(Register), response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Đăng ký thất bại: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi đăng ký");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Đã xảy ra lỗi, vui lòng thử lại sau." });
        }
    }

    /// <summary>Đăng nhập vào hệ thống</summary>
    /// <remarks>
    /// Xác thực email và mật khẩu. Trả về JWT token hợp lệ sau khi đăng nhập thành công.
    /// </remarks>
    /// <param name="request">Thông tin đăng nhập</param>
    /// <returns>JWT token và thông tin user</returns>
    /// <response code="200">Đăng nhập thành công, trả về JWT token</response>
    /// <response code="400">Dữ liệu không hợp lệ</response>
    /// <response code="401">Email hoặc mật khẩu không đúng</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Đăng nhập thất bại cho email: {Email}", request.Email);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi đăng nhập");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Đã xảy ra lỗi, vui lòng thử lại sau." });
        }
    }
}
