using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RGRE.ERP.Application.Abstractions;
using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Infrastructure.Persistence;

/// <summary>
/// Stamps <c>CreatedBy/CreatedAt/ModifiedBy/ModifiedAt</c> on every
/// <see cref="AuditableEntity"/> on save — the EF equivalent of the audit
/// columns the legacy MHK tables carried on every row.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserContext _userContext;
    private readonly IClock _clock;

    public AuditSaveChangesInterceptor(ICurrentUserContext userContext, IClock clock)
    {
        _userContext = userContext;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData?.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData?.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
            return;

        var nowUtc = _clock.UtcNow;
        var userId = _userContext.UserId;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(AuditableEntity.CreatedBy)).CurrentValue = userId;
                entry.Property(nameof(AuditableEntity.CreatedAt)).CurrentValue = nowUtc;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(AuditableEntity.ModifiedBy)).CurrentValue = userId;
                entry.Property(nameof(AuditableEntity.ModifiedAt)).CurrentValue = nowUtc;
            }
        }
    }
}
