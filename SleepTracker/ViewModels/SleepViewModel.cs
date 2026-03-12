using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SleepTracker.Data;

namespace SleepTracker.ViewModels;

/// <summary>
/// Drives the main UI.  Exposes:
/// <list type="bullet">
///   <item>Sleep Debt – rolling 14-day average sleep vs. the 8-hour baseline.</item>
///   <item>24-hour Energy Curve – a simple sinusoidal circadian model offset by
///         the user's calculated sleep debt.</item>
///   <item>Tracking controls to start/stop the background service.</item>
/// </list>
/// </summary>
public partial class SleepViewModel : ObservableObject
{
    // -----------------------------------------------------------------------
    // Constants
    // -----------------------------------------------------------------------

    /// <summary>Target/baseline sleep duration in hours.</summary>
    public const double BaselineSleepHours = 8.0;

    /// <summary>Number of days used for the rolling average.</summary>
    public const int RollingWindowDays = 14;

    // -----------------------------------------------------------------------
    // Dependencies
    // -----------------------------------------------------------------------

    private readonly SleepDatabase _database;

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    public SleepViewModel(SleepDatabase database)
    {
        _database = database;
    }

    // -----------------------------------------------------------------------
    // Observable properties
    // -----------------------------------------------------------------------

    /// <summary>
    /// Cumulative sleep debt in hours.  Positive = under-slept; negative =
    /// over-slept compared with the 8-hour baseline.
    /// </summary>
    [ObservableProperty]
    private double _sleepDebt;

    /// <summary>
    /// Average daily sleep duration (hours) over the rolling 14-day window.
    /// </summary>
    [ObservableProperty]
    private double _averageSleepHours;

    /// <summary>Last night's total sleep duration in hours.</summary>
    [ObservableProperty]
    private double _lastNightSleepHours;

    /// <summary>
    /// Human-readable summary of the current sleep debt, e.g. "−2.5 h debt".
    /// </summary>
    [ObservableProperty]
    private string _sleepDebtSummary = string.Empty;

    /// <summary>
    /// 24 energy level values (one per hour, starting at midnight / 00:00).
    /// Each value is in the range [0, 100] where 100 = peak energy.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<EnergyPoint> _energyCurve = Array.Empty<EnergyPoint>();

    /// <summary>
    /// Indicates whether the background sleep tracking service is running.
    /// </summary>
    [ObservableProperty]
    private bool _isTracking;

    /// <summary>
    /// Status text shown below the tracking toggle.
    /// </summary>
    [ObservableProperty]
    private string _trackingStatus = "Tracking stopped";

    // -----------------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads (or refreshes) sleep data from the database and recalculates all
    /// derived metrics.
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        // 1. Fetch per-day sleep hours over the rolling window.
        var dailyHours = await _database.GetDailySleepHoursAsync(RollingWindowDays)
            .ConfigureAwait(false);

        // 2. Rolling 14-day average — only over days that have actual readings.
        //    Days with no sensor data (0.0 h) are excluded so a fresh install
        //    shows 0 h rather than a falsely-low average.
        var loggedHours = dailyHours.Values.Where(h => h > 0).ToList();
        AverageSleepHours = loggedHours.Count > 0
            ? Math.Round(loggedHours.Average(), 2)
            : 0.0;

        // 3. Last night's sleep (yesterday's UTC date).
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        LastNightSleepHours = dailyHours.TryGetValue(yesterday, out double lastNight)
            ? lastNight : 0.0;

        // 4. Sleep Debt = Σ (baseline − actual) over LOGGED days only.
        //    Days with no readings are excluded: "unlogged" ≠ "0 h slept".
        //    Without this guard a fresh install shows 14 × 8 = 112 h of debt.
        double totalDebt = loggedHours.Sum(h => BaselineSleepHours - h);
        SleepDebt = Math.Round(totalDebt, 2);

        SleepDebtSummary = SleepDebt >= 0
            ? $"{SleepDebt:F1} h sleep debt"
            : $"{Math.Abs(SleepDebt):F1} h sleep surplus";

        // 5. Build 24-hour energy prediction curve.
        EnergyCurve = BuildEnergyCurve(SleepDebt);
    }

    /// <summary>
    /// Toggles the foreground sleep-tracking service on or off.
    /// </summary>
    [RelayCommand]
    public void ToggleTracking()
    {
        IsTracking = !IsTracking;
        TrackingStatus = IsTracking ? "Tracking in progress…" : "Tracking stopped";

#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity
            as SleepTracker.Platforms.Android.MainActivity;

        if (IsTracking)
            activity?.StartSleepTrackingService();
        else
            activity?.StopSleepTrackingService();
#endif
    }

    // -----------------------------------------------------------------------
    // Energy-curve calculation
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds a 24-point circadian energy curve for the current day, adjusted
    /// for accumulated sleep debt.
    ///
    /// Model:
    ///   base(h) = 50 + 50 × sin( π × (h − 6) / 16 )  for h ∈ [6, 22]
    ///   base(h) = 0                                      otherwise
    ///
    /// The peak near 14:00 (~2 PM) and the trough near 22:00–06:00 mirrors
    /// the classic two-process model of sleep regulation.
    ///
    /// Sleep-debt penalty: for every accumulated hour of debt we reduce the
    /// peak by up to 5 percentage points (capped at −40).
    /// </summary>
    /// <param name="sleepDebt">
    /// Accumulated sleep debt in hours (positive = under-slept).
    /// </param>
    /// <returns>
    /// List of 24 <see cref="EnergyPoint"/> values, one per hour 00–23.
    /// </returns>
    public static IReadOnlyList<EnergyPoint> BuildEnergyCurve(double sleepDebt)
    {
        const double wakeHour = 6.0;   // approx wake time
        const double sleepHour = 22.0; // approx sleep time
        const double activeSpan = sleepHour - wakeHour; // 16 hours

        // Clamp debt penalty to avoid negative energy levels.
        double penalty = Math.Min(sleepDebt * 5.0, 40.0);

        var points = new List<EnergyPoint>(24);

        for (int hour = 0; hour < 24; hour++)
        {
            double energy;

            if (hour >= wakeHour && hour <= sleepHour)
            {
                // Sinusoidal arc from 0 → peak → 0 over the active window.
                double phase = Math.PI * (hour - wakeHour) / activeSpan;
                energy = 50.0 + 50.0 * Math.Sin(phase) - penalty;
                energy = Math.Max(energy, 0.0);
                energy = Math.Min(energy, 100.0);
            }
            else
            {
                // Night hours – minimal energy (person is asleep).
                energy = 5.0;
            }

            points.Add(new EnergyPoint(hour, Math.Round(energy, 1)));
        }

        return points.AsReadOnly();
    }
}

/// <summary>A single data point on the 24-hour energy prediction curve.</summary>
/// <param name="Hour">Hour of day (0–23).</param>
/// <param name="EnergyLevel">Predicted energy level (0–100).</param>
public record EnergyPoint(int Hour, double EnergyLevel)
{
    /// <summary>Formatted label for axis display, e.g. "14:00".</summary>
    public string Label => $"{Hour:D2}:00";
}
