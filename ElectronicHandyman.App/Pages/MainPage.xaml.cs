using ElectronicHandyman.App.Models;
using ElectronicHandyman.App.PageModels;
using ElectronicHandyman.App.Services;

namespace ElectronicHandyman.App.Pages;

public partial class MainPage : ContentPage
{
    private readonly VideoFrameProcessor _processor;
    private readonly ChipIdentificationCoordinator _coordinator;
    private IDispatcherTimer _timer;
    private bool _isProcessing = false;
    private BoundingBoxDrawable _drawable;

    private const int MaxCroppedImagesDisplayed = 3;

    public MainPage(VideoFrameProcessor processor, ChipIdentificationCoordinator coordinator)
    {
        InitializeComponent();
        _processor = processor;
        _coordinator = coordinator;

        _coordinator.IdentificationCompleted += OnIdentificationCompleted;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _drawable = new BoundingBoxDrawable();
        OverlayView.Drawable = _drawable;

        CameraView.CamerasLoaded += CameraView_CamerasLoaded;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        CameraView.CamerasLoaded -= CameraView_CamerasLoaded;

        _coordinator.IdentificationCompleted -= OnIdentificationCompleted;
        _coordinator.Dispose();

        try
        {
            await CameraView.StopCameraAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Camera stop error]: {ex.Message}");
        }
    }

    private async void StartCamera()
    {
        try
        {
            CameraView.Camera = CameraView.Cameras.First();
            await CameraView.StartCameraAsync();
            StartFrameTimer();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Camera start error]: {ex.Message}");
        }
    }

    private void CameraView_CamerasLoaded(object sender, EventArgs e)
    {
        if (CameraView.Cameras.Count > 0)
        {
            MainThread.BeginInvokeOnMainThread(() => StartCamera());
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NoCameraLabel.IsVisible = true;
            });
        }
    }

    private void StartFrameTimer()
    {
        _timer = Application.Current.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(500);
        _timer.Tick += async (s, e) => await ProcessSingleFrame();
        _timer.Start();
    }

    private async Task ProcessSingleFrame()
    {
        if (_isProcessing) return;
        _isProcessing = true;

        try
        {
            var imageSource = CameraView.GetSnapShot(Camera.MAUI.ImageFormat.JPEG);
            
            if (imageSource is StreamImageSource streamImageSource)
            {
                using var stream = await streamImageSource.Stream(CancellationToken.None);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                byte[] frameBytes = memoryStream.ToArray();

                var result = await Task.Run(() => _processor.ProcessFrame(frameBytes));

                _coordinator.OnDetectionResult(result);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _drawable.BoundingBoxes = result.BoundingBoxes;
                    _drawable.FrameWidth = result.FrameWidth;
                    _drawable.FrameHeight = result.FrameHeight;
                    _drawable.IdentificationState = _coordinator.CurrentState;
                    OverlayView.Invalidate();

                    LoadingIndicator.IsVisible = _coordinator.CurrentState.IsLoading;
                    LoadingIndicator.IsRunning = _coordinator.CurrentState.IsLoading;

                    // Update cropped images
                    CroppedImagesLayout.Children.Clear();
                    var imagesToShow = result.CroppedImages.Take(MaxCroppedImagesDisplayed);
                    foreach (var imageBytes in imagesToShow)
                    {
                        var src = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                        CroppedImagesLayout.Children.Add(new Image
                        {
                            Source = src,
                            HeightRequest = 100,
                            WidthRequest = 100,
                            Margin = new Thickness(5)
                        });
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Błąd OpenCV]: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private void OnIdentificationCompleted(object? sender, IdentificationState state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _drawable.IdentificationState = _coordinator.CurrentState;
            OverlayView.Invalidate();

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;

            // Show summary of identified chips below camera
            var identified = state.BoxResults.Where(b => b.IsSuccess).ToList();
            if (identified.Count > 0)
            {
                IdentificationResultFrame.IsVisible = true;
                var names = string.Join(", ", identified.Select(b => b.ChipName).Where(n => !string.IsNullOrEmpty(n)));
                ChipNameLabel.Text = names;
                IdentificationStatusLabel.Text = $"✓ Zidentyfikowano {identified.Count}/{state.BoxResults.Count}";
            }
            else if (state.BoxResults.Count > 0)
            {
                IdentificationResultFrame.IsVisible = true;
                ChipNameLabel.Text = "Nie rozpoznano";
                IdentificationStatusLabel.Text = $"0/{state.BoxResults.Count} zidentyfikowanych";
            }
            else
            {
                IdentificationResultFrame.IsVisible = false;
            }
        });
    }
}
