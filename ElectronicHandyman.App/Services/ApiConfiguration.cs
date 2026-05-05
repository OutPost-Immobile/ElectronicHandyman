namespace ElectronicHandyman.App.Services;

/// <summary>
/// Centralized configuration constants for API communication.
/// </summary>
public static class ApiConfiguration
{
    /// <summary>
    /// Default base URL for the backend API.
    /// Uses 10.0.2.2 which maps to the host machine's localhost from the Android emulator.
    /// </summary>
    public const string DefaultBaseUrl = "https://100.101.239.33:7198";

    /// <summary>
    /// Default HTTP request timeout in seconds.
    /// </summary>
    public const int DefaultTimeoutSeconds = 15;

    /// <summary>
    /// Default debounce interval in milliseconds.
    /// The app waits this long after stable detection before sending an API request.
    /// </summary>
    public const int DefaultDebounceMs = 2000;

    /// <summary>
    /// Default grace period in milliseconds.
    /// Delays overlay removal after detection is lost to avoid flickering.
    /// </summary>
    public const int DefaultGracePeriodMs = 1000;
}
