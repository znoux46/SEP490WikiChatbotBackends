using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.Infrastructure.Services;

public class CloudinaryDocumentStorageService : IDocumentStorageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryDocumentStorageService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException("Cloudinary configuration is missing.");
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<UploadedDocumentResultDto> UploadAsync(
        DocumentUploadItemDto file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var stream = file.OpenReadStream();

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            PublicId = BuildPublicId(file.FileName),
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false,
        };

        var result = await _cloudinary.UploadAsync(uploadParams, "raw", cancellationToken);
        if (result.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
        }

        return new UploadedDocumentResultDto
        {
            PublicId = result.PublicId,
            Url = result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? string.Empty,
            ResourceType = result.ResourceType ?? "raw",
            Format = result.Format,
            Bytes = result.Bytes,
        };
    }

    public async Task DeleteAsync(
        string publicId,
        string resourceType = "raw",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        var deleteParams = new DeletionParams(publicId)
        {
            ResourceType = ParseResourceType(resourceType),
        };

        var result = await _cloudinary.DestroyAsync(deleteParams);
        if (result.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary delete failed: {result.Error.Message}");
        }
    }

    private static string BuildPublicId(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var safeBaseName = string.Concat(
            baseName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-'))
            .Trim('-');

        if (string.IsNullOrWhiteSpace(safeBaseName))
        {
            safeBaseName = "document";
        }

        return $"wiki-chat/{safeBaseName}-{Guid.NewGuid():N}{extension}";
    }

    private static ResourceType ParseResourceType(string resourceType)
    {
        return resourceType.ToLowerInvariant() switch
        {
            "image" => ResourceType.Image,
            "video" => ResourceType.Video,
            "auto" => ResourceType.Auto,
            _ => ResourceType.Raw,
        };
    }
}