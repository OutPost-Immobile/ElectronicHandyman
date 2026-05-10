using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ElectronicHandyman.Avalonia.Services;
using ElectronicHandyman.Avalonia.ViewModels;
using ElectronicHandyman.Avalonia.Views;

namespace ElectronicHandyman.Avalonia;

public partial class App : Application
{
    private ContentControl? _navigationHost;
    private MainViewModel? _mainViewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var cameraSource = PlatformServices.CameraFrameSource ?? new NoopCameraFrameSource();

        YoloDetector? yoloDetector = null;
        if (PlatformServices.YoloModelBytes is { Length: > 0 } modelBytes)
        {
            yoloDetector = new YoloDetector(modelBytes);
        }

        var processor = yoloDetector != null ? new VideoFrameProcessor(yoloDetector) : new VideoFrameProcessor();
        var identificationService = new ChipIdentificationService("https://100.101.239.33:7198");
        _mainViewModel = new MainViewModel(cameraSource, processor, identificationService);
        _mainViewModel.SetNavigateToResult(NavigateToResult);
        _mainViewModel.SetNavigateToBatchResult(NavigateToBatchResult);        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _navigationHost = new ContentControl { Content = new MainView { DataContext = _mainViewModel } };
            desktop.MainWindow = new MainWindow { Content = _navigationHost };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            _navigationHost = new ContentControl { Content = new MainView { DataContext = _mainViewModel } };
            singleViewPlatform.MainView = _navigationHost;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void NavigateToResult(string chipName, string svgContent)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_navigationHost == null) return;

            var resultVm = new ResultViewModel(
                System.Array.Empty<byte>(),
                new System.Collections.Generic.List<Services.BoundingBox>(),
                new System.Collections.Generic.List<Services.ChipIdentificationResult>
                {
                    new() { IsSuccess = true, ChipName = chipName, SvgContent = svgContent }
                },
                NavigateBack);
            _navigationHost.Content = new ResultView { DataContext = resultVm };
        });
    }

    private void NavigateToBatchResult(byte[] imageBytes, System.Collections.Generic.List<Services.BoundingBox> boxes, System.Collections.Generic.List<Services.ChipIdentificationResult> results)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_navigationHost == null) return;

            var resultVm = new ResultViewModel(imageBytes, boxes, results, NavigateBack);
            _navigationHost.Content = new ResultView { DataContext = resultVm };
        });
    }

    private void NavigateBack()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_navigationHost == null || _mainViewModel == null) return;
            _navigationHost.Content = new MainView { DataContext = _mainViewModel };
        });
    }
}
