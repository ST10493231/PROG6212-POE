namespace RaceDay.Common;

/// Consistent error body returned by every controller so API consumers
/// (and the Part 3 MVC app) can rely on one shape: { "message": "..." }.

public class ApiError
{
    public string Message { get; set; } = string.Empty;

    public ApiError(string message) => Message = message;
}
