using App.Domain.Entities;

namespace App.Application.Common.Interfaces;

public interface IBackgroundTaskDb
{
    BackgroundTask DequeueBackgroundTask();
}
