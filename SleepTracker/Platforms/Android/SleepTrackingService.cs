using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using SleepTracker.Data;
using SleepTracker.Models;

namespace SleepTracker.Platforms.Android;

/// <summary>
/// An Android Foreground Service that runs continuously overnight to monitor
/// the device's accelerometer and ambient light sensor.
///
/// Sensor strategy:
///   • TYPE_LIGHT   – detects when the room goes dark (intent to sleep).
///   • TYPE_ACCELEROMETER – tracks movement variance; low movement indicates
///     deep sleep, high movement indicates restlessness or waking up.
///
/// Battery conservation: readings are sampled in 5-minute bursts rather than
/// registering a permanent high-frequency listener.  A <see cref="Handler"/>
/// posts a <see cref="Runnable"/> every <see cref="SampleIntervalMs"/> ms to
/// register sensors, capture a sample, and unregister them immediately.
/// </summary>
[Service(Name = "com.sleeptracker.app.SleepTrackingService",
         Enabled = true,
         Exported = false,
         ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeHealth)]
public class SleepTrackingService : Service, ISensorEventListener
{
    // -----------------------------------------------------------------------
    // Constants
    // -----------------------------------------------------------------------

    /// <summary>Notification channel ID for the persistent foreground notification.</summary>
    private const string ChannelId = "sleep_tracker_channel";

    /// <summary>Unique notification ID for the foreground notification.</summary>
    private const int NotificationId = 1001;

    /// <summary>5-minute sampling interval in milliseconds.</summary>
    private const long SampleIntervalMs = 5 * 60 * 1_000L;

    /// <summary>
    /// How long (in ms) the listener stays registered to capture a stable
    /// reading before being unregistered again.
    /// </summary>
    private const long SensorListenDurationMs = 3_000L;

    /// <summary>
    /// Ambient light threshold (lux) below which the room is considered dark.
    /// Typical values: 0 = complete darkness, ~1 = candle, ~10 = dim room.
    /// </summary>
    private const float DarkRoomLuxThreshold = 5f;

    /// <summary>
    /// Accelerometer magnitude delta (m/s²) above which the user is considered
    /// to be moving (awake/restless).
    /// </summary>
    private const float MovementThreshold = 1.5f;

    // -----------------------------------------------------------------------
    // Fields
    // -----------------------------------------------------------------------

    private SensorManager? _sensorManager;
    private Sensor? _accelerometerSensor;
    private Sensor? _lightSensor;

    private Handler? _handler;
    private Java.Lang.Runnable? _sampleRunnable;

    private SleepDatabase? _database;

    // Mutable state updated by sensor callbacks (no lock needed – callbacks
    // are delivered on the same Looper thread as the Handler).
    private float _latestAccelMagnitude;
    private float _latestLightLevel;
    private bool _accelReceived;
    private bool _lightReceived;

    // -----------------------------------------------------------------------
    // Service lifecycle
    // -----------------------------------------------------------------------

    public override void OnCreate()
    {
        base.OnCreate();

        _database = new SleepDatabase(
            Path.Combine(FileSystem.AppDataDirectory, "sleep.db3"));

        _sensorManager = (SensorManager?)GetSystemService(SensorService);
        _accelerometerSensor = _sensorManager?.GetDefaultSensor(SensorType.Accelerometer);
        _lightSensor = _sensorManager?.GetDefaultSensor(SensorType.Light);

        CreateNotificationChannel();
        StartForeground(NotificationId, BuildNotification("Sleep Tracker running…"));

        _handler = new Handler(Looper.MainLooper!);
        ScheduleNextSample();
    }

