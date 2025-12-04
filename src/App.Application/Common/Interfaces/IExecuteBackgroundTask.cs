using System.Text.Json;

namespace App.Application.Common.Interfaces;

public interface IExecuteBackgroundTask
{
    Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken);
}
