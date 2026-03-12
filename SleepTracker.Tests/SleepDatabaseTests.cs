using SleepTracker.Data;
using SleepTracker.Models;
using Xunit;

namespace SleepTracker.Tests;

/// <summary>
/// Integration tests for <see cref="SleepDatabase"/> that exercise the
/// SQLite persistence layer using a temporary in-memory / temp-file database.
/// </summary>
public class SleepDatabaseTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SleepDatabase _db;

    public SleepDatabaseTests()
    {
        // Use a unique temp file for each test class instance so tests are isolated.
        _dbPath = Path.Combine(Path.GetTempPath(), $"sleep_test_{Guid.NewGuid():N}.db3");
        _db = new SleepDatabase(_dbPath);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    // -----------------------------------------------------------------------
    // Insert + retrieve
    // -----------------------------------------------------------------------

    [Fact]
    public async Task InsertReadingAsync_PersistsRow()
    {
        var reading = new SleepReading
        {
            Timestamp = DateTime.UtcNow,
            AccelerometerMagnitude = 0.5f,
            LightLevel = 2.0f,
            IsAsleep = true
        };

        int rowsAffected = await _db.InsertReadingAsync(reading);
        Assert.Equal(1, rowsAffected);
    }

    [Fact]
    public async Task GetReadingsForDateAsync_ReturnsOnlyMatchingDate()
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        await _db.InsertReadingAsync(new SleepReading
        {
            Timestamp = today.AddHours(2),
            AccelerometerMagnitude = 0.3f,
            LightLevel = 1f,
            IsAsleep = true
        });

        await _db.InsertReadingAsync(new SleepReading
        {
            Timestamp = yesterday.AddHours(22),
            AccelerometerMagnitude = 0.3f,
            LightLevel = 1f,
            IsAsleep = true
        });

        var todayReadings = await _db.GetReadingsForDateAsync(today);
        Assert.Single(todayReadings);
        Assert.Equal(today.Date, todayReadings[0].Timestamp.Date);
    }

    // -----------------------------------------------------------------------
    // Sleep duration calculation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetSleepDurationHoursAsync_ReturnsZeroForEmptyDay()
    {
        double hours = await _db.GetSleepDurationHoursAsync(DateTime.UtcNow.Date);
        Assert.Equal(0.0, hours);
    }

    [Fact]
    public async Task GetSleepDurationHoursAsync_CountsOnlyAsleepReadings()
    {
        var today = DateTime.UtcNow.Date;

        // Insert 12 asleep readings (12 × 5 min = 60 min = 1 hour).
        for (int i = 0; i < 12; i++)
        {
            await _db.InsertReadingAsync(new SleepReading
            {
                Timestamp = today.AddMinutes(i * 5),
                AccelerometerMagnitude = 0.1f,
                LightLevel = 0f,
                IsAsleep = true
            });
        }

        // Insert 3 awake readings that should NOT count.
        for (int i = 12; i < 15; i++)
        {
            await _db.InsertReadingAsync(new SleepReading
            {
                Timestamp = today.AddMinutes(i * 5),
                AccelerometerMagnitude = 5f,
                LightLevel = 300f,
                IsAsleep = false
            });
        }

        double hours = await _db.GetSleepDurationHoursAsync(today);
        // 12 samples × 5 minutes / 60 = 1.0 hour.
        Assert.Equal(1.0, hours);
    }

    // -----------------------------------------------------------------------
    // Rolling daily sleep hours
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetDailySleepHoursAsync_ReturnsDictionaryWithExpectedDays()
    {
        var result = await _db.GetDailySleepHoursAsync(days: 7);
        Assert.Equal(7, result.Count);
    }

    [Fact]
    public async Task GetDailySleepHoursAsync_AllValuesNonNegative()
    {
        var result = await _db.GetDailySleepHoursAsync(days: 14);
        foreach (var (_, hours) in result)
            Assert.True(hours >= 0.0, $"Sleep hours should be non-negative, was {hours}");
    }

    // -----------------------------------------------------------------------
    // Purge
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PurgeOldReadingsAsync_RemovesOldRows()
    {
        var oldDate = DateTime.UtcNow.Date.AddDays(-40);

        await _db.InsertReadingAsync(new SleepReading
        {
            Timestamp = oldDate,
            AccelerometerMagnitude = 0.1f,
            LightLevel = 0f,
            IsAsleep = true
        });

        int deleted = await _db.PurgeOldReadingsAsync(days: 30);
        Assert.Equal(1, deleted);
    }

    [Fact]
    public async Task PurgeOldReadingsAsync_DoesNotRemoveRecentRows()
    {
        await _db.InsertReadingAsync(new SleepReading
        {
            Timestamp = DateTime.UtcNow,
            AccelerometerMagnitude = 0.1f,
            LightLevel = 0f,
            IsAsleep = true
        });

        int deleted = await _db.PurgeOldReadingsAsync(days: 30);
        Assert.Equal(0, deleted);
    }
}