    [return: GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        // Returning Sticky ensures Android restarts the service if it is killed.
        return StartCommandResult.Sticky;
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnDestroy()
    {
        _handler?.RemoveCallbacksAndMessages(null);
        UnregisterSensors();
        _database?.Dispose();
        base.OnDestroy();
    }

    // -----------------------------------------------------------------------
    // Sensor sampling loop
    // -----------------------------------------------------------------------

    private void ScheduleNextSample()
    {
        _sampleRunnable = new Java.Lang.Runnable(async () =>
        {
            await TakeSensorSampleAsync().ConfigureAwait(false);
            ScheduleNextSample();
        });

        _handler?.PostDelayed(_sampleRunnable, SampleIntervalMs);
    }

    /// <summary>
    /// Registers both sensors, waits for a stable reading, then unregisters
    /// and persists the reading to SQLite.
    /// </summary>
    private async Task TakeSensorSampleAsync()
    {
        _accelReceived = false;
        _lightReceived = false;

        RegisterSensors();

        // Wait up to SensorListenDurationMs for both sensors to report.
        await Task.Delay((int)SensorListenDurationMs).ConfigureAwait(false);

        UnregisterSensors();

        var reading = new SleepReading
        {
            Timestamp = DateTime.UtcNow,
            AccelerometerMagnitude = _latestAccelMagnitude,
            LightLevel = _latestLightLevel,
            IsAsleep = DetermineAsleepState(_latestAccelMagnitude, _latestLightLevel)
        };

        if (_database is not null)
            await _database.InsertReadingAsync(reading).ConfigureAwait(false);

        UpdateNotification(reading.IsAsleep);
    }

    private void RegisterSensors()
    {
        if (_accelerometerSensor is not null)
            _sensorManager?.RegisterListener(this, _accelerometerSensor, SensorDelay.Normal);

        if (_lightSensor is not null)
            _sensorManager?.RegisterListener(this, _lightSensor, SensorDelay.Normal);
    }

    private void UnregisterSensors()
    {
        _sensorManager?.UnregisterListener(this);
    }

    // -----------------------------------------------------------------------
    // ISensorEventListener implementation
    // -----------------------------------------------------------------------

    public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy) { }

    // Keeps a rolling window of recent raw magnitudes to compute variance,
    // which is orientation-independent unlike a simple gravity-subtraction check.
    private readonly Queue<float> _magnitudeWindow = new(capacity: 10);

    public void OnSensorChanged(SensorEvent? e)
    {
        if (e?.Sensor?.Type == SensorType.Accelerometer && e.Values?.Count >= 3)
        {
            float x = e.Values[0];
            float y = e.Values[1];
            float z = e.Values[2];

            // Raw vector magnitude.
            float rawMag = MathF.Sqrt(x * x + y * y + z * z);

            // Maintain a small rolling window and compute variance so the
            // movement score is independent of device orientation (orientation
            // changes only shift the mean, not the variance).
            _magnitudeWindow.Enqueue(rawMag);
            if (_magnitudeWindow.Count > 10)
                _magnitudeWindow.Dequeue();

            float mean = _magnitudeWindow.Sum() / _magnitudeWindow.Count;
            float variance = _magnitudeWindow
                .Select(m => (m - mean) * (m - mean))
                .Sum() / _magnitudeWindow.Count;

            // Use the square root of variance (std-dev) as the movement score.
            _latestAccelMagnitude = MathF.Sqrt(variance);
            _accelReceived = true;
        }
        else if (e?.Sensor?.Type == SensorType.Light && e.Values?.Count >= 1)
        {
            _latestLightLevel = e.Values[0];
            _lightReceived = true;
        }
    }

    // -----------------------------------------------------------------------
    // Sleep state inference
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns <c>true</c> when both the room is dark AND the device is still.
    /// </summary>
    private static bool DetermineAsleepState(float accelMagnitude, float lightLevel)
        => lightLevel <= DarkRoomLuxThreshold && accelMagnitude <= MovementThreshold;

    // -----------------------------------------------------------------------
    // Notification helpers
    // -----------------------------------------------------------------------

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var channel = new NotificationChannel(
            ChannelId,
            "Sleep Tracker",
            NotificationImportance.Low)
        {
            Description = "Persistent notification for background sleep monitoring"
        };

        var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
        notificationManager?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification(string contentText)
    {
        var intent = new Intent(this, typeof(global::SleepTracker.Platforms.Android.MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop);

        var pendingIntent = PendingIntent.GetActivity(
            this, 0, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        return new Notification.Builder(this, ChannelId)
            .SetContentTitle("Sleep Tracker")
            .SetContentText(contentText)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetContentIntent(pendingIntent)
            .SetOngoing(true)
            .Build()!;
    }

    private void UpdateNotification(bool isAsleep)
    {
        string status = isAsleep ? "Monitoring sleep 💤" : "Monitoring wakefulness ☀️";
        var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
        notificationManager?.Notify(NotificationId, BuildNotification(status));
    }
}
