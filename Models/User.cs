using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace CatatanKeuanganDotnet.Models
{
    public class User
    {
        private const int DefaultIdLength = 10;
        private static readonly char[] AllowedIdCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

        public static string GenerateId(int length = DefaultIdLength)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");
            }

            Span<char> buffer = stackalloc char[length];
            Span<byte> randomBytes = stackalloc byte[length];
            RandomNumberGenerator.Fill(randomBytes);

            for (var i = 0; i < length; i++)
            {
                buffer[i] = AllowedIdCharacters[randomBytes[i] % AllowedIdCharacters.Length];
            }

            return new string(buffer);
        }
    }
}
