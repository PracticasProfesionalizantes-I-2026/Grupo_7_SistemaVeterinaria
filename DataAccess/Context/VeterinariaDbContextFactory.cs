using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataAccess.Context;

public class VeterinariaDbContextFactory : IDesignTimeDbContextFactory<VeterinariaDbContext>
{
    public VeterinariaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VeterinariaDbContext>();
        optionsBuilder.UseSqlite("Data Source=veterinaria.db");

        return new VeterinariaDbContext(optionsBuilder.Options);
    }
}
