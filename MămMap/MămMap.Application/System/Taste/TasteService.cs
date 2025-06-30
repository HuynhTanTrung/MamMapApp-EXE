using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Taste;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MamMap.Application.System.Taste
{
    public class TasteService : ITasteService
    {
        private readonly MamMapDBContext _context;

        public TasteService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<(bool isSuccess, string? errorMessage, Tastes? createdTaste)> CreateTasteAsync(Tastes taste)
        {
            if (string.IsNullOrWhiteSpace(taste.Description))
                return (false, "Tên Taste không được để trống.", null);

            taste.Id = Guid.NewGuid();
            _context.Tastes.Add(taste);
            await _context.SaveChangesAsync();
            return (true, null, taste);
        }

        public async Task<object> SearchTastesAsync(SearchTasteRequest request)
        {
            if (request.PageNum <= 0) request.PageNum = 1;
            if (request.PageSize <= 0) request.PageSize = 10;

            var query = _context.Tastes.AsQueryable();

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

            var taste = await query
                .Skip((request.PageNum - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var pageData = taste.Select(d => new
            {
                id = d.Id,
                name = d.Description,
                status = d.Status
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy danh sách vị món ăn thành công.",
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

        public async Task<object> GetAllTastesAsync()
        {
            var tastes = await _context.Tastes.ToListAsync();

            var data = tastes.Select(t => new
            {
                tasteId = t.Id,
                name = t.Description,
                status = t.Status
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy tất cả vị món ăn thành công.",
                data = data
            };
        }

        public async Task<Tastes?> GetByIdAsync(Guid id)
        {
            return await _context.Tastes.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<(bool isSuccess, string? errorMessage)> UpdateTasteAsync(Guid id, string newDescription)
        {
            var taste = await _context.Tastes.FirstOrDefaultAsync(d => d.Id == id);
            if (taste == null)
                return (false, "Không tìm thấy vị món ăn.");

            taste.Description = newDescription;
            _context.Tastes.Update(taste);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.Tastes.FindAsync(id);
            if (entity == null || entity.Status == false)
                return false;

            entity.Status = false;
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
