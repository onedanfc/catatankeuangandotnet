﻿using System;

namespace CatatanKeuanganDotnet.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
