using System.IO;
using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
using App.Application.MediaItems.Commands;
using App.Application.MediaItems.Queries;
using App.Domain.Entities;
using CSharpVitamins;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace App.Web.Areas.Admin.Endpoints;

public static class MediaItemsEndpoints
{
    private const string ADMIN_ROUTE_PREFIX = "admin";

    public static IEndpointRouteBuilder MapMediaItemsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup($"/{ADMIN_ROUTE_PREFIX}/media-items");

        group
            .MapPost("/presign", CloudUploadPresignRequest)
            .WithName("mediaitemspresignuploadurl")
            .RequireAuthorization()
            .ExcludeFromDescription();

        group
            .MapPost("/create-after-upload", CloudUploadCreateAfterUpload)
            .WithName("mediaitemscreateafterupload")
            .RequireAuthorization()
            .ExcludeFromDescription();

        group
            .MapPost("/upload", DirectUpload)
            .WithName("mediaitemslocalstorageupload")
            .RequireAuthorization()
            .DisableAntiforgery()
            .ExcludeFromDescription();

        group
            .MapGet("/objectkey/{objectKey}", RedirectToFileUrlByObjectKey)
            .WithName("mediaitemsredirecttofileurlbyobjectkey")
            .AllowAnonymous()
            .ExcludeFromDescription();

        group
            .MapGet("/id/{id}", RedirectToFileUrlById)
            .WithName("mediaitemsredirecttofileurlbyid")
            .AllowAnonymous()
            .ExcludeFromDescription();

        return endpoints;
    }

    private static async Task<IResult> CloudUploadPresignRequest(
        [FromBody] MediaItemPresignRequest body,
        [FromServices] IFileStorageProvider fileStorageProvider,
        [FromServices] IFileStorageProviderSettings fileStorageProviderSettings
    )
    {
        if (
            !FileStorageUtility.IsAllowedMimeType(
                body.ContentType,
                fileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            return Results.Json(
                new { success = false, error = "File type is not allowed." },
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        var idForKey = ShortGuid.NewGuid();
        var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(
            idForKey.ToString(),
            body.Filename
        );
        var url = await fileStorageProvider.GetUploadUrlAsync(
            objectKey,
            body.Filename,
            body.ContentType,
            FileStorageUtility.GetDefaultExpiry()
        );

        return Results.Json(
            new
            {
                url,
                fields = new
                {
                    id = idForKey.ToString(),
                    fileName = body.Filename,
                    contentType = body.ContentType,
                    objectKey,
                },
            }
        );
    }

    private static async Task<IResult> CloudUploadCreateAfterUpload(
        [FromBody] MediaItemCreateAfterUpload body,
        [FromServices] ISender mediator,
        [FromServices] IFileStorageProvider fileStorageProvider,
        [FromServices] IFileStorageProviderSettings fileStorageProviderSettings
    )
    {
        if (
            !FileStorageUtility.IsAllowedMimeType(
                body.ContentType,
                fileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            return Results.Json(
                new { success = false, error = "File type is not allowed." },
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        var input = new CreateMediaItem.Command
        {
            Id = body.Id,
            FileName = body.Filename,
            Length = body.Length,
            ContentType = body.ContentType,
            FileStorageProvider = fileStorageProvider.GetName(),
            ObjectKey = body.ObjectKey,
        };

        var response = await mediator.Send(input);
        if (response.Success)
        {
            return Results.Json(new { success = true });
        }
        else
        {
            return Results.Json(
                new { success = false, error = response.Error },
                statusCode: StatusCodes.Status403Forbidden
            );
        }
    }

    private static async Task<IResult> DirectUpload(
        IFormFile file,
        [FromServices] ISender mediator,
        [FromServices] IFileStorageProvider fileStorageProvider,
        [FromServices] IFileStorageProviderSettings fileStorageProviderSettings,
        [FromServices] IRelativeUrlBuilder relativeUrlBuilder
    )
    {
        if (file.Length <= 0)
        {
            return Results.Json(
                new { success = false },
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        if (
            !FileStorageUtility.IsAllowedMimeType(
                file.ContentType,
                fileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            return Results.Json(
                new { success = false, error = "File type is not allowed." },
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var data = stream.ToArray();

        var idForKey = ShortGuid.NewGuid();
        var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(
            idForKey.ToString(),
            file.FileName
        );
        await fileStorageProvider.SaveAndGetDownloadUrlAsync(
            data,
            objectKey,
            file.FileName,
            file.ContentType,
            FileStorageUtility.GetDefaultExpiry()
        );

        var input = new CreateMediaItem.Command
        {
            Id = idForKey,
            FileName = file.FileName,
            Length = data.Length,
            ContentType = file.ContentType,
            FileStorageProvider = fileStorageProvider.GetName(),
            ObjectKey = objectKey,
        };

        var response = await mediator.Send(input);
        if (response.Success)
        {
            var url = relativeUrlBuilder.MediaRedirectToFileUrl(objectKey);
            return Results.Json(
                new
                {
                    url,
                    location = url,
                    success = true,
                    fields = new
                    {
                        id = idForKey.ToString(),
                        fileName = file.FileName,
                        contentType = file.ContentType,
                        objectKey,
                    },
                }
            );
        }
        else
        {
            return Results.Json(
                new { success = false, error = response.Error },
                statusCode: StatusCodes.Status403Forbidden
            );
        }
    }

    private static async Task<IResult> RedirectToFileUrlByObjectKey(
        string objectKey,
        [FromServices] IFileStorageProvider fileStorageProvider
    )
    {
        var downloadUrl = await fileStorageProvider.GetDownloadUrlAsync(
            objectKey,
            FileStorageUtility.GetDefaultExpiry()
        );
        return Results.Redirect(downloadUrl);
    }

    private static async Task<IResult> RedirectToFileUrlById(
        string id,
        [FromServices] ISender mediator,
        [FromServices] IFileStorageProvider fileStorageProvider
    )
    {
        var input = new GetMediaItemById.Query { Id = id };
        var response = await mediator.Send(input);

        var downloadUrl = await fileStorageProvider.GetDownloadUrlAsync(
            response.Result.ObjectKey,
            FileStorageUtility.GetDefaultExpiry()
        );
        return Results.Redirect(downloadUrl);
    }
}

// DTOs for request bodies
public record MediaItemPresignRequest(string Filename, string ContentType, string? Extension);

public record MediaItemCreateAfterUpload(
    string Id,
    string Filename,
    string ContentType,
    string? Extension,
    string ObjectKey,
    long Length,
    string? ContentDisposition
);
