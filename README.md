# Base64-codec

A minimal ASP.NET Core Web API (.NET 10) that implements Base64 encoding and decoding **from scratch** — no `System.Convert.ToBase64String`, no external libraries. The algorithm is hand-rolled at the bit level.

Built as a portfolio project to demonstrate understanding of encoding fundamentals, HTTP streaming vs buffering trade-offs, and minimal API design.

## Running

```bash
dotnet run        # http://localhost:5170
dotnet watch run  # with hot reload
```

## Endpoints

All endpoints use `POST`.

| Route | Input | Output | Notes |
|---|---|---|---|
| `/encode/text` | `{ "text": "..." }` | `{ "encodedText": "..." }` | UTF-8 string → Base64 |
| `/decode/text` | `{ "text": "..." }` | `{ "decodedText": "..." }` | Base64 → UTF-8 string |
| `/encode/buffered` | multipart file | `text/plain` download | Whole file loaded into memory before encoding |
| `/decode/buffered` | multipart `text/plain` | `application/octet-stream` download | Whole file loaded into memory before decoding |
| `/encode/streamed` | multipart file (up to 50 MB) | `text/plain` download | Processes in 3072-byte chunks — memory stays flat regardless of file size |
| `/decode/streamed` | multipart `text/plain` (up to 50 MB) | `application/octet-stream` download | Processes in 4096-byte chunks |

The **buffered** and **streamed** endpoints expose the same operations with different memory profiles. The streamed endpoints demonstrate that a large file can be encoded/decoded while holding only a few KB in memory at a time.

Default request size limit is **2 MB** for all endpoints. The streamed endpoints override this to **50 MB** to make the performance difference observable.

## Architecture

`Program.cs` — the entire application. Six routes, no controllers, no middleware layers beyond CORS.

`Services/EncodeService.cs` — processes 3 bytes at a time into 4 Base64 characters using a lookup table. Padding (`=`) is added for 1 or 2 leftover bytes.

`Services/DecodeService.cs` — uses a 256-element reverse lookup table (built at startup) to map Base64 characters back to 6-bit values. Invalid characters throw `ArgumentException`.

## Known Limitations

These are intentional constraints of this implementation, not oversights.

**No URL-safe Base64.** This API uses standard Base64 (`+` and `/`). The URL-safe variant (RFC 4648 §5) replaces those with `-` and `_` — passing URL-safe encoded input to the decode endpoints will produce incorrect output without an error.

**No line breaks.** Some encoders split Base64 output into 76-character lines (MIME, PEM). This API expects the input as a single unbroken string. Line breaks (`\n`, `\r\n`) are not in the Base64 alphabet and will be rejected by the text and buffered endpoints.

**Streamed decode cannot validate `=` padding mid-stream.** The decode service correctly validates that `=` only appears at the end of a Base64 string. However, the streamed endpoint splits input into 4096-byte chunks and processes each independently — it cannot know whether a given chunk is the last one. A `=` appearing mid-file (i.e., malformed Base64) will be silently accepted in streaming mode.

**No error signal after a streamed decode failure.** HTTP response headers (including status `200 OK`) are sent before the body starts streaming. If a decode error occurs mid-stream, the connection is closed and the client receives a truncated, corrupt file with no HTTP-level error indication.
