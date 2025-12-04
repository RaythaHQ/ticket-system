namespace App.Application.Common.Exceptions;

public class InvalidApiKeyException : Exception
{
    public InvalidApiKeyException()
        : base() { }

    public InvalidApiKeyException(string message)
        : base(message) { }
}
