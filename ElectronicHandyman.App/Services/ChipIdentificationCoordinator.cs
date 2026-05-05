namespace ElectronicHandyman.App.Services;

/// <summary>
/// Coordinates chip identification for ALL detected bounding boxes.
/// Sends each cropped image to the API and stores results per box.
/// </summary>
public class ChipIdentificationCoordinator : IDisposable
{
    private readonly IChipIdentificationService _identificationService;
    private readonly object _lock = new();

    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRequestInFlight;
    private bool _disposed;
    private DateTime _lastRequestTime = DateTime.MinValue;

    private const int CooldownMs = 5000;

    public event EventHandler<IdentificationState>? IdentificationCompleted;

    public IdentificationState CurrentState { get; private set; } = new();

    public ChipIdentificationCoordinator(IChipIdentificationService identificationService)
    {
        _identificationService = identificationService ?? throw new ArgumentNullException(nameof(identificationService));
    }

    public void OnDetectionResult(DetectionResult result)
    {
        lock (_lock)
        {
            if (_disposed) return;

            if (result.BoundingBoxes.Count == 0 || result.CroppedImages.Count == 0)
            {
                // Detection lost — clear after brief delay
                if (!CurrentState.IsLoading)
                {
                    UpdateState(new IdentificationState());
                }
                return;
            }

            // In-flight guard
            if (_isRequestInFlight) return;

            // Cooldown guard
            var elapsed = (DateTime.UtcNow - _lastRequestTime).TotalMilliseconds;
            if (elapsed < CooldownMs) return;

            // Fire identification for ALL cropped images
            System.Diagnostics.Debug.WriteLine($"[Coordinator] Firing API calls for {result.CroppedImages.Count} boxes");
            _isRequestInFlight = true;
            _lastRequestTime = DateTime.UtcNow;
            _cancellationTokenSource = new CancellationTokenSource();

            UpdateState(new IdentificationState { IsLoading = true });

            var cts = _cancellationTokenSource;
            var detection = result;
            _ = Task.Run(() => ExecuteAllIdentificationsAsync(detection, cts.Token));
        }
    }

    private async Task ExecuteAllIdentificationsAsync(DetectionResult detection, CancellationToken ct)
    {
        var results = new List<BoxIdentification>();

        try
        {
            // Send all cropped images in parallel (max 3 concurrent)
            var tasks = new List<Task<BoxIdentification>>();
            var count = Math.Min(detection.CroppedImages.Count, detection.BoundingBoxes.Count);

            for (int i = 0; i < count; i++)
            {
                var imageData = detection.CroppedImages[i];
                var box = detection.BoundingBoxes[i];
                var index = i;

                tasks.Add(IdentifySingleBoxAsync(imageData, box, index, ct));
            }

            var completed = await Task.WhenAll(tasks);
            results.AddRange(completed);

            var successCount = results.Count(r => r.IsSuccess);
            System.Diagnostics.Debug.WriteLine($"[Coordinator] All done: {successCount}/{results.Count} identified successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Coordinator] Batch failed: {ex.Message}");
        }
        finally
        {
            lock (_lock)
            {
                _isRequestInFlight = false;
                _lastRequestTime = DateTime.UtcNow;

                var newState = new IdentificationState
                {
                    BoxResults = results,
                    IsLoading = false
                };
                UpdateState(newState);
            }

            IdentificationCompleted?.Invoke(this, CurrentState);
        }
    }

    private async Task<BoxIdentification> IdentifySingleBoxAsync(byte[] imageData, BoundingBox box, int index, CancellationToken ct)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[Coordinator] Box[{index}]: Sending {imageData.Length} bytes...");
            var result = await _identificationService.IdentifyChipAsync(imageData, ct);
            System.Diagnostics.Debug.WriteLine($"[Coordinator] Box[{index}]: {(result.IsSuccess ? result.ChipName : result.ErrorMessage)}");

            return new BoxIdentification
            {
                IsSuccess = result.IsSuccess,
                SvgContent = result.SvgContent,
                ChipName = result.ChipName,
                ErrorMessage = result.ErrorMessage,
                Box = box
            };
        }
        catch (Exception ex)
        {
            return new BoxIdentification
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Box = box
            };
        }
    }

    private void UpdateState(IdentificationState newState)
    {
        CurrentState = newState;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}
