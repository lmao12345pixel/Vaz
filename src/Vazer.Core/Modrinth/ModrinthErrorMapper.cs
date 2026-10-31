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
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is ModrinthRateLimitException rle)
            {
                return new ModrinthErrorInfo
                {
                    ErrorType = ModrinthErrorType.RateLimited,
                    CanRetry = true,
                    RetryAfterSeconds = rle.RetryAfterSeconds ?? 5,
                    Message = "Bạn thao tác quá nhanh. Vui lòng đợi một lát và thử lại."
                };
            }

            if (current is HttpRequestException hre)
            {
                if (hre.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    return new ModrinthErrorInfo
                    {
                        ErrorType = ModrinthErrorType.RateLimited,
                        CanRetry = true,
                        RetryAfterSeconds = 5,
                        Message = "Bạn thao tác quá nhanh. Vui lòng đợi một lát và thử lại."
                    };
                }

                if (hre.StatusCode.HasValue && (int)hre.StatusCode.Value >= 500 && (int)hre.StatusCode.Value < 600)
                {
                    return new ModrinthErrorInfo
                    {
                        ErrorType = ModrinthErrorType.Server,
                        CanRetry = true,
                        Message = "Máy chủ đang gặp sự cố. Vui lòng thử lại sau."
                    };
                }

                return new ModrinthErrorInfo
                {
                    ErrorType = ModrinthErrorType.Network,
                    CanRetry = true,
                    Message = "Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối Internet của bạn và thử lại."
                };
            }

            if (current is TaskCanceledException or TimeoutException)
            {
                return new ModrinthErrorInfo
                {
                    ErrorType = ModrinthErrorType.Timeout,
                    CanRetry = true,
                    Message = "Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối Internet của bạn và thử lại."
                };
            }
        }

        return new ModrinthErrorInfo
        {
            ErrorType = ModrinthErrorType.Unknown,
            CanRetry = true,
            Message = "Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối Internet của bạn và thử lại."
        };
    }
}
