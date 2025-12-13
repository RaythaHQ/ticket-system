using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
using App.Application.Contacts.Commands;
using App.Application.Contacts.Queries;
using App.Application.MediaItems.Commands;
using App.Application.Tickets.Commands;
using App.Application.Tickets.Queries;
using CSharpVitamins;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace App.Web.Areas.Staff.Endpoints;

public static class AttachmentEndpoints
{
    private const string STAFF_ROUTE_PREFIX = "staff";

    public static IEndpointRouteBuilder MapStaffAttachmentEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints.MapGroup($"/{STAFF_ROUTE_PREFIX}/attachments");

        // File upload endpoints (reusing MediaItems infrastructure)
        group
            .MapPost("/presign", CloudUploadPresignRequest)
            .WithName("staff-attachments-presign")
            .RequireAuthorization()
            .ExcludeFromDescription();

        group
            .MapPost("/create-after-upload", CloudUploadCreateAfterUpload)
            .WithName("staff-attachments-create-after-upload")
            .RequireAuthorization()
            .ExcludeFromDescription();

        group
            .MapPost("/upload", DirectUpload)
            .WithName("staff-attachments-upload")
            .RequireAuthorization()
            .DisableAntiforgery()
            .ExcludeFromDescription();

        // Ticket attachment endpoints
        group
            .MapPost("/ticket/{ticketId:long}", AddTicketAttachment)
            .WithName("staff-ticket-attachment-add")
            .RequireAuthorization()
            .ExcludeFromDescription();

        group
            .MapDelete("/ticket/remove/{attachmentId}", RemoveTicketAttachment)
            .WithName("staff-ticket-attachment-remove")
            .RequireAuthorization()
            .ExcludeFromDescription();

        group
            .MapGet("/ticket/{ticketId:long}", GetTicketAttachments)
            .WithName("staff-ticket-attachments-list")
            .RequireAuthorization()
            .ExcludeFromDescription();

        // Contact attachment endpoints
        group
            .MapPost("/contact/{contactId:long}", AddContactAttachment)
            .WithName("staff-contact-attachment-add")
            .RequireAuthorization()
            .ExcludeFromDescription();

        group
            .MapDelete("/contact/remove/{attachmentId}", RemoveContactAttachment)
            .WithName("staff-contact-attachment-remove")
            .RequireAuthorization()
            .ExcludeFromDescription();

        group
            .MapGet("/contact/{contactId:long}", GetContactAttachments)
            .WithName("staff-contact-attachments-list")
            .RequireAuthorization()
            .ExcludeFromDescription();

