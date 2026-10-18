using Project.Domain.Entities;

namespace Project.Application.Common;

internal static class RequestMappings
{
    public static RequestDto ToDto(this WorkRequest request) =>
        new(
            request.Id,
            request.Title,
            request.Description,
            request.Status.ToString(),
            request.CreatedByUserId,
            request.CreatedAt,
            request.UpdatedAt,
            request.CheckedAt,
            request.CheckerRemarks,
            request.AuthorizedAt,
            request.AuthorizerRemarks);
}
