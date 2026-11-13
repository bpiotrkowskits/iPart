using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace iPart.Infrastructure;

public sealed class IPartDbContextFactory : IDesignTimeDbContextFactory<IPartDbContext>
{
    public IPartDbContext CreateDbContext(string[] args)
    {
        var option = Array.IndexOf(args, "--settings");
        var settingsPath = option >= 0 && option + 1 < args.Length
            ? ResolveSettingsPath(args[option + 1])
            : Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var config = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(settingsPath)!)
            .AddJsonFile(Path.GetFileName(settingsPath), optional: false).Build();
        using var lifetime = config as IDisposable;
        var connectionString = config.GetConnectionString("IPartDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Brak ConnectionStrings:IPartDatabase w pliku konfiguracji. Podaj -- --settings <ścieżka do appsettings.json>.");
        return new IPartDbContext(new DbContextOptionsBuilder<IPartDbContext>().UseSqlServer(connectionString).Options);
    }

    private static string ResolveSettingsPath(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        // dotnet-ef runs the factory from the project directory. Resolve repository-relative arguments consistently.
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "iPart.slnx")))
                return Path.GetFullPath(path, directory.FullName);
        return Path.GetFullPath(path);
    }
}
