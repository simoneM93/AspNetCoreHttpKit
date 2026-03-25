using AspNetCoreHttpKit.Helpers;
using AspNetCoreHttpKit.Logging;
using AspNetCoreHttpKit.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AspNetCoreHttpKit.Tests
{
    // ----------------------------------------------------------
    // Mock HTTP handler — simula risposte HTTP senza rete reale
    // ----------------------------------------------------------

    internal sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public MockHttpMessageHandler(HttpResponseMessage response)
            => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_response);
        }
    }

    internal sealed class TimeoutHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    // ----------------------------------------------------------
    // Null IHttpLogger — per i test, non logga nulla
    // ----------------------------------------------------------

    internal sealed class NullHttpLogger : IHttpLogger
    {
        public static readonly NullHttpLogger Instance = new();
        public void LogRequest(HttpMethod method, string url, string? clientName) { }
        public void LogSuccess(HttpMethod method, string url, HttpStatusCode statusCode) { }
        public void LogWarning(HttpMethod method, string url, HttpStatusCode statusCode, string? error) { }
        public void LogTimeout(HttpMethod method, string url) { }
        public void LogException(HttpMethod method, string url, Exception ex) { }
    }

    internal static class HttpServiceTestFactory
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static HttpService Create(HttpResponseMessage response)
        {
            var handler = new MockHttpMessageHandler(response);
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.test.com"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            return new HttpService(client, NullHttpLogger.Instance);
        }

        public static HttpService CreateWithTimeout()
        {
            var handler = new TimeoutHttpMessageHandler();
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.test.com"),
                Timeout = TimeSpan.FromMilliseconds(50)
            };
            return new HttpService(client, NullHttpLogger.Instance);
        }

        public static HttpResponseMessage JsonResponse<T>(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
            => new(statusCode)
            {
                Content = JsonContent.Create(data, options: _jsonOptions)
            };

        public static HttpResponseMessage ErrorResponse(HttpStatusCode statusCode, string body = "")
            => new(statusCode)
            {
                Content = new StringContent(body)
            };
    }

    internal sealed record TestUser(int Id, string Name);
    internal sealed record TestPayload(string Value);

    public class HttpService_Constructor
    {
        [Fact]
        public void Constructor_NullHttpClient_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new HttpService(null!, NullHttpLogger.Instance));
            Assert.Equal("httpClient", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            var client = new HttpClient();
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new HttpService(client, null!));
            Assert.Equal("logger", ex.ParamName);
        }

        [Fact]
        public void Constructor_ValidArguments_DoesNotThrow()
        {
            var sut = HttpServiceTestFactory.Create(new HttpResponseMessage(HttpStatusCode.OK));
            Assert.NotNull(sut);
        }
    }

    public class HttpService_GetAsync
    {
        [Fact]
        public async Task GetAsync_Success_ReturnsDeserializedData()
        {
            var user = new TestUser(1, "Simone");
            var sut = HttpServiceTestFactory.Create(HttpServiceTestFactory.JsonResponse(user));

            var result = await sut.GetAsync<TestUser>("/users/1");

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Data?.Id);
            Assert.Equal("Simone", result.Data?.Name);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public async Task GetAsync_NotFound_ReturnsFailureResult()
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.ErrorResponse(HttpStatusCode.NotFound, "User not found"));

            var result = await sut.GetAsync<TestUser>("/users/999");

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Contains("User not found", result.ErrorMessage);
        }

        [Fact]
        public async Task GetAsync_ServerError_ReturnsFailureResult()
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.ErrorResponse(HttpStatusCode.InternalServerError, "Internal error"));

            var result = await sut.GetAsync<TestUser>("/users/1");

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        }

        [Fact]
        public async Task GetAsync_Timeout_ReturnsTimeoutFailure()
        {
            var sut = HttpServiceTestFactory.CreateWithTimeout();

            var result = await sut.GetAsync<TestUser>("/users/1");

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.RequestTimeout, result.StatusCode);
        }

        [Fact]
        public async Task GetAsync_CancelledToken_ThrowsOperationCanceledException()
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.JsonResponse(new TestUser(1, "x")));
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                sut.GetAsync<TestUser>("/users/1", cts.Token));
        }
    }

    public class HttpService_PostAsync
    {
        [Fact]
        public async Task PostAsync_Success_ReturnsDeserializedData()
        {
            var created = new TestUser(42, "New User");
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.JsonResponse(created, HttpStatusCode.Created));

            var result = await sut.PostAsync<TestUser>("/users", new TestPayload("test"));

            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Data?.Id);
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        }

        [Fact]
        public async Task PostAsync_BadRequest_ReturnsFailureResult()
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.ErrorResponse(HttpStatusCode.BadRequest, "Invalid payload"));

            var result = await sut.PostAsync<TestUser>("/users", new TestPayload("bad"));

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Contains("Invalid payload", result.ErrorMessage);
        }
    }

    public class HttpService_PutAsync
    {
        [Fact]
        public async Task PutAsync_Success_ReturnsUpdatedData()
        {
            var updated = new TestUser(1, "Updated");
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.JsonResponse(updated));

            var result = await sut.PutAsync<TestUser>("/users/1", new TestPayload("updated"));

            Assert.True(result.IsSuccess);
            Assert.Equal("Updated", result.Data?.Name);
        }

        [Fact]
        public async Task PutAsync_NotFound_ReturnsFailureResult()
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.ErrorResponse(HttpStatusCode.NotFound));

            var result = await sut.PutAsync<TestUser>("/users/999", new TestPayload("x"));

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }
    }

    public class HttpService_PatchAsync
    {
        [Fact]
        public async Task PatchAsync_Success_ReturnsUpdatedData()
        {
            var patched = new TestUser(1, "Patched");
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.JsonResponse(patched));

            var result = await sut.PatchAsync<TestUser>("/users/1", new TestPayload("patch"));

            Assert.True(result.IsSuccess);
            Assert.Equal("Patched", result.Data?.Name);
        }
    }

    public class HttpService_DeleteAsync
    {
        [Fact]
        public async Task DeleteAsync_Success_ReturnsSuccessResult()
        {
            var sut = HttpServiceTestFactory.Create(
                new HttpResponseMessage(HttpStatusCode.NoContent));

            var result = await sut.DeleteAsync("/users/1");

            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ReturnsFailureResult()
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.ErrorResponse(HttpStatusCode.NotFound, "Not found"));

            var result = await sut.DeleteAsync("/users/999");

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }
    }

    public class HttpResult_FluentApi
    {
        [Fact]
        public async Task OnSuccess_InvokedWhenSuccessful()
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.JsonResponse(new TestUser(1, "Simone")));
            var invoked = false;

            var result = await sut.GetAsync<TestUser>("/users/1");
            result.OnSuccess(_ => invoked = true);

            Assert.True(invoked);
        }

        [Fact]
        public async Task OnError_NotInvokedWhenSuccessful()
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.JsonResponse(new TestUser(1, "Simone")));
            var invoked = false;

            var result = await sut.GetAsync<TestUser>("/users/1");
            result.OnError((_, _) => invoked = true);

            Assert.False(invoked);
        }

        [Fact]
        public async Task OnError_InvokedWhenFailed()
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.ErrorResponse(HttpStatusCode.NotFound, "Not found"));
            var invoked = false;

            var result = await sut.GetAsync<TestUser>("/users/1");
            result.OnError((_, _) => invoked = true);

            Assert.True(invoked);
        }

        [Fact]
        public async Task FluentChaining_WorksCorrectly()
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.JsonResponse(new TestUser(1, "Simone")));
            var successInvoked = false;
            var errorInvoked = false;

            var result = await sut.GetAsync<TestUser>("/users/1");
            result
                .OnSuccess(_ => successInvoked = true)
                .OnError((_, _) => errorInvoked = true);

            Assert.True(successInvoked);
            Assert.False(errorInvoked);
        }
    }

    public class HttpService_ThrowIfFailed
    {
        [Theory]
        [InlineData(HttpStatusCode.BadRequest, typeof(HttpBadRequestException))]
        [InlineData(HttpStatusCode.Unauthorized, typeof(HttpUnauthorizedException))]
        [InlineData(HttpStatusCode.Forbidden, typeof(HttpForbiddenException))]
        [InlineData(HttpStatusCode.NotFound, typeof(HttpNotFoundException))]
        [InlineData(HttpStatusCode.Conflict, typeof(HttpConflictException))]
        [InlineData(HttpStatusCode.UnprocessableEntity, typeof(HttpUnprocessableEntityException))]
        [InlineData(HttpStatusCode.TooManyRequests, typeof(HttpTooManyRequestsException))]
        [InlineData(HttpStatusCode.InternalServerError, typeof(HttpServerErrorException))]
        public async Task ThrowIfFailed_ThrowsCorrectException(
            HttpStatusCode statusCode,
            Type expectedExceptionType)
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.ErrorResponse(statusCode, "error"));

            var result = await sut.GetAsync<TestUser>("/users/1");

            var ex = Assert.Throws(expectedExceptionType,
                () => HttpServiceHelper.ThrowIfFailed(result));

            Assert.IsAssignableFrom<HttpServiceException>(ex);
            Assert.Equal(statusCode, ((HttpServiceException)ex).StatusCode);
        }

        [Fact]
        public async Task ThrowIfFailed_DoesNotThrowOnSuccess()
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.JsonResponse(new TestUser(1, "Simone")));

            var result = await sut.GetAsync<TestUser>("/users/1");

            HttpServiceHelper.ThrowIfFailed(result);
        }

        [Fact]
        public async Task ThrowIfFailed_Delete_ThrowsOnFailure()
        {
            var sut = HttpServiceTestFactory.Create(
                HttpServiceTestFactory.ErrorResponse(HttpStatusCode.NotFound, "Not found"));

            var result = await sut.DeleteAsync("/users/1");

            Assert.Throws<HttpNotFoundException>(() => HttpServiceHelper.ThrowIfFailed(result));
        }
    }

    public class HttpResult_FactoryMethods
    {
        [Fact]
        public void Success_SetsIsSuccessTrue()
        {
            var result = HttpResult<TestUser>.Success(new TestUser(1, "x"), HttpStatusCode.OK);
            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void Failure_SetsIsSuccessFalse()
        {
            var result = HttpResult<TestUser>.Failure(HttpStatusCode.NotFound, "Not found");
            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Equal("Not found", result.ErrorMessage);
            Assert.Null(result.Data);
        }

        [Fact]
        public void HttpResult_Success_SetsIsSuccessTrue()
        {
            var result = HttpResult.Success(HttpStatusCode.NoContent);
            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Fact]
        public void HttpResult_Failure_SetsIsSuccessFalse()
        {
            var result = HttpResult.Failure(HttpStatusCode.BadRequest, "Bad");
            Assert.False(result.IsSuccess);
            Assert.Equal("Bad", result.ErrorMessage);
        }
    }

    public class NullHttpLogger_Tests
    {
        [Fact]
        public void AllMethods_DoNotThrow()
        {
            var logger = NullHttpLogger.Instance;

            logger.LogRequest(HttpMethod.Get, "/test", "client");
            logger.LogSuccess(HttpMethod.Get, "/test", HttpStatusCode.OK);
            logger.LogWarning(HttpMethod.Get, "/test", HttpStatusCode.NotFound, "error");
            logger.LogTimeout(HttpMethod.Get, "/test");
            logger.LogException(HttpMethod.Get, "/test", new Exception("test"));
        }
    }
}
