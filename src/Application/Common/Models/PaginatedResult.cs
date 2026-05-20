namespace Application.Common.Models;

public record PaginatedResult<T>(
    List<T> Data,
    int Total,
    int Page,
    int PageSize);