        return endpoints;
    }

    private static async Task<IResult> CloudUploadPresignRequest(
        [FromBody] PresignRequest body,
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
        [FromBody] CreateAfterUploadRequest body,
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
            return Results.Json(
                new
                {
                    success = true,
                    mediaItemId = body.Id,
                    fileName = body.Filename,
                    contentType = body.ContentType,
                    objectKey = body.ObjectKey,
                    length = body.Length,
                }
            );
        }

        return Results.Json(
            new { success = false, error = response.Error },
            statusCode: StatusCodes.Status403Forbidden
        );
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
                new { success = false, error = "File is empty." },
                statusCode: StatusCodes.Status400BadRequest
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
                    success = true,
                    mediaItemId = idForKey.ToString(),
                    fileName = file.FileName,
                    contentType = file.ContentType,
                    objectKey,
                    length = data.Length,
                    url,
                    location = url,
                }
            );
        }

        return Results.Json(
            new { success = false, error = response.Error },
            statusCode: StatusCodes.Status403Forbidden
        );
    }

    private static async Task<IResult> AddTicketAttachment(
        long ticketId,
        [FromBody] AddAttachmentRequest body,
        [FromServices] ISender mediator
    )
    {
        var command = new AddTicketAttachment.Command
        {
            TicketId = ticketId,
            MediaItemId = body.MediaItemId,
            DisplayName = body.DisplayName,
            Description = body.Description,
        };

        var response = await mediator.Send(command);
        if (response.Success)
        {
            return Results.Json(new { success = true, attachmentId = response.Result.ToString() });
        }

        return Results.Json(
            new { success = false, error = response.Error },
            statusCode: StatusCodes.Status400BadRequest
        );
    }

    private static async Task<IResult> RemoveTicketAttachment(
        string attachmentId,
        [FromServices] ISender mediator
    )
    {
        var command = new RemoveTicketAttachment.Command { AttachmentId = attachmentId };
        var response = await mediator.Send(command);

        if (response.Success)
        {
            return Results.Json(new { success = true });
        }

        return Results.Json(
            new { success = false, error = response.Error },
            statusCode: StatusCodes.Status400BadRequest
        );
    }

    private static async Task<IResult> GetTicketAttachments(
        long ticketId,
        [FromServices] ISender mediator,
        [FromServices] IRelativeUrlBuilder relativeUrlBuilder
    )
    {
        var query = new GetTicketAttachments.Query { TicketId = ticketId };
        var response = await mediator.Send(query);

        var attachments = response.Result.Select(a => new
        {
            id = a.Id.ToString(),
            mediaItemId = a.MediaItemId.ToString(),
            displayName = a.DisplayName,
            description = a.Description,
            fileName = a.FileName,
            contentType = a.ContentType,
            sizeBytes = a.SizeBytes,
            fileSizeDisplay = a.FileSizeDisplay,
            fileExtension = a.FileExtension,
            isImage = a.IsImage,
            isPdf = a.IsPdf,
            uploadedByUserName = a.UploadedByUserName,
            createdAt = a.CreatedAt.ToString("MMM d, yyyy h:mm tt"),
            downloadUrl = relativeUrlBuilder.MediaRedirectToFileUrl(a.ObjectKey),
        });

        return Results.Json(new { success = true, attachments });
    }

    private static async Task<IResult> AddContactAttachment(
        long contactId,
        [FromBody] AddAttachmentRequest body,
        [FromServices] ISender mediator
    )
    {
        var command = new AddContactAttachment.Command
        {
            ContactId = contactId,
            MediaItemId = body.MediaItemId,
            DisplayName = body.DisplayName,
            Description = body.Description,
        };

        var response = await mediator.Send(command);
        if (response.Success)
        {
            return Results.Json(new { success = true, attachmentId = response.Result.ToString() });
        }

        return Results.Json(
            new { success = false, error = response.Error },
            statusCode: StatusCodes.Status400BadRequest
        );
    }

    private static async Task<IResult> RemoveContactAttachment(
        string attachmentId,
        [FromServices] ISender mediator
    )
    {
        var command = new RemoveContactAttachment.Command { AttachmentId = attachmentId };
        var response = await mediator.Send(command);

        if (response.Success)
        {
            return Results.Json(new { success = true });
        }

        return Results.Json(
            new { success = false, error = response.Error },
            statusCode: StatusCodes.Status400BadRequest
        );
    }

    private static async Task<IResult> GetContactAttachments(
        long contactId,
        [FromServices] ISender mediator,
        [FromServices] IRelativeUrlBuilder relativeUrlBuilder
    )
    {
        var query = new GetContactAttachments.Query { ContactId = contactId };
        var response = await mediator.Send(query);

        var attachments = response.Result.Select(a => new
        {
            id = a.Id.ToString(),
            mediaItemId = a.MediaItemId.ToString(),
            displayName = a.DisplayName,
            description = a.Description,
            fileName = a.FileName,
            contentType = a.ContentType,
            sizeBytes = a.SizeBytes,
            fileSizeDisplay = a.FileSizeDisplay,
            fileExtension = a.FileExtension,
            isImage = a.IsImage,
            isPdf = a.IsPdf,
            uploadedByUserName = a.UploadedByUserName,
            createdAt = a.CreatedAt.ToString("MMM d, yyyy h:mm tt"),
            downloadUrl = relativeUrlBuilder.MediaRedirectToFileUrl(a.ObjectKey),
        });

        return Results.Json(new { success = true, attachments });
    }
}

// Request DTOs
public record PresignRequest(string Filename, string ContentType, string? Extension);

public record CreateAfterUploadRequest(
    string Id,
    string Filename,
    string ContentType,
    string? Extension,
    string ObjectKey,
    long Length
);

public record AddAttachmentRequest(string MediaItemId, string? DisplayName, string? Description);
