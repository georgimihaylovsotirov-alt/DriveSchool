using DriveSchool.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveSchool.Data;

public sealed class DriveSchoolDbContextFactory : IDesignTimeDbContextFactory<DriveSchoolDbContext>
{
    public DriveSchoolDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING")
            ?? "Server=localhost;Database=DriveSchoolDesignTime;Integrated Security=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<DriveSchoolDbContext>()
            .UseSqlServer(connection)
            .Options;
        return new DriveSchoolDbContext(options);
    }
}
