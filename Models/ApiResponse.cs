using System;
using System.Text.Json.Serialization;

namespace IbnElgm3a.Models
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; }

        [JsonPropertyName("meta")]
        public ApiMeta Meta { get; set; } = new ApiMeta();

        [JsonPropertyName("pagination")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiPagination? Pagination { get; set; }

        [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiError? Error { get; set; }

        public static ApiResponse<T> CreateSuccess(T? data, string version = "1.0", ApiPagination? pagination = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Meta = new ApiMeta { Version = version },
                Pagination = pagination
            };
        }

        public static ApiResponse<T> CreateError(string code, string message, string? messageAr = null, string? field = null, string version = "1.0")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = code,
                    Message = message,
                    MessageAr = messageAr,
                    Field = field
                },
                Meta = new ApiMeta { Version = version }
            };
        }
    }

    public class ApiMeta
    {
        [JsonPropertyName("request_id")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";
    }

    public class ApiPagination
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }
    }

    public class ApiError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("message_ar")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MessageAr { get; set; }

        [JsonPropertyName("field")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Field { get; set; }
    }
}
