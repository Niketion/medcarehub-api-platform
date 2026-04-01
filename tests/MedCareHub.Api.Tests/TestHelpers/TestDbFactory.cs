using MedCareHub.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MedCareHub.Api.Tests.TestHelpers;

public static class TestDbFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        return db;
    }
}