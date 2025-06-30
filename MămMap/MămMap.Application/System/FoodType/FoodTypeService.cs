using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.FoodType;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MamMap.Application.System.FoodType
{
    public class FoodTypeService : IFoodTypeService
    {
        private readonly MamMapDBContext _context;

        public FoodTypeService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<(bool isSuccess, string? errorMessage, FoodTypes? createdFoodType)> CreateFoodTypeAsync(FoodTypes foodType)
        {
            if (string.IsNullOrWhiteSpace(foodType.Description))
                return (false, "FoodType name is required.", null);

            var exists = await _context.FoodTypes.AnyAsync(f => f.Description == foodType.Description);
            if (exists)
                return (false, "FoodType already exists.", null);

            _context.FoodTypes.Add(foodType);
            await _context.SaveChangesAsync();

            return (true, null, foodType);
        }

        public async Task<object> SearchFoodTypesAsync(SearchFoodTypeRequest request)
        {
            if (request.PageNum <= 0) request.PageNum = 1;
            if (request.PageSize <= 0) request.PageSize = 10;

            var query = _context.FoodTypes.AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchKeyword))
            {
                query = query.Where(d =>
                    d.Description != null &&
                    d.Description.Contains(request.SearchKeyword));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(d => d.Status == request.Status.Value);
            }

            var total = await query.CountAsync();

            var type = await query
                .Skip((request.PageNum - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var pageData = type.Select(d => new
            {
                id = d.Id,
                name = d.Description,    
                status = d.Status
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy danh sách loại món ăn thành công.",
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

        public async Task<object> GetAllFoodTypesAsync()
        {
            var foodTypes = await _context.FoodTypes.ToListAsync();

            var foodTypeData = foodTypes.Select(f => new
            {
                foodTypeId = f.Id,
                name = f.Description,
                status = f.Status
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy tất cả loại thức ăn thành công.",
                data = foodTypeData
            };
        }

        public async Task<FoodTypes?> GetByIdAsync(Guid id)
        {
            return await _context.FoodTypes.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<(bool isSuccess, string? errorMessage)> UpdateTypeAsync(Guid id, string newDescription)
        {
            var type = await _context.FoodTypes.FirstOrDefaultAsync(d => d.Id == id);
            if (type == null)
                return (false, "Không tìm thấy loại món ăn.");

            type.Description = newDescription;
            _context.FoodTypes.Update(type);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.FoodTypes.FindAsync(id);
            if (entity == null || entity.Status == false)
                return false;

            entity.Status = false;
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
