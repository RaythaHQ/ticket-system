using System.IO;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Security;
using App.Application.Common.Utils;
using App.Application.MediaItems.Commands;
using App.Application.MediaItems.Queries;
using App.Web.Authentication;

namespace App.Web.Areas.Api.Controllers.V1;

[Authorize(Policy = AppApiAuthorizationHandler.POLICY_PREFIX + RaythaClaimTypes.IsAdmin)]
public class MediaItemsController : BaseController
{
    /// <summary>
    /// Get a presigned URL for uploading a file to cloud storage.
    /// </summary>
    [HttpPost("presign", Name = "ApiMediaItemsPresign")]
    [ProducesResponseType(typeof(PresignResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Presign([FromBody] PresignRequest request)
    {
        if (
            !FileStorageUtility.IsAllowedMimeType(
                request.ContentType,
                FileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { success = false, error = "File type is not allowed." }
            );
        }

        var idForKey = ShortGuid.NewGuid();
        var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(
            idForKey.ToString(),
            request.Filename
        );
        var url = await FileStorageProvider.GetUploadUrlAsync(
            objectKey,
            request.Filename,
            request.ContentType,
            FileStorageUtility.GetDefaultExpiry()
        );

        return Ok(
            new PresignResponse
            {
                Url = url,
                Fields = new PresignFieldsResponse
                {
                    Id = idForKey.ToString(),
                    FileName = request.Filename,
                    ContentType = request.ContentType,
                    ObjectKey = objectKey,
                },
            }
        );
    }

    /// <summary>
    /// Create a media item record after uploading to cloud storage.
    /// </summary>
    [HttpPost("create-after-upload", Name = "ApiMediaItemsCreateAfterUpload")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAfterUpload([FromBody] CreateAfterUploadRequest request)
    {
        if (
            !FileStorageUtility.IsAllowedMimeType(
                request.ContentType,
                FileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { success = false, error = "File type is not allowed." }
            );
        }

        var input = new CreateMediaItem.Command
        {
            Id = request.Id,
            FileName = request.Filename,
            Length = request.Length,
            ContentType = request.ContentType,
            FileStorageProvider = FileStorageProvider.GetName(),
            ObjectKey = request.ObjectKey,
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            return Ok(new { success = true });
        }
        else
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { success = false, error = response.Error }
            );
        }
    }

    /// <summary>
    /// Upload a file directly (for local storage providers).
    /// </summary>
    [HttpPost("upload", Name = "ApiMediaItemsUpload")]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file.Length <= 0)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { success = false });
        }

        if (
            !FileStorageUtility.IsAllowedMimeType(
                file.ContentType,
                FileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { success = false, error = "File type is not allowed." }
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
        var downloadUrl = await FileStorageProvider.SaveAndGetDownloadUrlAsync(
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
            FileStorageProvider = FileStorageProvider.GetName(),
            ObjectKey = objectKey,
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            return Ok(
                new UploadResponse
                {
                    Url = downloadUrl,
                    Success = true,
                    Fields = new PresignFieldsResponse
                    {
                        Id = idForKey.ToString(),
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        ObjectKey = objectKey,
                    },
                }
            );
        }
        else
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { success = false, error = response.Error }
            );
        }
    }

    /// <summary>
    /// Get a redirect URL to download a file by its object key.
    /// </summary>
    [HttpGet("objectkey/{objectKey}", Name = "ApiMediaItemsGetByObjectKey")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> GetByObjectKey(string objectKey)
    {
        var downloadUrl = await FileStorageProvider.GetDownloadUrlAsync(
            objectKey,
            FileStorageUtility.GetDefaultExpiry()
        );
        return Redirect(downloadUrl);
    }

    /// <summary>
    /// Get a redirect URL to download a file by its ID.
    /// </summary>
    [HttpGet("{id}", Name = "ApiMediaItemsGetById")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var input = new GetMediaItemById.Query { Id = id };
        var response = await Mediator.Send(input);

        var downloadUrl = await FileStorageProvider.GetDownloadUrlAsync(
            response.Result.ObjectKey,
            FileStorageUtility.GetDefaultExpiry()
        );
        return Redirect(downloadUrl);
    }
}

// Request/Response DTOs
public record PresignRequest(string Filename, string ContentType);

public record CreateAfterUploadRequest(
    string Id,
    string Filename,
    string ContentType,
    string ObjectKey,
    long Length
);

public class PresignResponse
{
    public string Url { get; set; } = null!;
    public PresignFieldsResponse Fields { get; set; } = null!;
}

public class PresignFieldsResponse
{
    public string Id { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string ObjectKey { get; set; } = null!;
}

public class UploadResponse
{
    public string Url { get; set; } = null!;
    public bool Success { get; set; }
    public PresignFieldsResponse Fields { get; set; } = null!;
}

