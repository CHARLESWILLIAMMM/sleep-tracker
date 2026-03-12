using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace SleepTracker.Platforms.Android;

[Activity(Theme = "@style/Maui.SplashTheme",
          MainLauncher = true,
          LaunchMode = LaunchMode.SingleTop,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
                                 ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
                                 ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RequestSensorPermissionsIfNeeded();
    }

    /// <summary>
    /// Requests BODY_SENSORS and POST_NOTIFICATIONS at runtime on Android 6.0+
    /// (API 23+). On API 21–22 all permissions are granted at install time via
    /// the manifest, so no runtime request is needed or possible – the early
    /// return is correct behaviour for those older API levels.
    /// </summary>
    private void RequestSensorPermissionsIfNeeded()
    {
        // Runtime permissions were introduced in Android M (API 23).
        // On earlier versions the OS grants all manifest permissions at install time.
        if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            return;

        var permissionsToRequest = new List<string>();

        if (CheckSelfPermission(global::Android.Manifest.Permission.BodySensors) != Permission.Granted)
            permissionsToRequest.Add(global::Android.Manifest.Permission.BodySensors);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu &&
            CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) != Permission.Granted)
            permissionsToRequest.Add(global::Android.Manifest.Permission.PostNotifications);

        if (permissionsToRequest.Count > 0)
            RequestPermissions(permissionsToRequest.ToArray(), requestCode: 100);
    }

    /// <summary>Starts the foreground sleep-tracking service.</summary>
    public void StartSleepTrackingService()
    {
        var serviceIntent = new Intent(this, typeof(SleepTrackingService));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            StartForegroundService(serviceIntent);
        else
            StartService(serviceIntent);
    }

    /// <summary>Stops the foreground sleep-tracking service.</summary>
    public void StopSleepTrackingService()
    {
        var serviceIntent = new Intent(this, typeof(SleepTrackingService));
        StopService(serviceIntent);
    }
}
