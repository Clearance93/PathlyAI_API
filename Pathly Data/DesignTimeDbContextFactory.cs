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

            // The SQL Server connection string comes from the CONNECTIONSTRINGS__PATHLYCONNECTION
            // environment variable (set by the host's configuration, and locally on the dev
            // machine via user secrets or a machine-level env var). Keeping the secret out of
            // source control: this file is committed, the password is not.
            var connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__PATHLYCONNECTION")
                ?? "Server=localhost;Database=pathlyDB;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True";

            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
