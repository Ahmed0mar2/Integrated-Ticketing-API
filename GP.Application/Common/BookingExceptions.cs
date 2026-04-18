namespace GP.Application.Common;

public sealed class CartValidationException : Exception
{
    public CartValidationException(string message) : base(message)
    {
    }
}

public sealed class CartConcurrencyException : Exception
{
    public CartConcurrencyException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
