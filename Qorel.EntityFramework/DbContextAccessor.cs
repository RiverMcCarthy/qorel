using Microsoft.EntityFrameworkCore;

namespace Qorel.EntityFramework;

/// <summary>
/// The app's <see cref="DbContext"/>, resolved for the current scope. Lets the pipeline be registered once for every
/// model, without knowing the app's context type.
/// </summary>
internal sealed class DbContextAccessor(DbContext context)
{
    public DbContext Context { get; } = context;
}
