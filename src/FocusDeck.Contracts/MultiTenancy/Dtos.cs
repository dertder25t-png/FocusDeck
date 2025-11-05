namespace FocusDeck.Contracts.MultiTenancy;

public record CreateOrganizationRequest(string Name, string Slug);

public record OrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    DateTime CreatedAt,
    int MemberCount,
    string UserRole
);

public record OrgMemberDto(
    string UserId,
    string Email,
    string Name,
    string? Picture,
    string Role,
    DateTime JoinedAt
);

public record CreateInviteRequest(string Email, string Role);

public record InviteDto(
    Guid Id,
    string Email,
    string Role,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsExpired,
    bool IsAccepted
);

public record AcceptInviteRequest(string Token);

public record UpdateMemberRoleRequest(string Role);
