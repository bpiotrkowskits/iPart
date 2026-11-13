using Microsoft.EntityFrameworkCore;

namespace iPart.Infrastructure;

public sealed class IPartDbContext(DbContextOptions<IPartDbContext> options) : DbContext(options)
{
}
