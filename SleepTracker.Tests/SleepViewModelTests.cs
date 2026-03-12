using SleepTracker.ViewModels;
using Xunit;

namespace SleepTracker.Tests;

/// <summary>
/// Unit tests for <see cref="SleepViewModel"/> focusing on the Sleep Debt
/// calculation and the 24-hour energy prediction curve – the two core
/// algorithms described in the problem statement.
/// </summary>
public class SleepViewModelTests
{
    // -----------------------------------------------------------------------
    // Energy curve – structural invariants
    // -----------------------------------------------------------------------

    [Fact]
    public void BuildEnergyCurve_Returns24Points()
    {
        var curve = SleepViewModel.BuildEnergyCurve(sleepDebt: 0);
        Assert.Equal(24, curve.Count);
    }

    [Fact]
    public void BuildEnergyCurve_AllEnergyLevelsInRange()
    {
        var curve = SleepViewModel.BuildEnergyCurve(sleepDebt: 0);
        foreach (var point in curve)
        {
            Assert.InRange(point.EnergyLevel, 0.0, 100.0);
        }
    }

    [Fact]
    public void BuildEnergyCurve_HoursAreSequential()
    {
        var curve = SleepViewModel.BuildEnergyCurve(sleepDebt: 0);
        for (int i = 0; i < curve.Count; i++)
            Assert.Equal(i, curve[i].Hour);
    }

    // -----------------------------------------------------------------------
    // Energy curve – circadian shape
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0)]   // hour 0 = midnight
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(23)]
    public void BuildEnergyCurve_NightHoursHaveLowEnergy(int hour)
    {
        var curve = SleepViewModel.BuildEnergyCurve(sleepDebt: 0);
        // Night hours (outside 06:00–22:00) should have minimal energy.
        Assert.True(curve[hour].EnergyLevel <= 10.0,
            $"Hour {hour} energy should be low (≤10), was {curve[hour].EnergyLevel}");
    }

    [Theory]
    [InlineData(12)]  // noon
    [InlineData(13)]  // early afternoon
    [InlineData(14)]  // peak
    public void BuildEnergyCurve_PeakHoursHaveHighEnergy(int hour)
    {
        var curve = SleepViewModel.BuildEnergyCurve(sleepDebt: 0);
        Assert.True(curve[hour].EnergyLevel >= 60.0,
            $"Hour {hour} energy should be high (≥60), was {curve[hour].EnergyLevel}");
    }

    // -----------------------------------------------------------------------
    // Energy curve – sleep-debt penalty
    // -----------------------------------------------------------------------

    [Fact]
    public void BuildEnergyCurve_SleepDebtReducesPeakEnergy()
    {
        var curveNormal = SleepViewModel.BuildEnergyCurve(sleepDebt: 0);
        var curveDebt   = SleepViewModel.BuildEnergyCurve(sleepDebt: 4); // 4-hour debt

        // Peak energy should be lower when there is significant sleep debt.
        double peakNormal = curveNormal.Max(p => p.EnergyLevel);
        double peakDebt   = curveDebt.Max(p => p.EnergyLevel);
        Assert.True(peakDebt < peakNormal,
            $"Peak energy with debt ({peakDebt}) should be less than without ({peakNormal})");
    }

    [Fact]
    public void BuildEnergyCurve_ExtremeDebtDoesNotProduceNegativeEnergy()
    {
        // 100-hour debt should be fully capped; no negative energy values.
        var curve = SleepViewModel.BuildEnergyCurve(sleepDebt: 100);
        Assert.All(curve, p => Assert.True(p.EnergyLevel >= 0,
            $"Hour {p.Hour} energy was negative: {p.EnergyLevel}"));
    }

    [Fact]
    public void BuildEnergyCurve_ZeroDebtProducesHigherPeakThanLargeDebt()
    {
        var curve0   = SleepViewModel.BuildEnergyCurve(sleepDebt: 0);
        var curve10  = SleepViewModel.BuildEnergyCurve(sleepDebt: 10);

        double peak0  = curve0.Max(p => p.EnergyLevel);
        double peak10 = curve10.Max(p => p.EnergyLevel);

        Assert.True(peak0 > peak10);
    }

    // -----------------------------------------------------------------------
    // EnergyPoint record
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0,  "00:00")]
    [InlineData(6,  "06:00")]
    [InlineData(14, "14:00")]
    [InlineData(23, "23:00")]
    public void EnergyPoint_LabelFormatsCorrectly(int hour, string expectedLabel)
    {
        var point = new EnergyPoint(hour, 50.0);
        Assert.Equal(expectedLabel, point.Label);
    }

    // -----------------------------------------------------------------------
    // Sleep debt constants
    // -----------------------------------------------------------------------

    [Fact]
    public void BaselineSleepHours_IsEightHours()
    {
        Assert.Equal(8.0, SleepViewModel.BaselineSleepHours);
    }

    [Fact]
    public void RollingWindowDays_IsFourteenDays()
    {
        Assert.Equal(14, SleepViewModel.RollingWindowDays);
    }
}
