using App.Application.Common.Interfaces;

namespace App.Web.Services;

public class CurrentVersion : ICurrentVersion
{
    public string Version => "0.0.0";
}
