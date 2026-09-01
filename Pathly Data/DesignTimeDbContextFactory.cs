using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pathly_Data;

namespace Pathly_Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // The Azure SQL connection string comes from the CONNECTIONSTRINGS__PATHLYCONNECTION
            // environment variable (set in Azure App Service configuration, and locally on the
            // dev machine via user secrets or a machine-level env var). Keeping the secret out of
            // source control: this file is committed, the password is not.
            var connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__PATHLYCONNECTION")
                ?? "Server=tcp:pathlyserver.database.windows.net,1433;Initial Catalog=pathlydb;Persist Security Info=False;User ID=clearance;Password=CHANGE_ME;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";

            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}