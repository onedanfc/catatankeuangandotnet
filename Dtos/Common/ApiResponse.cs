using Microsoft.AspNetCore.Http;

namespace CatatanKeuanganDotnet.Dtos.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; init; }

        public int StatusCode { get; init; }

        public string Message { get; init; } = string.Empty;

        public T? Data { get; init; }

        public static ApiResponse<T> Succeeded(T data, string message, int statusCode = StatusCodes.Status200OK)
            => new()
            {
                Success = true,
                StatusCode = statusCode,
                Message = message,
                Data = data
            };

        public static ApiResponse<T> Succeeded(string message, int statusCode = StatusCodes.Status200OK)
            => new()
            {
                Success = true,
                StatusCode = statusCode,
                Message = message
            };

        public static ApiResponse<T> Failure(string message, int statusCode, T? data = default)
            => new()
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Data = data
            };
    }

    public class ApiResponse : ApiResponse<object?>
    {
        public new static ApiResponse Succeeded(string message, int statusCode = StatusCodes.Status200OK)
            => new()
            {
                Success = true,
                StatusCode = statusCode,
                Message = message
            };

        public new static ApiResponse Failure(string message, int statusCode, object? data = default)
            => new()
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Data = data
            };
    }
}
