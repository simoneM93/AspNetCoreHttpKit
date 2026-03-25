using StructLog.Interfaces;

namespace AspNetCoreHttpKit.Logging
{
    /// <summary>
    /// StructLog EventCodes for HTTP operations.
    /// Each HTTP method has a dedicated request and error code
    /// to make filtering and alerting in monitoring tools straightforward.
    /// </summary>
    internal static class HttpEventCodes
    {
        // GET
        public static readonly IEventCode GetRequest = new EventCode("HTTP_GET_REQ", "HTTP GET request");
        public static readonly IEventCode GetError = new EventCode("HTTP_GET_ERR", "HTTP GET error");

        // POST
        public static readonly IEventCode PostRequest = new EventCode("HTTP_POST_REQ", "HTTP POST request");
        public static readonly IEventCode PostError = new EventCode("HTTP_POST_ERR", "HTTP POST error");

        // PUT
        public static readonly IEventCode PutRequest = new EventCode("HTTP_PUT_REQ", "HTTP PUT request");
        public static readonly IEventCode PutError = new EventCode("HTTP_PUT_ERR", "HTTP PUT error");

        // PATCH
        public static readonly IEventCode PatchRequest = new EventCode("HTTP_PATCH_REQ", "HTTP PATCH request");
        public static readonly IEventCode PatchError = new EventCode("HTTP_PATCH_ERR", "HTTP PATCH error");

        // DELETE
        public static readonly IEventCode DeleteRequest = new EventCode("HTTP_DELETE_REQ", "HTTP DELETE request");
        public static readonly IEventCode DeleteError = new EventCode("HTTP_DELETE_ERR", "HTTP DELETE error");

        // Cross-cutting
        public static readonly IEventCode Timeout = new EventCode("HTTP_TIMEOUT", "HTTP request timeout");
        public static readonly IEventCode Exception = new EventCode("HTTP_EXCEPTION", "HTTP unhandled exception");

        public static IEventCode RequestCodeFor(HttpMethod method)
        {
            if (method == HttpMethod.Get) return GetRequest;
            if (method == HttpMethod.Post) return PostRequest;
            if (method == HttpMethod.Put) return PutRequest;
            if (method == HttpMethod.Patch) return PatchRequest;
            if (method == HttpMethod.Delete) return DeleteRequest;
            return new EventCode($"HTTP_{method.Method}_REQ", $"HTTP {method.Method} request");
        }

        public static IEventCode ErrorCodeFor(HttpMethod method)
        {
            if (method == HttpMethod.Get) return GetError;
            if (method == HttpMethod.Post) return PostError;
            if (method == HttpMethod.Put) return PutError;
            if (method == HttpMethod.Patch) return PatchError;
            if (method == HttpMethod.Delete) return DeleteError;
            return new EventCode($"HTTP_{method.Method}_ERR", $"HTTP {method.Method} error");
        }

        private sealed class EventCode : IEventCode
        {
            public string Code { get; }
            public string Description { get; }
            public EventCode(string code, string description) { Code = code; Description = description; }
        }
    }
}
