using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Diet;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MamMap.Application.System.Diet
{
    public class DietService : IDietService
    {
        private readonly MamMapDBContext _context;

        public DietService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<(bool isSuccess, string? errorMessage, Diets? createdDiet)> CreateDietAsync(Diets diet)
        {
            if (string.IsNullOrWhiteSpace(diet.Description))
                return (false, "Diet name is required.", null);

            var exists = await _context.Diets.AnyAsync(d => d.Description == diet.Description);
            if (exists)
                return (false, "Diet already exists.", null);

            _context.Diets.Add(diet);
            await _context.SaveChangesAsync();

            return (true, null, diet);
        }

        public async Task<object> SearchDietsAsync(SearchDietRequest request)
        {
            if (request.PageNum <= 0) request.PageNum = 1;
            if (request.PageSize <= 0) request.PageSize = 10;

            var query = _context.Diets.AsQueryable();

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

            var diets = await query
                .Skip((request.PageNum - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var pageData = diets.Select(d => new
            {
                id = d.Id,
                name = d.Description,
                status = d.Status
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy danh sách chế độ ăn thành công.",
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
        public async Task<object> GetAllDietsAsync()
        {
            var diets = await _context.Diets.ToListAsync();

            var dietData = diets.Select(d => new
            {
                dietId = d.Id,
                name = d.Description,
                status = d.Status
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy tất cả chế độ ăn thành công.",
                data = dietData
            };
        }

        public async Task<Diets?> GetDietByIdAsync(Guid id)
        {
            return await _context.Diets.FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<(bool isSuccess, string? errorMessage)> UpdateDietAsync(Guid id, string newDescription)
        {
            var diet = await _context.Diets.FirstOrDefaultAsync(d => d.Id == id);
            if (diet == null)
                return (false, "Không tìm thấy chế độ ăn.");

            diet.Description = newDescription;
            _context.Diets.Update(diet);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool isSuccess, string? errorMessage)> DeleteDietAsync(Guid id)
        {
            var diet = await _context.Diets.FirstOrDefaultAsync(d => d.Id == id && d.Status != false);
            if (diet == null)
                return (false, "Không tìm thấy chế độ ăn.");

            diet.Status = false;
            _context.Diets.Update(diet);
            await _context.SaveChangesAsync();

            return (true, null);
        }
    }
}
