using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Dish;
using MamMap.ViewModels.System.Other;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Dish
{
    public class DishService : IDishService
    {
        private readonly MamMapDBContext _context;
        public DishService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<(bool isSuccess, string? errorMessage, Dishes? createdDish)> CreateDishAsync(Dishes dish)
        {
            var snackPlaceExists = await _context.SnackPlaces.AnyAsync(sp => sp.SnackPlaceId == dish.SnackPlaceId);
            if (!snackPlaceExists)
            {
                return (false, "SnackPlaceId không hợp lệ.", null);
            }

            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync();
            return (true, null, dish);
        }

        public async Task<object> SearchDishesAsync(SearchDishRequest request)
        {
            if (request.PageNum <= 0) request.PageNum = 1;
            if (request.PageSize <= 0) request.PageSize = 10;

            var query = _context.Dishes.AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchKeyword))
            {
                query = query.Where(d => d.Name.Contains(request.SearchKeyword));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(d => d.Status == request.Status.Value);
            }

            var total = await query.CountAsync();

            var dishes = await query
                .Skip((request.PageNum - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var pageData = dishes.Select(d => new
            {
                id = d.DishId,
                name = d.Name,
                description = d.Description,
                price = d.Price,
                isDrink = d.Drink,
                status = d.Status,
                image = d.Image
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy danh sách món ăn thành công.",
                data = new
                {
                    pageData,
                    pageInfo = new
                    {
                        pageNum = request.PageNum,
                        pageSize = request.PageSize,
                        total,
                        totalPages = (int)Math.Ceiling((double)total / request.PageSize)
                    }
                }
            };
        }

        public async Task<object> GetAllDishesAsync()
        {
            var dishes = await _context.Dishes.ToListAsync();

            var dishData = dishes.Select(d => new
            {
                dishId = d.DishId,
                name = d.Name,
                description = d.Description,
                image = d.Image,
                price = d.Price,
                snackPlaceId = d.SnackPlaceId
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy tất cả món ăn thành công.",
                data = dishData
            };
        }

        public async Task<Dishes?> GetDishByIdAsync(Guid id)
        {
            return await _context.Dishes.FindAsync(id);
        }

        public async Task<IEnumerable<Dishes>> GetDishesBySnackPlaceIdAsync(Guid snackPlaceId)
        {
            return await _context.Dishes
                .Where(d => d.SnackPlaceId == snackPlaceId && d.Status == true)
                .ToListAsync();
        }

        public async Task<(bool isSuccess, string? errorMessage, Dishes? updatedDish)> UpdateDishAsync(UpdateDishDTO dto)
        {
            var dish = await _context.Dishes.FindAsync(dto.DishId);
            if (dish == null)
                return (false, "Món ăn không tồn tại.", null);

            if (!string.IsNullOrWhiteSpace(dto.Name))
                dish.Name = dto.Name;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                dish.Description = dto.Description;

            if (!string.IsNullOrWhiteSpace(dto.Image))
                dish.Image = dto.Image;

            if(dto.Price != null)
                dish.Price = (int)dto.Price;

            if (dto.SnackPlaceId != Guid.Empty && dto.SnackPlaceId != dish.SnackPlaceId)
            {
                var snackPlaceExists = await _context.SnackPlaces.AnyAsync(sp => sp.SnackPlaceId == dto.SnackPlaceId);
                if (!snackPlaceExists)
                    return (false, "SnackPlaceId không hợp lệ.", null);

                dish.SnackPlaceId = dto.SnackPlaceId;
            }

            await _context.SaveChangesAsync();
            return (true, null, dish);
        }

        public async Task<(bool isSuccess, string? errorMessage)> DeleteDishAsync(Guid id)
        {
            var dish = await _context.Dishes.FirstOrDefaultAsync(d => d.DishId == id && d.Status != false);
            if (dish == null)
                return (false, "Không tìm thấy món ăn.");

            dish.Status = false;
            _context.Dishes.Update(dish);
            await _context.SaveChangesAsync();

            return (true, null);
        }
    }
}
