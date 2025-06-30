using MamMap.Data.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(RoleManager<AspNetRoles> roleManager)
        {
            string[] roles = new[] { "Admin", "User", "Merchant" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new AspNetRoles { Name = role, NormalizedName = role.ToUpper() });
                }
            }
        }
    }
}
