namespace Project.Application.Common;

public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, string Role);

public record RegisterResponse(Guid Id, string FullName, string Email, string Role);

public record DashboardResponse(string Message);
