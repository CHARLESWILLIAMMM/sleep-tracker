using SQLite;
using SleepTracker.Models;

namespace SleepTracker.Data;

/// <summary>
/// Provides thread-safe access to the local SQLite database that stores all
/// sensor readings captured by <see cref="SleepTrackingService"/>.
/// </summary>
public class SleepDatabase : IDisposable
{
    private readonly SQLiteAsyncConnection _database;
    private bool _disposed;

    /// <summary>
    /// Initialises the database connection and creates the table if it does
    /// not already exist.
    /// </summary>
    /// <param name="dbPath">
    /// Full file-system path to the SQLite file.  Typically obtained via
    /// <c>Path.Combine(FileSystem.AppDataDirectory, "sleep.db3")</c>.
    /// </param>
    public SleepDatabase(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        // Ensure the table exists – this is a no-op after first run.
        _database.CreateTableAsync<SleepReading>().GetAwaiter().GetResult();
    }

    // -----------------------------------------------------------------------
    // Write operations
    // -----------------------------------------------------------------------

    /// <summary>Persists a new sensor reading to the database.</summary>
    public Task<int> InsertReadingAsync(SleepReading reading)
    {
        reading.Timestamp = reading.Timestamp == default ? DateTime.UtcNow : reading.Timestamp;
        return _database.InsertAsync(reading);
    }

    // -----------------------------------------------------------------------
    // Read operations
    // -----------------------------------------------------------------------

    /// <summary>Returns all readings for a specific UTC calendar date.</summary>
    public Task<List<SleepReading>> GetReadingsForDateAsync(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);
        return _database.Table<SleepReading>()
            .Where(r => r.Timestamp >= startOfDay && r.Timestamp < endOfDay)
            .OrderBy(r => r.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Returns all readings within the last <paramref name="days"/> calendar
    /// days (inclusive of today).
    /// </summary>
    public Task<List<SleepReading>> GetReadingsForLastDaysAsync(int days)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-days + 1);
        return _database.Table<SleepReading>()
            .Where(r => r.Timestamp >= cutoff)
            .OrderBy(r => r.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Calculates the total sleep duration (in hours) for a given UTC date by
    /// summing consecutive "asleep" reading intervals.  Each reading represents
    /// a 5-minute sample window, so a contiguous block of N asleep readings
    /// contributes N × 5 minutes.
    /// </summary>
    public async Task<double> GetSleepDurationHoursAsync(DateTime date)
    {
        var readings = await GetReadingsForDateAsync(date).ConfigureAwait(false);
        if (readings.Count == 0)
            return 0.0;

        // Each reading represents one ~5-minute sampling window.
        const double minutesPerSample = 5.0;
        int asleepCount = readings.Count(r => r.IsAsleep);
        return asleepCount * minutesPerSample / 60.0;
    }

    /// <summary>
    /// Returns a dictionary mapping each UTC date (within the last
    /// <paramref name="days"/> days) to the number of hours slept that day.
    /// </summary>
    public async Task<Dictionary<DateTime, double>> GetDailySleepHoursAsync(int days = 14)
    {
        var result = new Dictionary<DateTime, double>();
        var today = DateTime.UtcNow.Date;

        for (int i = 0; i < days; i++)
        {
            var date = today.AddDays(-i);
            result[date] = await GetSleepDurationHoursAsync(date).ConfigureAwait(false);
        }

        return result;
    }

    // -----------------------------------------------------------------------
    // Cleanup
    // -----------------------------------------------------------------------

    /// <summary>Deletes all readings older than <paramref name="days"/> days.</summary>
    public Task<int> PurgeOldReadingsAsync(int days = 30)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-days);
        // sqlite-net-pcl does not support lambda predicates in DeleteAsync;
        // use a raw SQL DELETE statement instead.  Passing cutoff as a DateTime
        // parameter ensures sqlite-net-pcl serialises it using the same format
        // it used when the rows were originally inserted.
        return _database.ExecuteAsync(
            "DELETE FROM SleepReadings WHERE Timestamp < ?",
            cutoff);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _database.CloseAsync().GetAwaiter().GetResult();
            _disposed = true;
        }
    }
}
