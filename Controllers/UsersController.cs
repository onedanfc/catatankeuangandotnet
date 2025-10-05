using System.Collections.Generic;
using System.Linq;
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
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        {
            var users = await _userService.GetAllAsync(cancellationToken);
            var data = users.Select(MapUser).ToList();

            return Ok(ApiResponse<IEnumerable<UserResponse>>.Succeeded(
                data,
                "Daftar pengguna berhasil diambil."));
        }

        [HttpGet("{id:int}", Name = "GetUserById")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(ApiResponse.Failure(
                    "Pengguna tidak ditemukan.",
                    StatusCodes.Status404NotFound));
            }

            return Ok(ApiResponse<UserResponse>.Succeeded(
                MapUser(user),
                "Detail pengguna berhasil diambil."));
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
