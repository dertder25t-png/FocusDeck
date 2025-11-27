namespace FocusDeck.Contracts.MultiTenancy;

public record CreateTenantRequest(string Name, string Slug);

public record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    DateTime CreatedAt,
    int MemberCount,
    string UserRole
);

public record CurrentTenantDto(
    Guid Id,
    string Name,
    string Slug,
    string UserRole,
    int MemberCount
);

public record TenantMemberDto(
    string UserId,
    string Email,
    string Name,
    string? Picture,
    string Role,
    DateTime JoinedAt
);

public record CreateTenantInviteRequest(string Email, string Role);

public record TenantInviteDto(
    Guid Id,
    string Email,
    string Role,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsExpired,
    bool IsAccepted
);

public record AcceptTenantInviteRequest(string Token);

public record UpdateTenantMemberRoleRequest(string Role);
