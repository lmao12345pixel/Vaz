using System.Net;

namespace Vazer.Core.Modrinth;

public enum ModrinthErrorType
{
    None,
    Network,
    Timeout,
    RateLimited,
    Server,
    Unknown
}

public sealed class ModrinthRateLimitException : HttpRequestException
{
    public int? RetryAfterSeconds { get; }

    public ModrinthRateLimitException(string message, int? retryAfterSeconds = null)
        : base(message, null, HttpStatusCode.TooManyRequests)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}

public sealed record ModrinthErrorInfo
{
    public ModrinthErrorType ErrorType { get; init; } = ModrinthErrorType.None;
    public string Message { get; init; } = string.Empty;
    public bool CanRetry { get; init; }
    public int? RetryAfterSeconds { get; init; }
}

public static class ModrinthErrorMapper
{
    public static ModrinthErrorInfo Map(Exception ex)
    {
        if (ex is ModrinthRateLimitException rle)
        {
            var seconds = rle.RetryAfterSeconds ?? 5;
            return new ModrinthErrorInfo
            {
                ErrorType = ModrinthErrorType.RateLimited,
                CanRetry = true,
                RetryAfterSeconds = seconds,
                Message = $"Bạn đã gửi quá nhiều yêu cầu đến Modrinth (429). Vui lòng thử lại sau {seconds} giây."
            };
        }

        if (ex is TaskCanceledException or TimeoutException)
        {
            return new ModrinthErrorInfo
            {
                ErrorType = ModrinthErrorType.Timeout,
                CanRetry = true,
                Message = "Yêu cầu đến Modrinth bị quá thời gian (timeout). Vui lòng thử lại."
            };
        }

        if (ex is HttpRequestException hre)
        {
            if (hre.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return new ModrinthErrorInfo
                {
                    ErrorType = ModrinthErrorType.RateLimited,
                    CanRetry = true,
                    RetryAfterSeconds = 5,
                    Message = "Quá giới hạn tần suất yêu cầu Modrinth (429). Vui lòng thử lại sau."
                };
            }

            if (hre.StatusCode.HasValue && (int)hre.StatusCode.Value >= 500 && (int)hre.StatusCode.Value < 600)
            {
                return new ModrinthErrorInfo
                {
                    ErrorType = ModrinthErrorType.Server,
                    CanRetry = true,
                    Message = $"Máy chủ Modrinth gặp sự cố ({(int)hre.StatusCode.Value}). Vui lòng thử lại sau."
                };
            }

            return new ModrinthErrorInfo
            {
                ErrorType = ModrinthErrorType.Network,
                CanRetry = true,
                Message = "Không thể kết nối đến máy chủ Modrinth. Kiểm tra lại kết nối mạng."
            };
        }

        return new ModrinthErrorInfo
        {
            ErrorType = ModrinthErrorType.Unknown,
            CanRetry = true,
            Message = $"Đã xảy ra lỗi: {ex.Message}"
        };
    }
}
