namespace App.Domain.Exceptions;

public class SortOrderNotFoundException : Exception
{
    public SortOrderNotFoundException(string developerName)
        : base($"Sort order \"{developerName}\" is unknown.") { }
}
