using CSharpVitamins;
using Mediator;
using App.Application.Common.Attributes;

namespace App.Application.Common.Models;

public interface ILoggableRequest { }

public abstract record LoggableRequest<T> : IRequest<T>, ILoggableRequest
{
    public virtual string GetLogName()
    {
        return this.GetType()
            .FullName.Replace("App.Application.", string.Empty)
            .Replace("+Command", string.Empty);
    }
}

public interface ILoggableEntityRequest { }

public abstract record LoggableEntityRequest<T> : LoggableRequest<T>, ILoggableEntityRequest
{
    [ExcludePropertyFromOpenApiDocs]
    public virtual ShortGuid Id { get; init; }
}
