# ASP.NET Core File Upload & Download

A small ASP.NET Core Minimal API example that demonstrates how to upload files to local storage and stream them back for download.

The project intentionally stays small and focused. It demonstrates the core file-handling flow while covering a few safeguards that are easy to miss in basic examples.

## Features

- Upload files using `multipart/form-data`
- 10 MB upload limit
- Reject empty files
- Sanitize client-provided file names before writing to disk
- Prevent accidental overwrites with `409 Conflict`
- Protect the download endpoint against path traversal
- Stream files from disk instead of loading the whole file into memory
- Detect common MIME types and fall back to `application/octet-stream`
- Support HTTP range requests for downloads

## Requirements

To build and run this project, install:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (`8.0.x`)
- Git, if you want to clone the repository
- `curl` or another HTTP client to test the endpoints

The project targets `net8.0`. Installing only a newer .NET runtime does not necessarily provide the .NET 8 runtime required by this application. Installing the .NET 8 SDK includes the required .NET 8 runtime.

Check your installed SDKs and runtimes with:

```bash
dotnet --list-sdks
dotnet --list-runtimes
```

You should see a .NET 8 SDK and `Microsoft.NETCore.App 8.0.x` in the output.

## Run locally

```bash
git clone https://github.com/MostafaNasrollahpour/aspnet-core-file-upload-download.git
cd aspnet-core-file-upload-download
dotnet restore
dotnet run --project FileUploadApi
```

ASP.NET Core prints the listening URL in the terminal. With the included launch settings, the HTTP profile uses:

```text
http://localhost:5064
```

If your terminal shows a different URL, use that URL in the examples below.

## API

### Upload a file

`POST /upload`

The request must use `multipart/form-data` with a field named `file`.

```bash
curl -X POST \
  http://localhost:5064/upload \
  -F "file=@/path/to/file.txt"
```

Example response:

```json
{
  "fileName": "file.txt",
  "size": 128,
  "downloadUrl": "/download/file.txt"
}
```

Possible responses:

- `201 Created` — file saved successfully
- `400 Bad Request` — empty, invalid, or oversized file
- `409 Conflict` — a file with the same name already exists

### Download a file

`GET /download/{fileName}`

```bash
curl -OJ http://localhost:5064/download/file.txt
```

The endpoint streams the file from disk and supports range requests.

Possible responses:

- `200 OK` — file returned successfully
- `400 Bad Request` — invalid file path
- `404 Not Found` — file does not exist

## Storage

Uploaded files are stored in the application's local `uploads/` directory. The directory is created automatically at runtime and is ignored by Git.

This is appropriate for a small demo. In a production application, file storage would typically be moved to dedicated object storage such as Amazon S3 or Azure Blob Storage, with file metadata stored separately when needed.

## Security notes

This sample avoids writing a client-provided path directly to disk and validates resolved download paths to reduce path-traversal risk.

The upload endpoint uses `DisableAntiforgery()` because this repository is a Minimal API example intended to be called by API clients such as `curl`. For a browser application that uses cookie-based authentication, configure antiforgery protection rather than disabling it.

Production systems may also require authentication and authorization, extension or MIME allow-lists, malware scanning, storage quotas, generated storage names, and external object storage.

## Project history

This project was originally created on **July 17, 2025** as a small ASP.NET Core file upload/download example.

It was revisited in **September 2026** to clean up the repository and improve the example, including safer file handling, streamed downloads, path validation, clearer project structure, and documentation.

The repository remains intentionally small and is meant to serve as a focused reference implementation rather than a production-ready file storage service.

## Project structure

```text
.
├── FileUploadApi/
│   ├── Program.cs
│   ├── FileUploadApi.csproj
│   └── Properties/
├── FileUploadApi.sln
├── .gitignore
├── LICENSE
└── README.md
```

## License

This project is licensed under the [MIT License](LICENSE).
