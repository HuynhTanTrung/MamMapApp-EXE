using MamMap.Application.System.Diet;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Diet;
using MamMap.ViewModels.System.Dish;
using Microsoft.AspNetCore.Mvc;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DietController : ControllerBase
    {
        private readonly IDietService _dietService;

        public DietController(IDietService dietService)
        {
            _dietService = dietService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateDiet([FromBody] CreateDietDTO dto)
        {
            var diet = new Diets
            {
                Description = dto.Name
            };

            var (isSuccess, errorMessage, createdDiet) = await _dietService.CreateDietAsync(diet);

            if (!isSuccess)
            {
                return BadRequest(new
                {
                    status = 400,
                    message = errorMessage
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Tạo chế độ ăn thành công.",
                data = new
                {
                    dietId = createdDiet.Id,
                    name = createdDiet.Description
                }
            });
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchDiet([FromBody] SearchDietRequest request)
        {
            var result = await _dietService.SearchDietsAsync(request);
            return Ok(result);
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllDiets()
        {
            var result = await _dietService.GetAllDietsAsync();
            return Ok(result);
        }

        [HttpGet("getById")]
        public async Task<IActionResult> GetDietById(Guid id)
        {
            var diet = await _dietService.GetDietByIdAsync(id);

            if (diet == null)
            {
                return NotFound(new
                {
                    status = 404,
                    message = "Chế độ ăn không tồn tại."
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Lấy thông tin chế độ ăn thành công.",
                data = new
                {
                    dietId = diet.Id,
                    name = diet.Description
                }
            });
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateDiet([FromBody] UpdateDietDTO dto)
        {
            var (isSuccess, errorMessage) = await _dietService.UpdateDietAsync(dto.Id, dto.Name);

            if (!isSuccess)
            {
                return NotFound(new
                {
                    status = 404,
                    message = errorMessage
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Cập nhập chế độ ăn thành công.",
                data = new
                {
                    dto.Id,
                    newName = dto.Name
                }
            });
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteDiet(Guid id)
        {
            var (isSuccess, errorMessage) = await _dietService.DeleteDietAsync(id);

            if (!isSuccess)
            {
                return NotFound(new
                {
                    status = 404,
                    message = errorMessage
                });
            }

            return Ok(new
            {
                status = 200,
                message = "Xóa (ẩn) chế độ ăn thành công."
            });
        }
    }
}
