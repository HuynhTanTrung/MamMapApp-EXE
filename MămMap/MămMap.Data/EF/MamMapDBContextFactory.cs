using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Data.EF
{
    public class MamMapDBContextFactory : IDesignTimeDbContextFactory<MamMapDBContext>
    {
        public MamMapDBContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("MamMapDb");

            var optionsBuilder = new DbContextOptionsBuilder<MamMapDBContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new MamMapDBContext(optionsBuilder.Options);
        }
    }
}
