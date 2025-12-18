using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.BackgroundTasks;

public class QueuedHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(
        IServiceProvider serviceProvider,
        ILogger<QueuedHostedService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await BackgroundProcessing(stoppingToken);
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Use async delay instead of blocking Thread.Sleep
            await Task.Delay(1000, stoppingToken);

            BackgroundTask? dqTask = null;

            // Create a scope just for dequeuing
            using (var dequeueScope = _serviceProvider.CreateScope())
            {
                var taskQueue =
                    dequeueScope.ServiceProvider.GetRequiredService<IBackgroundTaskQueue>();
                dqTask = await taskQueue.DequeueAsync(stoppingToken);
            }

            if (dqTask == null)
                continue;

            // Create a fresh scope for each task to ensure clean DbContext
            using (var taskScope = _serviceProvider.CreateScope())
            {
                var db = taskScope.ServiceProvider.GetRequiredService<IAppDbContext>();

                // Use async FirstOrDefaultAsync instead of synchronous First
                var backgroundTask = await db.BackgroundTasks.FirstOrDefaultAsync(
                    p => p.Id == dqTask.Id,
                    stoppingToken
                );

                if (backgroundTask == null)
                {
                    _logger.LogWarning("Background task {TaskId} not found in database", dqTask.Id);
                    continue;
                }

                try
                {
                    var taskType = Type.GetType(backgroundTask.Name);
                    if (taskType == null)
                    {
                        throw new InvalidOperationException(
                            $"Could not resolve type: {backgroundTask.Name}"
                        );
                    }

                    var scopedProcessingService =
                        taskScope.ServiceProvider.GetRequiredService(taskType)
                        as IExecuteBackgroundTask;

                    if (scopedProcessingService == null)
                    {
                        throw new InvalidOperationException(
                            $"Service {taskType.Name} does not implement IExecuteBackgroundTask"
                        );
                    }

                    await scopedProcessingService.Execute(
                        backgroundTask.Id,
                        JsonSerializer.Deserialize<JsonElement>(backgroundTask.Args),
                        stoppingToken
                    );
                    backgroundTask.Status = BackgroundTaskStatus.Complete;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error executing background task {TaskId} ({TaskName})",
                        backgroundTask.Id,
                        backgroundTask.Name
                    );
                    backgroundTask.Status = BackgroundTaskStatus.Error;
                    backgroundTask.ErrorMessage = ex.Message;
                }

                backgroundTask.PercentComplete = 100;
                backgroundTask.CompletionTime = DateTime.UtcNow;
                db.BackgroundTasks.Update(backgroundTask);
                await db.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
