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
            return base.CustomizeAppBuilder(builder)
            .WithInterFont();
        }
    }
}
