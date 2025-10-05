using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Dtos.Auth;
using CatatanKeuanganDotnet.Dtos.Common;
using CatatanKeuanganDotnet.Models;
using CatatanKeuanganDotnet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CatatanKeuanganDotnet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ITokenService tokenService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var validationDetails = new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Permintaan tidak valid."
                };

                return BadRequest(ApiResponse<ValidationProblemDetails>.Failure(
                    "Data registrasi tidak valid.",
                    StatusCodes.Status400BadRequest,
                    validationDetails));
            }

            try
            {
                var user = await _userService.RegisterAsync(request, cancellationToken);
                var response = ApiResponse<UserResponse>.Succeeded(
                    MapUser(user),
                    "Registrasi berhasil.",
                    StatusCodes.Status201Created);

                return CreatedAtRoute("GetUserById", new { id = user.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Registrasi gagal untuk email {Email}", request.Email);
                return Conflict(ApiResponse.Failure(ex.Message, StatusCodes.Status409Conflict));
            }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var validationDetails = new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Permintaan tidak valid."
                };

                return BadRequest(ApiResponse<ValidationProblemDetails>.Failure(
                    "Data login tidak valid.",
                    StatusCodes.Status400BadRequest,
                    validationDetails));
            }

            var user = await _userService.AuthenticateAsync(request, cancellationToken);
            if (user == null)
            {
                return Unauthorized(ApiResponse.Failure(
                    "Email atau password salah.",
                    StatusCodes.Status401Unauthorized));
            }

            var token = _tokenService.GenerateToken(user);
            var authResponse = new AuthResponse
            {
                Token = token,
                User = MapUser(user)
            };

            return Ok(ApiResponse<AuthResponse>.Succeeded(
                authResponse,
                "Login berhasil."));
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var validationDetails = new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Permintaan tidak valid."
                };

                return BadRequest(ApiResponse<ValidationProblemDetails>.Failure(
                    "Data reset password tidak valid.",
                    StatusCodes.Status400BadRequest,
                    validationDetails));
            }

            var updated = await _userService.ResetPasswordAsync(request, cancellationToken);
            if (!updated)
            {
                return BadRequest(ApiResponse.Failure(
                    "Token reset password tidak valid atau sudah kadaluarsa.",
                    StatusCodes.Status400BadRequest));
            }

            return Ok(ApiResponse.Succeeded("Password berhasil diperbarui."));
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var validationDetails = new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Permintaan tidak valid."
                };

                return BadRequest(ApiResponse<ValidationProblemDetails>.Failure(
                    "Data lupa password tidak valid.",
                    StatusCodes.Status400BadRequest,
                    validationDetails));
            }

            var tokenGenerated = await _userService.SendPasswordResetTokenAsync(request, cancellationToken);

            if (!tokenGenerated)
            {
                _logger.LogInformation("Password reset requested for non-registered email {Email}", request.Email);
            }

            return Ok(ApiResponse.Succeeded("Jika email terdaftar, token reset telah dikirim."));
        }

        private static UserResponse MapUser(User user) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
}
