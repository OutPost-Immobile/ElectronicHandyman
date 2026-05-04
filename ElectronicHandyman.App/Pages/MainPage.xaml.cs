using ElectronicHandyman.App.Models;
using ElectronicHandyman.App.PageModels;
using ElectronicHandyman.App.Services;

namespace ElectronicHandyman.App.Pages;

public partial class MainPage : ContentPage
{
    private readonly VideoFrameProcessor _processor;
    private IDispatcherTimer _timer;
    private bool _isProcessing = false;
    private BoundingBoxDrawable _drawable;

    public MainPage(VideoFrameProcessor processor)
    {
        InitializeComponent();
        _processor = processor;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _drawable = new BoundingBoxDrawable();
        OverlayView.Drawable = _drawable;

        // Always register the event — on Android, accessing Cameras before the
        // native handler is attached can throw JavaProxyThrowable.
        CameraView.CamerasLoaded += CameraView_CamerasLoaded;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        CameraView.CamerasLoaded -= CameraView_CamerasLoaded;

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
        // Ustawiamy timer na przechwyt co 500 ms (2 klatki na sekundę). 
        // Wystarczy, by w miarę szybko złapać układ w kadrze.
        _timer = Application.Current.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(500);
        _timer.Tick += async (s, e) => await ProcessSingleFrame();
        _timer.Start();
    }

    private async Task ProcessSingleFrame()
    {
        // Zabezpieczenie przed kolejkowaniem: jeśli OpenCV nadal liczy, olewamy nową klatkę
        if (_isProcessing) return;
        _isProcessing = true;

        try
        {
            // Snapshot to zrzut klatki z podglądu (bez wywoływania "pstryknięcia" aparatu)
            var imageSource = CameraView.GetSnapShot(Camera.MAUI.ImageFormat.JPEG);
            
            // MAUI ładuje to jako strumień (StreamImageSource)
            if (imageSource is StreamImageSource streamImageSource)
            {
                // Konwersja obrazu na tablicę bajtów, którą lubi OpenCV (Cv2.ImDecode)
                using var stream = await streamImageSource.Stream(CancellationToken.None);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                byte[] frameBytes = memoryStream.ToArray();

                // 1. Uruchamiamy "ciężką" detekcję na osobnym wątku, żeby ekran nie zamarzł
                var result = await Task.Run(() => _processor.ProcessFrame(frameBytes));

                // 2. Wracamy na wątek UI, aby zaktualizować overlay i listę wyciętych kostek
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Update bounding box overlay — always replace to clear stale rectangles
                    _drawable.BoundingBoxes = result.BoundingBoxes;
                    _drawable.FrameWidth = result.FrameWidth;
                    _drawable.FrameHeight = result.FrameHeight;
                    OverlayView.Invalidate();

                    // Update cropped images display — always clear previous results
                    CroppedImagesLayout.Children.Clear();

                    foreach (var imageBytes in result.CroppedImages)
                    {
                        var src = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                        var imageControl = new Image
                        {
                            Source = src,
                            HeightRequest = 100,
                            WidthRequest = 100,
                            Margin = new Thickness(5)
                        };
                        CroppedImagesLayout.Children.Add(imageControl);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            // Opcjonalnie: wyrzuci na konsolę ewentualny błąd z OpenCV (złe wczytanie klatki itp.)
            System.Diagnostics.Debug.WriteLine($"[Błąd OpenCV]: {ex.Message}");
        }
        finally
        {
            _isProcessing = false; // Zwalniamy blokadę
        }
    }
}