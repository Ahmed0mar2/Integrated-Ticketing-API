namespace GP.Application.DTOs.Admin
{
    public record AssignRoleRequest
    {
        public string Role { get; init; } = null!;
    }
}