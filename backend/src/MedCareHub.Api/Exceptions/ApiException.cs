namespace MedCareHub.Api.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }
    public object? Extra { get; }

    public ApiException(int statusCode, string title, string message, object? extra = null) : base(message)
    {
        StatusCode = statusCode;
        Title = title;
        Extra = extra;
    }
}

public sealed class NotFoundException : ApiException
{
    public NotFoundException(string message, object? extra = null)
        : base(StatusCodes.Status404NotFound, "Not Found", message, extra) { }
}

public sealed class ConflictException : ApiException
{
    public ConflictException(string message, object? extra = null)
        : base(StatusCodes.Status409Conflict, "Conflict", message, extra) { }
}

public sealed class ForbiddenException : ApiException
{
    public ForbiddenException(string message, object? extra = null)
        : base(StatusCodes.Status403Forbidden, "Forbidden", message, extra) { }
}

public sealed class BadRequestException : ApiException
{
    public BadRequestException(string message, object? extra = null)
        : base(StatusCodes.Status400BadRequest, "Bad Request", message, extra) { }
}