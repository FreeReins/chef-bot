using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <summary>
/// Builds the context for <c>dotnet ef migrations add</c>, which needs the model but never a database. The
/// connection string is a placeholder: nothing connects to it.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ChefBotDbContext>
{
    public ChefBotDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ChefBotDbContext>().UseNpgsql("Host=localhost;Database=chefbot").Options);
}
