using MamMap.Data.EF;
using MamMap.Data.Entities;
using MamMap.ViewModels.System.Package;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Package
{
    public class PackageService : IPackageService
    {
        private readonly MamMapDBContext _context;

        public PackageService(MamMapDBContext context)
        {
            _context = context;
        }

        public async Task<(bool isSuccess, string? errorMessage, PremiumPackage? package)> CreateAsync(CreatePremiumPackageDTO dto)
        {
            var package = new PremiumPackage
            {
                Name = dto.Name,
                Price = dto.Price
            };

            foreach (var desc in dto.Descriptions)
            {
                package.Descriptions.Add(new PackageDescription { Description = desc });
            }

            _context.PremiumPackages.Add(package);
            await _context.SaveChangesAsync();

            return (true, null, package);
        }

        public async Task<object> GetAllAsync()
        {
            var packages = await _context.PremiumPackages
                .Include(p => p.Descriptions)
                .Where(p => p.Status)
                .ToListAsync();

            var data = packages.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                price = p.Price,
                descriptions = p.Descriptions.Select(d => d.Description).ToList()
            });

            return new
            {
                status = 200,
                message = "Lấy danh sách gói premium thành công.",
                data
            };
        }

        public async Task<PremiumPackage?> GetByIdAsync(int id)
        {
            return await _context.PremiumPackages
                .Include(p => p.Descriptions)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status);
        }

        public async Task<object> SearchPackageAsync(SearchPackageRequest request)
        {
            if (request.PageNum <= 0) request.PageNum = 1;
            if (request.PageSize <= 0) request.PageSize = 10;

            var query = _context.PremiumPackages.AsQueryable()
                .Include(p => p.Descriptions)
                .Where(p => p.Status)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchKeyword))
            {
                query = query.Where(d =>
                    d.Name != null &&
                    d.Name.Contains(request.SearchKeyword));
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
                name = d.Name,
                status = d.Status,
                price = d.Price,
                descriptions = d.Descriptions.Select(d => d.Description).ToList()
            }).ToList();

            return new
            {
                status = 200,
                message = "Lấy danh sách gói premium thành công.",
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

        public async Task<(bool isSuccess, string? errorMessage)> UpdatePremiumPackageAsync(int packageId, UpdatePremiumPackageDTO dto)
        {
            var existing = await _context.PremiumPackages
                .Include(p => p.Descriptions)
                .FirstOrDefaultAsync(p => p.Id == packageId && p.Status);

            if (existing == null)
                return (false, "Không tìm thấy gói Premium hợp lệ.");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                existing.Name = dto.Name;

            if (dto.Price.HasValue)
                existing.Price = dto.Price.Value;

            if (dto.Descriptions != null && dto.Descriptions.Count > 0)
            {
                _context.PackageDescriptions.RemoveRange(existing.Descriptions);
                foreach (var desc in dto.Descriptions)
                {
                    existing.Descriptions.Add(new PackageDescription
                    {
                        PremiumPackageId = existing.Id,
                        Description = desc
                    });
                }
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var package = await _context.PremiumPackages.FindAsync(id);
            if (package == null || !package.Status) return false;

            package.Status = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
