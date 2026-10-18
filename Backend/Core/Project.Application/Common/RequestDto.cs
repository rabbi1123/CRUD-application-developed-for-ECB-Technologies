namespace Project.Application.Common;

public record RequestDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CheckedAt,
    string? CheckerRemarks,
    DateTime? AuthorizedAt,
    string? AuthorizerRemarks);
