using Project.Domain.Enums;

namespace Project.Domain.Entities;

public class WorkRequest
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public RequestStatus Status { get; set; }

    public Guid CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? CheckedByUserId { get; set; }

    public DateTime? CheckedAt { get; set; }

    public string? CheckerRemarks { get; set; }

    public Guid? AuthorizedByUserId { get; set; }

    public DateTime? AuthorizedAt { get; set; }

    public string? AuthorizerRemarks { get; set; }
}
