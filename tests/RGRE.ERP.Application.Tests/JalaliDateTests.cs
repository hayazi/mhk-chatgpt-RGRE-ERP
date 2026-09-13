using System.Globalization;
using Xunit;
using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Application.Tests;

/// <summary>
/// Regression tests for the jalaali algorithm port. Nowruz dates are pinned
/// to well-known Gregorian anchors (e.g. 1405/01/01 = 21 Mar 2026).
/// </summary>
public class JalaliDateTests
{
    [Theory]
    [InlineData(1405, 1, 1, "2026-03-21")]
    [InlineData(1404, 1, 1, "2025-03-21")]
    [InlineData(1403, 1, 1, "2024-03-20")]
    [InlineData(1402, 1, 1, "2023-03-21")]
    [InlineData(1400, 1, 1, "2021-03-21")]
    public void Nowruz_converts_to_known_gregorian_anchors(
        int jalaliYear, int jalaliMonth, int jalaliDay, string expectedIso)
    {
        var gregorian = JalaliDate.ToGregorian(jalaliYear, jalaliMonth, jalaliDay);

        Assert.Equal(ParseIso(expectedIso), gregorian);
    }

    [Fact]
    public void Leap_year_has_30_days_in_esfand()
    {
        // 1403 is a Jalali leap year (30th of Esfand = 20 Mar 2025).
        Assert.True(JalaliDate.IsLeapYear(1403));
        Assert.Equal(30, JalaliDate.DaysInMonth(1403, 12));
        Assert.Equal(20, JalaliDate.ToGregorian(1403, 12, 30).Day);

        Assert.False(JalaliDate.IsLeapYear(1404));
        Assert.Equal(29, JalaliDate.DaysInMonth(1404, 12));
    }

    [Theory]
    [InlineData(2026, 3, 21, 1405, 1, 1)]
    [InlineData(2026, 9, 12, 1405, 6, 21)]
    [InlineData(2025, 3, 20, 1403, 12, 30)]
    [InlineData(2024, 3, 20, 1403, 1, 1)]
    public void Round_trip_jalali_to_gregorian_preserves_the_date(
        int gy, int gm, int gd, int jy, int jm, int jd)
    {
        var gregorian = new DateTime(gy, gm, gd);

        var (year, month, day) = JalaliDate.ToJalali(gregorian);
        var back = JalaliDate.ToGregorian(jy, jm, jd);

        Assert.Equal((jy, jm, jd), (year, month, day));
        Assert.Equal(gregorian, back);
    }

    [Fact]
    public void Format_and_parse_round_trip()
    {
        var gregorian = new DateTime(2026, 9, 12);
        var formatted = JalaliDate.Format(gregorian);

        Assert.Equal("1405/06/21", formatted);
        Assert.True(JalaliDate.TryParse(formatted, out var parsed));
        Assert.Equal(gregorian, parsed);
    }

    [Theory]
    [InlineData("1405/6/21", "2026-09-12")]
    [InlineData("05/06/21", "2026-09-12")]
    [InlineData("۱۴۰۵/۰۶/۲۱", "2026-09-12")]
    [InlineData("1405-06-21", "2026-09-12")]
    public void TryParse_accepts_common_input_variants(string input, string expectedIso)
    {
        Assert.True(JalaliDate.TryParse(input, out var parsed));
        Assert.Equal(ParseIso(expectedIso), parsed);
    }

    [Fact]
    public void TryParse_rejects_invalid_dates()
    {
        Assert.False(JalaliDate.TryParse("1404/12/30", out _)); // not a leap year
        Assert.False(JalaliDate.TryParse("1405/13/01", out _));
        Assert.False(JalaliDate.TryParse("1405/07/31", out _)); // Mehr has 30 days
        Assert.False(JalaliDate.TryParse("", out _));
        Assert.False(JalaliDate.TryParse(null, out _));
    }

    // The host may run under fa-IR (Persian calendar); pin all parses.
    private static DateTime ParseIso(string iso)
        => DateTime.ParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
