using CatatanKeuanganDotnet.Models;

namespace CatatanKeuanganDotnet.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}