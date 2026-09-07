using System.Security.Claims;
using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.Brownfield;
using EventManagementSystemServiceLayer.Services.Brownfield;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
    [ApiController]
    [Route("api/v1/categories")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories([FromQuery] bool onlyActive = true, CancellationToken ct = default)
        {
            var items = await _service.GetCategoriesAsync(onlyActive, ct);
            return Ok(new ApiResponse<IReadOnlyList<CategoryResponseDto>>(true, 200, "Categories retrieved.", items));
        }

        [HttpGet("{categoryId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int categoryId, CancellationToken ct = default)
        {
            var item = await _service.GetByIdAsync(categoryId, ct);
            return item is null
                ? NotFound(new ApiResponse<object>(false, 404, "Category not found."))
                : Ok(new ApiResponse<CategoryResponseDto>(true, 200, "Category retrieved.", item));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create([FromBody] CategoryCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _service.CreateAsync(userId, dto, ct);
                return CreatedAtAction(nameof(GetById), new { categoryId = result!.CategoryId }, new ApiResponse<CategoryResponseDto>(true, 201, "Category created.", result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
            }
        }

        [HttpPut("{categoryId:int}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Update(int categoryId, [FromBody] CategoryUpdateDto dto, CancellationToken ct = default)
        {
            if (categoryId != dto.CategoryId)
                return BadRequest(new ApiResponse<object>(false, 400, "Category ID mismatch."));

            var result = await _service.UpdateAsync(dto, ct);
            return result is null
                ? NotFound(new ApiResponse<object>(false, 404, "Category not found."))
                : Ok(new ApiResponse<CategoryResponseDto>(true, 200, "Category updated.", result));
        }

        private long GetCurrentUserId()
        {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return long.TryParse(val, out var id) ? id : 0;
        }
    }
}

