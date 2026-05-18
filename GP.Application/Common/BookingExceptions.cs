namespace GP.Application.Common;

public sealed class CartValidationException : Exception
{
    public string? ErrorCode { get; }

    public CartValidationException(string message, string? errorCode = null) : base(message)
    {
        ErrorCode = errorCode;
    }
}

public sealed class CartConcurrencyException : Exception
{
    public string? ErrorCode { get; }

    public CartConcurrencyException(
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
