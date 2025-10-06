using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Dtos.Categories;
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
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetByUser([FromQuery] string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(ApiResponse.Failure(
                    "Parameter userId diperlukan.",
                    StatusCodes.Status400BadRequest));
            }

            var categories = await _categoryService.GetByUserAsync(userId, cancellationToken);
            var data = categories.Select(MapCategory).ToList();

            return Ok(ApiResponse<IEnumerable<CategoryResponse>>.Succeeded(
                data,
                "Daftar kategori berhasil diambil."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var category = await _categoryService.GetByIdAsync(id, cancellationToken);
            if (category == null)
            {
                return NotFound(ApiResponse.Failure(
                    "Kategori tidak ditemukan.",
                    StatusCodes.Status404NotFound));
            }

            return Ok(ApiResponse<CategoryResponse>.Succeeded(
                MapCategory(category),
                "Detail kategori berhasil diambil."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var validationDetails = new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Permintaan tidak valid."
                };

                return BadRequest(ApiResponse<ValidationProblemDetails>.Failure(
                    "Data kategori tidak valid.",
                    StatusCodes.Status400BadRequest,
                    validationDetails));
            }

            var category = await _categoryService.CreateAsync(request, cancellationToken);
            var response = ApiResponse<CategoryResponse>.Succeeded(
                MapCategory(category),
                "Kategori berhasil dibuat.",
                StatusCodes.Status201Created);

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var validationDetails = new ValidationProblemDetails(ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Permintaan tidak valid."
                };

                return BadRequest(ApiResponse<ValidationProblemDetails>.Failure(
                    "Data kategori tidak valid.",
                    StatusCodes.Status400BadRequest,
                    validationDetails));
            }

            var updated = await _categoryService.UpdateAsync(id, request, cancellationToken);
            if (!updated)
            {
                return NotFound(ApiResponse.Failure(
                    "Kategori tidak ditemukan.",
                    StatusCodes.Status404NotFound));
            }

            return Ok(ApiResponse.Succeeded("Kategori berhasil diperbarui."));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var deleted = await _categoryService.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(ApiResponse.Failure(
                    "Kategori tidak ditemukan.",
                    StatusCodes.Status404NotFound));
            }

            return Ok(ApiResponse.Succeeded("Kategori berhasil dihapus."));
        }

        private static CategoryResponse MapCategory(Category category) => new()
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsIncome = category.IsIncome,
            UserId = category.UserId
        };
    }
}
