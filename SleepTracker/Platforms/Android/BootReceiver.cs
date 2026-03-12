using Android.App;
using Android.Content;
using Android.OS;

namespace SleepTracker.Platforms.Android;

/// <summary>
/// Broadcast receiver that restarts <see cref="SleepTrackingService"/> after
/// the device reboots, ensuring sleep monitoring continues without requiring
/// the user to manually open the app.
/// </summary>
[BroadcastReceiver(Name = "com.sleeptracker.app.BootReceiver",
                   Enabled = true,
                   Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted })]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action != Intent.ActionBootCompleted || context is null)
            return;

        var serviceIntent = new Intent(context, typeof(SleepTrackingService));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(serviceIntent);
        else
            context.StartService(serviceIntent);
    }
}
