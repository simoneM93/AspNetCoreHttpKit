# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## [1.0.0] - YYYY-MM-DD

### Added
- `IHttpService` interface with `GetAsync<T>`, `PostAsync<T>`, `PutAsync<T>`, `PatchAsync<T>`, `DeleteAsync`
- `HttpResult<T>` and `HttpResult` — result pattern with fluent `OnSuccess` / `OnError` API
- `HttpServiceException` base class and 8 typed exceptions: `HttpBadRequestException`, `HttpUnauthorizedException`, `HttpForbiddenException`, `HttpNotFoundException`, `HttpConflictException`, `HttpUnprocessableEntityException`, `HttpTooManyRequestsException`, `HttpServerErrorException`
- `HttpService.ThrowIfFailed(result)` — converts a failed `HttpResult` to the appropriate typed exception
- `IHttpServiceFactory` and `HttpServiceFactory` for named client resolution
- `HttpServiceOptions` with global `BaseUrl`, `TimeoutSeconds`, and per-client `Clients` dictionary
- Priority chain for named client config: named client `BaseUrl`/`TimeoutSeconds` → global fallback
- `AddAspNetCoreHttpKit(IConfiguration)` and `AddAspNetCoreHttpKit()` DI extension methods
- Automatic JSON serialization via `System.Text.Json` with camelCase naming policy
- `IHttpLogger` internal abstraction for pluggable logging
- **StructLog integration** — `StructLogHttpLogger` uses StructLog with typed EventCodes if `IStructLog<T>` is registered in DI
- **ILogger fallback** — `ILoggerHttpLogger` is used automatically when StructLog is not registered
- `HttpEventCodes` — dedicated EventCodes per HTTP method: `HTTP_GET_REQ`, `HTTP_GET_ERR`, `HTTP_POST_REQ`, `HTTP_POST_ERR`, `HTTP_PUT_REQ`, `HTTP_PUT_ERR`, `HTTP_PATCH_REQ`, `HTTP_PATCH_ERR`, `HTTP_DELETE_REQ`, `HTTP_DELETE_ERR`, `HTTP_TIMEOUT`, `HTTP_EXCEPTION`
- Timeout detection via `TaskCanceledException` — distinguished from explicit cancellation
- `ValidateOnStart` for configuration validation at startup
- Unit test suite (`AspNetCoreHttpKit.Tests`) with `MockHttpMessageHandler` — no network, no extra packages
- MIT license

---

[Unreleased]: https://github.com/simoneM93/AspNetCoreHttpKit/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/simoneM93/AspNetCoreHttpKit/releases/tag/v1.0.0