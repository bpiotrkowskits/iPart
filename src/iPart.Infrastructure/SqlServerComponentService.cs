using System.Security.Principal;
using iPart.Core.Components;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace iPart.Infrastructure;

public sealed class WindowsCurrentUser : ICurrentUser
{
    public string GetAuthor()
    {
        if (!OperatingSystem.IsWindows()) return Environment.UserName;
        using var identity = WindowsIdentity.GetCurrent();
        return identity.Name;
    }
}

public sealed class IPartContextFactory(DbContextOptions<IPartDbContext> options) : IDbContextFactory<IPartDbContext>
{
    public IPartDbContext CreateDbContext() => new(options);
}

public sealed class SqlServerComponentService(IDbContextFactory<IPartDbContext> contexts, ICurrentUser currentUser) : IComponentService
{
    public async Task<Component?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        return await Aggregate(context).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ComponentSaveResult> SaveAsync(Component component, Guid? expectedVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            ComponentValidator.Validate(component);
        }
        catch (ComponentValidationException exception)
        {
            return Failure(component.Id, SaveFailure.Validation, exception.Message);
        }

        await using var context = await contexts.CreateDbContextAsync(cancellationToken);
        // A caller may supply a transaction (e.g. rollback-only SQL integration tests).
        // Otherwise this operation owns and commits its own transaction as before.
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            var existing = await Aggregate(context).SingleOrDefaultAsync(x => x.Id == component.Id, cancellationToken);
            if ((expectedVersion is null && existing is not null) ||
                (expectedVersion is not null && (existing is null || existing.Version != expectedVersion)))
                return Concurrent(component.Id);

            if (await context.Components.AnyAsync(x => x.Id != component.Id && x.Mpn == component.Mpn &&
                    x.ManufacturerId == component.ManufacturerId && x.CustomerId == component.CustomerId, cancellationToken))
                return Duplicate(component.Id);
            if (component.SubcategoryId is { } subcategory && !await context.Subcategories.AnyAsync(
                    x => x.Id == subcategory && x.CategoryId == component.CategoryId, cancellationToken))
                return Failure(component.Id, SaveFailure.InvalidReference, "Podkategoria nie należy do wybranej kategorii.");

            var changes = ComponentChanges.Between(existing, component);
            if (changes.Count == 0)
                return new(true, component.Id, existing!.Version, SaveFailure.None, null);
            var author = currentUser.GetAuthor();
            if (string.IsNullOrWhiteSpace(author) || author.Length > 256)
                return Failure(component.Id, SaveFailure.Validation, "Nie można ustalić poprawnego autora zapisu.");

            SetDependentIds(component);
            Component saved;
            if (existing is null)
            {
                saved = component;
                context.Components.Add(saved);
            }
            else
            {
                saved = existing;
                context.Entry(saved).CurrentValues.SetValues(component);
                // Preserve the loaded version as the expected value in SQL's UPDATE predicate.
                context.Entry(saved).Property(x => x.Version).OriginalValue = expectedVersion!.Value;
                foreach (var property in ComponentValidator.ParameterProperties)
                {
                    var oldParameters = property.GetValue(saved);
                    var newParameters = property.GetValue(component);
                    if (oldParameters is not null && newParameters is not null)
                        context.Entry(oldParameters).CurrentValues.SetValues(newParameters);
                    else if (oldParameters is not null)
                    {
                        context.Remove(oldParameters);
                        property.SetValue(saved, null);
                    }
                    else if (newParameters is not null)
                    {
                        property.SetValue(saved, newParameters);
                        context.Add(newParameters);
                    }
                }
                SyncCollection(context, saved.Links, component.Links, x => x.Id);
                SyncCollection(context, saved.Qualifications, component.Qualifications, x => x.QualificationId);
            }

            // Force an aggregate UPDATE even for relationship-only or parameter-only edits.
            saved.Version = Guid.NewGuid();
            if (existing is not null) context.Entry(saved).Property(x => x.Version).IsModified = true;
            var operationId = Guid.NewGuid();
            var utc = DateTimeOffset.UtcNow;
            context.ComponentHistory.AddRange(changes.Select(change => new ComponentHistoryEntry
            {
                OperationId = operationId,
                ComponentId = saved.Id,
                Author = author,
                OccurredAtUtc = utc,
                Operation = existing is null ? HistoryOperation.Created : HistoryOperation.Updated,
                FieldPath = change.Path,
                PreviousValue = change.PreviousValue,
                NewValue = change.NewValue
            }));
            await context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new(true, saved.Id, saved.Version, SaveFailure.None, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Concurrent(component.Id);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Duplicate(component.Id);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            return Failure(component.Id, SaveFailure.InvalidReference, "Zapis narusza reguły danych lub wskazuje nieistniejącą pozycję słownika. Odśwież słowniki i sprawdź dane.");
        }
    }

    private static IQueryable<Component> Aggregate(IPartDbContext context) => context.Components
        .Include(x => x.Resistor).Include(x => x.Capacitor).Include(x => x.Inductor).Include(x => x.Led)
        .Include(x => x.Connector).Include(x => x.Crystal).Include(x => x.Pads)
        .Include(x => x.Qualifications).Include(x => x.Links).AsSplitQuery();

    private static void SetDependentIds(Component component)
    {
        foreach (var property in ComponentValidator.ParameterProperties)
            if (property.GetValue(component) is IComponentDependent dependent) dependent.ComponentId = component.Id;
        foreach (var dependent in component.Links.Cast<IComponentDependent>().Concat(component.Qualifications))
            dependent.ComponentId = component.Id;
    }

    private static void SyncCollection<T, TKey>(IPartDbContext context, List<T> tracked, List<T> incoming, Func<T, TKey> key)
        where T : class where TKey : notnull
    {
        var requested = incoming.ToDictionary(key);
        foreach (var item in tracked.ToArray())
        {
            if (requested.Remove(key(item), out var replacement)) context.Entry(item).CurrentValues.SetValues(replacement);
            else { context.Remove(item); tracked.Remove(item); }
        }
        foreach (var item in requested.Values) { tracked.Add(item); context.Add(item); }
    }

    private static ComponentSaveResult Failure(Guid id, SaveFailure failure, string message) => new(false, id, null, failure, message);
    private static ComponentSaveResult Concurrent(Guid id) => Failure(id, SaveFailure.ConcurrentEdit, "Komponent został zmieniony przez innego użytkownika. Wczytaj aktualną wersję przed ponownym zapisem.");
    private static ComponentSaveResult Duplicate(Guid id) => Failure(id, SaveFailure.DuplicateBusinessKey, "Istnieje już komponent o takim MPN, producencie i kliencie (również przy braku producenta lub klienta). Dane nie zostały scalone.");
}
