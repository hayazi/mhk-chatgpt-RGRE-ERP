using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RGRE.ERP.Infrastructure.Persistence;

namespace RGRE.ERP.Web.Components.Services;

/// <summary>
/// Backing store for persisted authentication tickets (one row per user).
/// Kept in a separate small DbContext so the main ERP model stays clean.
/// </summary>
public sealed class WebAuthDbContext : DbContext
{
    public WebAuthDbContext(DbContextOptions<WebAuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<PersistedAuthTicket> AuthTickets => Set<PersistedAuthTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PersistedAuthTicket>(b =>
        {
            b.ToTable("AuthTickets");
            b.HasKey(t => t.UserId);
            b.Property(t => t.TicketJson).HasMaxLength(16000).IsRequired();
        });
    }

    /// <summary>Stores (or replaces) the persisted ticket for the user.</summary>
    public async Task PersistTicketAsync(
        Guid userId,
        ClaimsPrincipal principal,
        DateTimeOffset expiresUtc,
        CancellationToken cancellationToken = default)
    {
        var record = AuthTicketRecord.FromPrincipal(principal);
        var json = System.Text.Json.JsonSerializer.Serialize(record);

        var ticket = await AuthTickets.FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);

        if (ticket is null)
        {
            ticket = new PersistedAuthTicket { UserId = userId };
            AuthTickets.Add(ticket);
        }

        ticket.TicketJson = json;
        ticket.ExpiresUtc = expiresUtc.UtcDateTime;

        await SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveTicketAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var ticket = await AuthTickets.FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);

        if (ticket is not null)
        {
            AuthTickets.Remove(ticket);
            await SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed class PersistedAuthTicket
{
    public Guid UserId { get; set; }

    public string TicketJson { get; set; } = null!;

    public DateTime ExpiresUtc { get; set; }
}

/// <summary>JSON-friendly snapshot of a claims principal.</summary>
public sealed record AuthTicketRecord(
    string AuthenticationType,
    string Name,
    List<AuthClaimRecord> Claims)
{
    public static AuthTicketRecord FromPrincipal(ClaimsPrincipal principal) => new(
        principal.Identity?.AuthenticationType ?? CookieAuthenticationDefaults.AuthenticationScheme,
        principal.Identity?.Name ?? string.Empty,
        principal.Claims.Select(c => new AuthClaimRecord(c.Type, c.Value)).ToList());

    public ClaimsPrincipal ToPrincipal()
        => new(new ClaimsIdentity(
            Claims.Select(c => new Claim(c.Type, c.Value)),
            AuthenticationType,
            ClaimTypes.Name,
            ClaimTypes.Role));
}

public sealed record AuthClaimRecord(string Type, string Value);
