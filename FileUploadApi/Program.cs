using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;

const long maxFileSize = 10 * 1024 * 1024; // 10 MB

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxFileSize;
});

var app = builder.Build();

var uploadDirectory = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadDirectory);

var contentTypeProvider = new FileExtensionContentTypeProvider();

app.MapPost("/upload", async (IFormFile file, CancellationToken cancellationToken) =>
{
    if (file.Length == 0)
    {
        return Results.BadRequest(new { error = "The uploaded file is empty." });
    }

    if (file.Length > maxFileSize)
    {
        return Results.BadRequest(new { error = $"The file exceeds the {maxFileSize / 1024 / 1024} MB limit." });
    }

    // Never use the client-provided path directly. Keep only the file name.
    var safeFileName = Path.GetFileName(file.FileName.Replace('\\', '/'));

    if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName is "." or "..")
    {
        return Results.BadRequest(new { error = "The file name is invalid." });
    }

    var filePath = Path.Combine(uploadDirectory, safeFileName);

    try
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true);

        await file.CopyToAsync(stream, cancellationToken);
    }
    catch (IOException) when (File.Exists(filePath))
    {
        return Results.Conflict(new { error = $"A file named '{safeFileName}' already exists." });
    }

    var downloadUrl = $"/download/{Uri.EscapeDataString(safeFileName)}";

    return Results.Created(downloadUrl, new
    {
        fileName = safeFileName,
        size = file.Length,
        downloadUrl
    });
})
.DisableAntiforgery()
.WithName("UploadFile");

app.MapGet("/download/{fileName}", (string fileName) =>
{
    var uploadRoot = Path.GetFullPath(uploadDirectory) + Path.DirectorySeparatorChar;
    var filePath = Path.GetFullPath(Path.Combine(uploadDirectory, fileName));
    var pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    if (!filePath.StartsWith(uploadRoot, pathComparison))
    {
        return Results.BadRequest(new { error = "The file name is invalid." });
    }

    if (!File.Exists(filePath))
    {
        return Results.NotFound(new { error = "File not found." });
    }

    if (!contentTypeProvider.TryGetContentType(fileName, out var contentType))
    {
        contentType = "application/octet-stream";
    }

    // Passing the file path lets ASP.NET Core stream the file instead of loading it all into memory.
    return Results.File(
        filePath,
        contentType,
        fileDownloadName: fileName,
        enableRangeProcessing: true);
})
.WithName("DownloadFile");

app.Run();
