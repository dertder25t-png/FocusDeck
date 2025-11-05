namespace FocusDeck.Domain.Entities;

/// <summary>
/// Represents an organization in the multi-tenant system
/// </summary>
public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // URL-friendly identifier
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<OrgUser> Members { get; set; } = new List<OrgUser>();
    public ICollection<Invite> Invites { get; set; } = new List<Invite>();
}

/// <summary>
/// Represents a user in the system
/// </summary>
public class User
{
    public string Id { get; set; } = string.Empty; // Email or OAuth sub
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Picture { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public ICollection<OrgUser> Organizations { get; set; } = new List<OrgUser>();
}

/// <summary>
/// Join table for many-to-many relationship between users and organizations
/// </summary>
public class OrgUser
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public OrgRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
}

/// <summary>
/// Roles within an organization
/// </summary>
public enum OrgRole
{
    Member = 0,
    Admin = 1,
    Owner = 2
}

/// <summary>
/// Invitation to join an organization
/// </summary>
public class Invite
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public OrgRole Role { get; set; }
    public string Token { get; set; } = string.Empty; // Unique token for accepting invite
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string? AcceptedByUserId { get; set; }
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
}
