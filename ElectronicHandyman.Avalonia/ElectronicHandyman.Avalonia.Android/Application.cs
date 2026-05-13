using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using ElectronicHandyman.Avalonia.Services;

namespace ElectronicHandyman.Avalonia.Android
{
    [Application]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            PlatformServices.CameraFrameSource = new AndroidCameraFrameSource();

            // Load YOLO model from assets
            try
            {
                using var assetStream = Assets!.Open("best.onnx");
                using var memoryStream = new System.IO.MemoryStream();
                assetStream.CopyTo(memoryStream);
                PlatformServices.YoloModelBytes = memoryStream.ToArray();
            }
            catch
            {
                // Model not available — detection will be disabled
            }

            return base.CustomizeAppBuilder(builder)
                .WithInterFont();
        }
    }
}
