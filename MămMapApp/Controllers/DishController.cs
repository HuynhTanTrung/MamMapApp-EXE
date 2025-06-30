using MamMap.Application.System.Dish;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Dish;
using MamMap.ViewModels.System.Other;
using Microsoft.AspNetCore.Mvc;

namespace MamMapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DishController : ControllerBase
    {
        private readonly IDishService _dishService;

        public DishController(IDishService dishService)
        {
            _dishService = dishService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateDish([FromBody] CreateDishDTO dto)
        {
            var newDish = new Dishes
            {
                Name = dto.Name,
                Description = dto.Description,
                Image = dto.Image,
                Price = dto.Price,
                SnackPlaceId = dto.SnackPlaceId
            };

            var (isSuccess, errorMessage, createdDish) = await _dishService.CreateDishAsync(newDish);

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
                message = "Tạo món ăn thành công.",
                data = new
                {
                    dishId = createdDish.DishId,
                    name = createdDish.Name,
                    description = createdDish.Description,
                    image = createdDish.Image,
                    price = createdDish.Price,
                    snackPlaceId = createdDish.SnackPlaceId
                }
            });
        }

        [HttpPost("search-dishes")]
        public async Task<IActionResult> SearchDishes([FromBody] SearchDishRequest request)
        {
            var result = await _dishService.SearchDishesAsync(request);
            return Ok(result);
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllDishes()
        {
            var result = await _dishService.GetAllDishesAsync();
            return Ok(result);
        }

        [HttpGet("getById")]
        public async Task<IActionResult> GetDishById(Guid id)
        {
            var dish = await _dishService.GetDishByIdAsync(id);

            return Ok(new
            {
                status = 200,
                message = "Lấy thông tin món ăn thành công.",
                data = new
                {
                    dish.DishId,
                    dish.Name,
                    dish.Description,
                    dish.Image,
                    dish.Price,
                    dish.SnackPlaceId
                }
            });
        }

        [HttpGet("getBySnackPlace")]
        public async Task<IActionResult> GetDishesBySnackPlace([FromQuery] Guid snackPlaceId)
        {
            var dishes = await _dishService.GetDishesBySnackPlaceIdAsync(snackPlaceId);

            return Ok(new
            {
                status = 200,
                message = "Dishes from snack place retrieved successfully.",
                data = dishes.Select(d => new
                {
                    d.DishId,
                    d.Name,
                    d.Description,
                    d.Image,
                    d.Price,
                    d.Drink,
                    d.SnackPlaceId
                })
            });
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateDish([FromBody] UpdateDishDTO dto)
        {
            var (isSuccess, errorMessage, updatedDish) = await _dishService.UpdateDishAsync(dto);

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
                message = "Cập nhật món ăn thành công.",
                data = new
                {
                    updatedDish.DishId,
                    updatedDish.Name,
                    updatedDish.Description,
                    updatedDish.Image,
                    updatedDish.Price,
                    updatedDish.SnackPlaceId
                }
            });
        }

    }
}
