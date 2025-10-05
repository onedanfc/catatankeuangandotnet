using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Data;
using CatatanKeuanganDotnet.Dtos.Auth;
using CatatanKeuanganDotnet.Models;
using CatatanKeuanganDotnet.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CatatanKeuanganDotnet.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly INotificationService _notificationService;

        public UserService(ApplicationDbContext context, IPasswordHasher<User> passwordHasher, INotificationService notificationService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _notificationService = notificationService;
        }

        public async Task<User> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var existingUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

            if (existingUser != null)
            {
                throw new InvalidOperationException("Email sudah terdaftar.");
            }

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return user;
        }

        public async Task<User?> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsActive, cancellationToken);

            if (user == null)
            {
                return null;
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            return verificationResult == PasswordVerificationResult.Failed ? null : user;
        }

        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.FullName)
                .ToListAsync(cancellationToken);

            return users;
        }

        public async Task<bool> SendPasswordResetTokenAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsActive, cancellationToken);

            if (user == null)
            {
                return false;
            }

            var existingTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.UsedAt.HasValue && t.ExpiresAt >= DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            foreach (var token in existingTokens)
            {
                token.UsedAt = DateTime.UtcNow;
            }

            var rawToken = GenerateSecureToken();
            var tokenHash = HashToken(rawToken);

            var expiresAt = DateTime.UtcNow.AddMinutes(30);

            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt
            };

            await _context.PasswordResetTokens.AddAsync(resetToken, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                await _notificationService.SendPasswordResetTokenAsync(user.Email, rawToken, expiresAt, cancellationToken);
            }
            catch
            {
                resetToken.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                throw;
            }

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var tokenHash = HashToken(request.Token);

            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (resetToken?.User == null)
            {
                return false;
            }

            if (resetToken.UsedAt.HasValue || resetToken.ExpiresAt < DateTime.UtcNow || !resetToken.User.IsActive)
            {
                return false;
            }

            resetToken.User.PasswordHash = _passwordHasher.HashPassword(resetToken.User, request.NewPassword);
            resetToken.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        private static string GenerateSecureToken()
        {
            Span<byte> buffer = stackalloc byte[32];
            RandomNumberGenerator.Fill(buffer);
            return Convert.ToHexString(buffer);
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = sha.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}
