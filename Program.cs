using System.ComponentModel.DataAnnotations;
using System.Text;
using base64.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidation();

string allowedOrigin = builder.Configuration["AllowedOrigin"] ?? "http://localhost:5170";
string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins(allowedOrigin)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

long kestrelResquestStreamedMaxSizeBytes = builder.Configuration.GetValue<long>("kestrelResquestStreamedMaxSizeBytes", 52428800);
long kestrelRequestMaxSizeBytes = builder.Configuration.GetValue<long>("RequestMaxSizeBytes", 2097152);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = kestrelRequestMaxSizeBytes);

var app = builder.Build();

app.UseCors(MyAllowSpecificOrigins);

app.MapPost("/encode/text", (TextRequest request) =>
{
    byte[] bytes = Encoding.UTF8.GetBytes(request.Text);
    string result = EncodeService.Encode(bytes, bytes.Length);
    return TypedResults.Ok(new { encodedText = result });
});

app.MapPost("/decode/text", (TextRequest request) =>
{
    try
    {
        byte[] bytes = DecodeService.Decode(request.Text, request.Text.Length);
        string result = Encoding.UTF8.GetString(bytes);
        return TypedResults.Ok(new { decodedText = result });
    }
    catch (ArgumentException)
    {
        return Results.Problem(statusCode: 400, detail: "Invalid base64");
    }
});

app.MapPost("/encode/streamed", async (IFormFile file) =>
{
    string outputFileName = "encodedFile.txt";

    return TypedResults.Stream(streamWriterCallback: async outputStream =>
    {
        using var inputStream = file.OpenReadStream();

        await using (var writer = new StreamWriter(outputStream, Encoding.ASCII))
        {
            byte[] buffer = new byte[3072];
            int bytesRead;

            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                string base64Chunk = EncodeService.Encode(buffer, bytesRead);

                await writer.WriteAsync(base64Chunk);
            }

            await writer.FlushAsync();
        }
    }, contentType: "text/plain", fileDownloadName: outputFileName);
})
.WithMetadata(new RequestSizeLimitAttribute(kestrelResquestStreamedMaxSizeBytes))
.DisableAntiforgery();

app.MapPost("/decode/streamed", async (IFormFile file) =>
{
    if (file.ContentType != "text/plain") return Results.Problem(statusCode: 400, detail: "File must be plain text");

    string outputFileName = "decodedfile";

    return TypedResults.Stream(async outputStream =>
    {
        using var inputStream = file.OpenReadStream();

        byte[] buffer = new byte[4096];
        int bytesRead;

        while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            string fileChunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            try
            {
                byte[] bytesConverted = DecodeService.Decode(fileChunk, fileChunk.Length);

                await outputStream.WriteAsync(bytesConverted);
            }
            catch (ArgumentException)
            {
                return;
            }
        }
    }, contentType: "application/octet-stream", fileDownloadName: outputFileName);
})
.WithMetadata(new RequestSizeLimitAttribute(kestrelResquestStreamedMaxSizeBytes))
.DisableAntiforgery();

app.MapPost("/encode/buffered", async (IFormFile file) =>
{
    string outputFileName = "encodedFile.txt";

    using var memoryStream = new MemoryStream();
    await file.CopyToAsync(memoryStream);
    byte[] inputBytes = memoryStream.ToArray();

    string base64Result = EncodeService.Encode(inputBytes, inputBytes.Length);

    byte[] outputBytes = Encoding.ASCII.GetBytes(base64Result);

    return TypedResults.File(outputBytes, "text/plain", outputFileName);
})
.DisableAntiforgery();

app.MapPost("/decode/buffered", async (IFormFile file) =>
{
    if (file.ContentType != "text/plain") return Results.Problem(statusCode: 400, detail: "File must be plain text");

    string outputFileName = "decodedfile";

    using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
    string fileContentStr = await reader.ReadToEndAsync();

    try
    {
        byte[] bytesConverted = DecodeService.Decode(fileContentStr, fileContentStr.Length);

        return TypedResults.File(bytesConverted, contentType: "application/octet-stream", fileDownloadName: outputFileName);
    }
    catch (ArgumentException)
    {
        return Results.Problem(statusCode: 400, detail: "Invalid base64");
    }
})
.DisableAntiforgery();

app.Run();

public record TextRequest([Required] string Text);
