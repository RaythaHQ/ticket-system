using System.Collections.Generic;
using System.Linq;
using App.Application.AuthenticationSchemes;
using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
using App.Application.OrganizationSettings;
using App.Domain.ValueObjects;
using App.Infrastructure.Services;
using CSharpVitamins;

namespace App.Web.Services;

public class CurrentOrganization : ICurrentOrganization
{
    private OrganizationSettingsDto? _organizationSettings;
    private IEnumerable<AuthenticationSchemeDto>? _authenticationSchemes;

    private readonly ICachedOrganizationService _cachedOrganizationService;
    private readonly ICurrentOrganizationConfiguration _configuration;

    public CurrentOrganization(
        ICachedOrganizationService cachedOrganizationService,
        ICurrentOrganizationConfiguration configuration
    )
    {
        _cachedOrganizationService = cachedOrganizationService;
        _configuration = configuration;
    }

    private OrganizationSettingsDto? OrganizationSettings
    {
        get
        {
            // Use cached service - still sync but now hits memory cache instead of DB
            _organizationSettings ??= _cachedOrganizationService
                .GetOrganizationSettingsAsync()
                .GetAwaiter()
                .GetResult();
            return _organizationSettings;
        }
    }

    public IEnumerable<AuthenticationSchemeDto> AuthenticationSchemes
    {
        get
        {
            // Use cached service - still sync but now hits memory cache instead of DB
            _authenticationSchemes ??= _cachedOrganizationService
                .GetAuthenticationSchemesAsync()
                .GetAwaiter()
                .GetResult();
            return _authenticationSchemes;
        }
    }

    public bool EmailAndPasswordIsEnabledForAdmins =>
        AuthenticationSchemes.Any(p =>
            p.IsEnabledForAdmins
            && p.AuthenticationSchemeType.DeveloperName == AuthenticationSchemeType.EmailAndPassword
        );
    public bool EmailAndPasswordIsEnabledForUsers =>
        AuthenticationSchemes.Any(p =>
            p.IsEnabledForUsers
            && p.AuthenticationSchemeType.DeveloperName == AuthenticationSchemeType.EmailAndPassword
        );

    public bool InitialSetupComplete => OrganizationSettings != null;

    public string OrganizationName => OrganizationSettings?.OrganizationName;

    public string WebsiteUrl => OrganizationSettings?.WebsiteUrl;

    public string TimeZone => OrganizationSettings?.TimeZone;

    public string SmtpDefaultFromAddress => OrganizationSettings?.SmtpDefaultFromAddress;

    public string SmtpDefaultFromName => OrganizationSettings?.SmtpDefaultFromName;

    public string DateFormat => OrganizationSettings?.DateFormat;

    public OrganizationTimeZoneConverter TimeZoneConverter =>
        OrganizationTimeZoneConverter.From(TimeZone, DateFormat);

    public string PathBase => _configuration.PathBase;
    public string RedirectWebsite => _configuration.RedirectWebsite;
}
