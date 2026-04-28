using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace SibGost.Core.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SibGostDbContext>
    {
        public SibGostDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SibGostDbContext>();

            var dbPath = @"F:\Колледж\ВиПКС\SibGost\DataBase\SibGost.db";

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath));

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            return new SibGostDbContext(optionsBuilder.Options);
        }
    }
}
