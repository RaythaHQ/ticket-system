using Mediator;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;

namespace App.Application.AuditLogs.Commands;

public class ClearAllAuditLogs
{
    public record Command : LoggableRequest<CommandResponseDto<string>> { }

    public class Handler : IRequestHandler<Command, CommandResponseDto<string>>
    {
        private readonly IAppRawDbCommands _dbCommands;

        public Handler(IAppRawDbCommands dbCommands)
        {
            _dbCommands = dbCommands;
        }

        public async ValueTask<CommandResponseDto<string>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            await _dbCommands.ClearAuditLogsAsync(cancellationToken);

            return new CommandResponseDto<string>("All audit logs have been cleared successfully.");
        }
    }
}

