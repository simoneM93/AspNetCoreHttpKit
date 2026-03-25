# AspNetCoreHttpKit

[![NuGet](https://img.shields.io/nuget/v/AspNetCoreHttpKit.svg)](https://www.nuget.org/packages/AspNetCoreHttpKit)
[![Publish to NuGet](https://github.com/simoneM93/AspNetCoreHttpKit/actions/workflows/publish.yml/badge.svg)](https://github.com/simoneM93/AspNetCoreHttpKit/actions/workflows/publish.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![GitHub Sponsors](https://img.shields.io/badge/Sponsor-%E2%9D%A4-ea4aaa?logo=github-sponsors)](https://github.com/sponsors/simoneM93)
[![Changelog](https://img.shields.io/badge/Changelog-view-blue)](CHANGELOG.md)

A lightweight HTTP client toolkit for ASP.NET Core with typed results, structured logging (StructLog or ILogger), named clients, and configurable timeout via `appsettings.json`.

> **Why AspNetCoreHttpKit?**
> `HttpClient` in .NET is powerful but verbose. Every project ends up writing the same boilerplate:
> JSON serialization, error handling, logging, timeout management.
> AspNetCoreHttpKit wraps all of this in a clean, testable abstraction with a consistent API.

---

## ✨ Features

- 🌐 **GET, POST, PUT, PATCH, DELETE** with automatic JSON serialization/deserialization
- 📦 **Result pattern** — `HttpResult<T>` with fluent `OnSuccess` / `OnError`
- 💥 **Typed exceptions** — `HttpNotFoundException`, `HttpUnauthorizedException`, and more
- ⏱️ **Configurable timeout** — globally and per named client
- 🔑 **Named clients** — one client per external service, configured in `appsettings.json`
- 📝 **StructLog integration** — uses StructLog with typed EventCodes if registered, falls back to `ILogger` automatically
- 🧪 **DI-ready** — register with one line, mock `IHttpService` in tests

---

## 📋 Requirements

| Requirement | Minimum version |
|---|---|
| .NET | 9.0+ |
| ASP.NET Core | 9.0+ |

---

## 🚀 Installation

```bash
dotnet add package AspNetCoreHttpKit
```

---

## 🎯 Quick Start

### 1. Configure `appsettings.json`

```json
{
  "HttpServiceOptions": {
    "BaseUrl": "https://api.myservice.com",
    "TimeoutSeconds": 30,
    "Clients": {
      "PaymentApi": {
        "BaseUrl": "https://api.payment.com",
        "TimeoutSeconds": 10
      },
      "UserApi": {
        "TimeoutSeconds": 5
      }
    }
  }
}
```

> `UserApi` inherits the global `BaseUrl` but uses its own `TimeoutSeconds` of 5 seconds.
> `PaymentApi` overrides both.

### 2. Register services

```csharp
// With StructLog (automatic detection)
builder.Services.AddStructLog(); // register StructLog first
builder.Services.AddAspNetCoreHttpKit(builder.Configuration); // HttpKit detects it automatically

// Without StructLog — falls back to ILogger automatically
builder.Services.AddAspNetCoreHttpKit(builder.Configuration);

// Without appsettings (uses defaults)
builder.Services.AddAspNetCoreHttpKit();
```

### 3. Use the global client

```csharp
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IHttpService _http;

    public UsersController(IHttpService http)
    {
        _http = http;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken ct)
    {
        var result = await _http.GetAsync<User>($"/users/{id}", ct);

        return result.IsSuccess ? Ok(result.Data) : NotFound();
    }
}
```

### 4. Use a named client

```csharp
public class PaymentService
{
    private readonly IHttpService _client;

    public PaymentService(IHttpServiceFactory factory)
    {
        _client = factory.Create("PaymentApi");
    }

    public async Task<PaymentResult?> ChargeAsync(ChargeRequest request, CancellationToken ct)
    {
        var result = await _client.PostAsync<PaymentResult>("/charge", request, ct);

        // Option A — result pattern (fluent)
        result
            .OnSuccess(data => Console.WriteLine($"Charged: {data?.TransactionId}"))
            .OnError((msg, code) => Console.WriteLine($"Failed: {code} — {msg}"));

        // Option B — throw typed exception
        HttpServiceHelper.ThrowIfFailed(result);

        return result.Data;
    }
}
```

---

## 📝 Logging

AspNetCoreHttpKit resolves the logger automatically at startup:

| Scenario | Logger used |
|---|---|
| `AddStructLog()` called before `AddAspNetCoreHttpKit()` | StructLog with typed EventCodes |
| StructLog not registered | Standard `ILogger` |

### StructLog EventCodes

When StructLog is active, every HTTP operation is logged with a dedicated EventCode:

| EventCode | Description |
|---|---|
| `HTTP_GET_REQ` / `HTTP_GET_ERR` | GET request / error |
| `HTTP_POST_REQ` / `HTTP_POST_ERR` | POST request / error |
| `HTTP_PUT_REQ` / `HTTP_PUT_ERR` | PUT request / error |
| `HTTP_PATCH_REQ` / `HTTP_PATCH_ERR` | PATCH request / error |
| `HTTP_DELETE_REQ` / `HTTP_DELETE_ERR` | DELETE request / error |
| `HTTP_TIMEOUT` | Request timed out |
| `HTTP_EXCEPTION` | Unhandled exception |

---

## 📚 API Reference

### `IHttpService` methods

| Method | Description |
|---|---|
| `GetAsync<T>(url, ct)` | GET request, deserializes response to `T` |
| `PostAsync<T>(url, body, ct)` | POST with JSON body, deserializes response to `T` |
| `PutAsync<T>(url, body, ct)` | PUT with JSON body, deserializes response to `T` |
| `PatchAsync<T>(url, body, ct)` | PATCH with JSON body, deserializes response to `T` |
| `DeleteAsync(url, ct)` | DELETE request, returns `HttpResult` |

### `HttpResult<T>` properties

| Property | Type | Description |
|---|---|---|
| `IsSuccess` | `bool` | `true` if the response was 2xx |
| `Data` | `T?` | Deserialized response body (null on error) |
| `StatusCode` | `HttpStatusCode` | HTTP status code |
| `ErrorMessage` | `string?` | Error message or response body on failure |
| `Exception` | `Exception?` | Inner exception if the request threw |

### Fluent result handling

```csharp
var result = await _http.GetAsync<User>("/users/1", ct);

result
    .OnSuccess(user => _logger.LogInformation("Found user {Id}", user?.Id))
    .OnError((msg, code) => _logger.LogWarning("Error {Code}: {Msg}", code, msg));
```

### Typed exceptions

Use `HttpService.ThrowIfFailed(result)` to convert a failed result into a typed exception:

| Exception | Status code |
|---|---|
| `HttpBadRequestException` | 400 |
| `HttpUnauthorizedException` | 401 |
| `HttpForbiddenException` | 403 |
| `HttpNotFoundException` | 404 |
| `HttpConflictException` | 409 |
| `HttpUnprocessableEntityException` | 422 |
| `HttpTooManyRequestsException` | 429 |
| `HttpServerErrorException` | 500+ |

---

## ⚙️ Configuration options

### Global options

| Option | Type | Default | Description |
|---|---|---|---|
| `BaseUrl` | `string?` | `null` | Base URL for the default client |
| `TimeoutSeconds` | `int` | `30` | Default timeout in seconds |
| `Clients` | `Dictionary<string, HttpClientOptions>` | `{}` | Named client configurations |

### Per-client options

| Option | Type | Default | Description |
|---|---|---|---|
| `BaseUrl` | `string?` | Inherits global | Base URL override for this client |
| `TimeoutSeconds` | `int?` | Inherits global | Timeout override for this client |

---

## 🧪 Testing

`IHttpService` is a plain interface — mock it directly in unit tests:

```csharp
var httpMock = new Mock<IHttpService>();

httpMock
    .Setup(h => h.GetAsync<User>("/users/1", It.IsAny<CancellationToken>()))
    .ReturnsAsync(HttpResult<User>.Success(new User { Id = 1, Name = "Simone" }, HttpStatusCode.OK));
```

---

## ❤️ Support

If you find AspNetCoreHttpKit useful, consider sponsoring its development.

[![Sponsor simoneM93](https://img.shields.io/badge/Sponsor-%E2%9D%A4-ea4aaa?logo=github-sponsors&style=for-the-badge)](https://github.com/sponsors/simoneM93)

---

## 📄 License

MIT — see [LICENSE](LICENSE) for details.
