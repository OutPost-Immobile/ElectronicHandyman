using ElectronicHandyman.App.Models;
using ElectronicHandyman.App.PageModels;

namespace ElectronicHandyman.App.Pages;

public partial class MainPage : ContentPage
{
    private readonly VideoFrameProcessor _processor;
    private IDispatcherTimer _timer;
    private bool _isProcessing = false;

    public MainPage()
    {
        InitializeComponent();
        _processor = new VideoFrameProcessor();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Rejestrujemy zdarzenie, by wiedzieć, kiedy kamery sprzętowe są gotowe
        CameraView.CamerasLoaded += CameraView_CamerasLoaded;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        await CameraView.StopCameraAsync();
    }

    private async void CameraView_CamerasLoaded(object sender, EventArgs e)
    {
        if (CameraView.Cameras.Count > 0)
        {
            CameraView.Camera = CameraView.Cameras.First(); // Domyślnie główna kamera z tyłu
            
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await CameraView.StartCameraAsync();
                StartFrameTimer(); // Zaczynamy cykliczne przechwytywanie
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
                var croppedComponents = await Task.Run(() => 
                {
                    return _processor.ProcessCameraFrameAndCrop(frameBytes);
                });

                // 2. Wracamy na wątek UI, aby zaktualizować listę wyciętych kostek
                if (croppedComponents.Count > 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        CroppedImagesLayout.Children.Clear(); // opcjonalnie: czyści starsze wyniki
                        
                        foreach (var imageBytes in croppedComponents)
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