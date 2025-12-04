using Microsoft.Extensions.Configuration;
using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
using App.Domain.Entities;

namespace App.Infrastructure.Configurations;

public class EmailerConfiguration : IEmailerConfiguration
{
    private readonly IConfiguration _configuration;
    private readonly IAppDbContext _db;
    private OrganizationSettings? _organizationSettings;

    public EmailerConfiguration(IConfiguration configuration, IAppDbContext db)
    {
        _configuration = configuration;
        _db = db;
    }

    private OrganizationSettings OrganizationSettings =>
        _organizationSettings ??= _db.OrganizationSettings.First();

    public string SmtpHost =>
        OrganizationSettings.SmtpOverrideSystem
            ? OrganizationSettings.SmtpHost!
            : _configuration["SMTP_HOST"]!;
    public int SmtpPort =>
        OrganizationSettings.SmtpOverrideSystem
            ? OrganizationSettings.SmtpPort ?? 25
            : Convert.ToInt32(_configuration["SMTP_PORT"]);

    public string SmtpUsername =>
        OrganizationSettings.SmtpOverrideSystem
            ? OrganizationSettings.SmtpUsername!
            : _configuration["SMTP_USERNAME"]!;
    public string SmtpPassword =>
        OrganizationSettings.SmtpOverrideSystem
            ? OrganizationSettings.SmtpPassword!
            : _configuration["SMTP_PASSWORD"]!;
    public string SmtpFromAddress =>
        _configuration["SMTP_FROM_ADDRESS"]!.IfNullOrEmpty(
            OrganizationSettings.SmtpDefaultFromAddress!
        );
    public string SmtpFromName =>
        _configuration["SMTP_FROM_NAME"]!.IfNullOrEmpty(OrganizationSettings.SmtpDefaultFromName!);
    public string SmtpDefaultFromName => OrganizationSettings.SmtpDefaultFromName!;
    public string SmtpDefaultFromAddress => OrganizationSettings.SmtpDefaultFromAddress!;

    public bool IsMissingSmtpEnvVars()
    {
        return string.IsNullOrEmpty(_configuration["SMTP_HOST"]);
    }
}
