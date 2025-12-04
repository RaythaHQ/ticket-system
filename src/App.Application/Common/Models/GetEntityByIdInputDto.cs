using CSharpVitamins;

namespace App.Application.Common.Models;

public record GetEntityByIdInputDto
{
    public ShortGuid Id { get; init; }
}
