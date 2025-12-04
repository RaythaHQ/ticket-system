using App.Domain.Common;

namespace App.Application.Common.Interfaces;

public interface IEmailer
{
    void SendEmail(EmailMessage message);
}
