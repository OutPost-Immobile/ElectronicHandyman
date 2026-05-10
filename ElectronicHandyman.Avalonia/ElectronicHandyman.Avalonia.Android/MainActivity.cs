using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using ElectronicHandyman.Avalonia.Services;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace ElectronicHandyman.Avalonia.Android;

[Activity(
    Label = "ElectronicHandyman.Avalonia.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    private const int CameraRequestCode = 1101;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (PlatformServices.CameraFrameSource is AndroidCameraFrameSource cameraSource)
        {
            cameraSource.SetActivity(this);
        }

        EnsureCameraPermission();
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == CameraRequestCode && grantResults.Length > 0 && grantResults[0] == Permission.Granted)
        {
            _ = PlatformServices.CameraFrameSource?.StartAsync();
        }
    }

    private void EnsureCameraPermission()
    {
        if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) == Permission.Granted)
        {
            _ = PlatformServices.CameraFrameSource?.StartAsync();
            return;
        }

        ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.Camera }, CameraRequestCode);
    }
}
