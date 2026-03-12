using SQLite;

namespace SleepTracker.Models;

/// <summary>
/// Represents a single sensor reading captured by the sleep tracking service.
/// Each row stores the accelerometer magnitude (movement variance) and the
/// ambient light level sampled at a specific point in time.
/// </summary>
[Table("SleepReadings")]
public class SleepReading
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>UTC timestamp when this reading was taken.</summary>
    [Indexed]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Magnitude of the accelerometer vector (√(x²+y²+z²)) minus gravity (9.81 m/s²).
    /// Values near 0 indicate the device is at rest (deep sleep);
    /// higher values indicate movement (restlessness or wake).
    /// </summary>
    public float AccelerometerMagnitude { get; set; }

    /// <summary>
    /// Raw ambient light level in lux from TYPE_LIGHT.
    /// Values near 0 indicate a dark room (intent to sleep).
    /// </summary>
    public float LightLevel { get; set; }

    /// <summary>
    /// Derived sleep state based on movement and light thresholds.
    /// True  = device owner is likely asleep (low light + low movement).
    /// False = device owner is likely awake.
    /// </summary>
    public bool IsAsleep { get; set; }
}
