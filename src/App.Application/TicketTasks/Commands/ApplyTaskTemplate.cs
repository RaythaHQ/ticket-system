using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketTasks.Commands;

public class ApplyTaskTemplate
{
    public record Command : LoggableRequest<CommandResponseDto<List<TicketTaskDto>>>
    {
        public long TicketId { get; init; }
        public required ShortGuid TemplateId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).GreaterThan(0);
            RuleFor(x => x.TemplateId).NotEmpty();
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<List<TicketTaskDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<List<TicketTaskDto>>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var ticket = await _db.Tickets
                .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);
            if (ticket == null)
                return new CommandResponseDto<List<TicketTaskDto>>("TicketId", "Ticket not found.");

            var templateId = request.TemplateId.Guid;
            var template = await _db.TaskTemplates.AsNoTracking()
                .Include(t => t.Items.OrderBy(i => i.SortOrder))
                .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive, cancellationToken);

            if (template == null)
                return new CommandResponseDto<List<TicketTaskDto>>("TemplateId", "Task template not found or is inactive.");

            // Calculate starting sort order
            var maxSortOrder = await _db.TicketTasks
                .Where(t => t.TicketId == request.TicketId)
                .MaxAsync(t => (int?)t.SortOrder, cancellationToken) ?? 0;

            // Create tasks from template items, maintaining dependency mapping
            var itemIdToTaskMap = new Dictionary<Guid, TicketTask>();
            var createdTasks = new List<TicketTask>();

            foreach (var item in template.Items)
            {
                maxSortOrder++;
                var task = new TicketTask
                {
                    Id = Guid.NewGuid(),
                    TicketId = request.TicketId,
                    Title = item.Title,
                    Status = TicketTaskStatus.OPEN,
                    SortOrder = maxSortOrder,
                    CreatedByStaffId = _currentUser.UserIdAsGuid,
                };

                task.AddDomainEvent(new TicketTaskCreatedEvent(task));
                itemIdToTaskMap[item.Id] = task;
                createdTasks.Add(task);
                _db.TicketTasks.Add(task);
            }

            // Map template item dependencies to actual task dependencies
            foreach (var item in template.Items)
            {
                if (item.DependsOnItemId.HasValue && itemIdToTaskMap.ContainsKey(item.DependsOnItemId.Value))
                {
                    var dependentTask = itemIdToTaskMap[item.Id];
                    var dependsOnTask = itemIdToTaskMap[item.DependsOnItemId.Value];
                    dependentTask.DependsOnTaskId = dependsOnTask.Id;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            // Return DTOs
            var dtos = createdTasks.Select(t => TicketTaskDto.MapFrom(t)).ToList();
            return new CommandResponseDto<List<TicketTaskDto>>(dtos);
        }
    }
}
