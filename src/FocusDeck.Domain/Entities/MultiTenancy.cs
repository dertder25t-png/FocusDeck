namespace FocusDeck.Domain.Entities;

/// <summary>
/// Represents a tenant/organization in the FocusDeck multi-tenant system.
/// </summary>
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserTenant> Members { get; set; } = new List<UserTenant>();
    public ICollection<TenantInvite> Invites { get; set; } = new List<TenantInvite>();
}

/// <summary>
/// Represents a user in the multi-tenant system.
/// </summary>
public class TenantUser
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Picture { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<UserTenant> Tenants { get; set; } = new List<UserTenant>();
}

/// <summary>
/// Join table between <see cref="Tenant"/> and <see cref="TenantUser"/>.
/// </summary>
public class UserTenant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public TenantRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public TenantUser User { get; set; } = null!;
}

/// <summary>
/// Roles that a tenant member can have.
/// </summary>
public enum TenantRole
{
    Member = 0,
    Admin = 1,
    Owner = 2
}

/// <summary>
/// Invitation to join a tenant.
/// </summary>
public class TenantInvite
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public TenantRole Role { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string? AcceptedByUserId { get; set; }

    public Tenant Tenant { get; set; } = null!;
}

/// <summary>
/// Marker for entities that must always be associated with a tenant.
/// </summary>
public interface IMustHaveTenant
{
    Guid TenantId { get; set; }
}
