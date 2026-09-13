namespace RGRE.ERP.Domain.Common;

/// <summary>
/// Jalali (Persian / Shamsi) calendar conversion. Ports the widely used
/// Khayyam-Birashk "jalaali" algorithm (same tables as jalaali-js) to C#,
/// replacing the legacy <c>StrDate</c> / <c>Calender</c> helpers.
/// All operations work on calendar dates (time-of-day is ignored).
/// </summary>
public static class JalaliDate
{
    /// <summary>Supported Jalali year range (same as the reference tables).</summary>
    public const int MinYear = -61;
    public const int MaxYear = 3177;

    private static readonly int[] Breaks =
    {
        -61, 9, 38, 199, 426, 686, 756, 818, 1111, 1181, 1210,
        1635, 2060, 2097, 2192, 2262, 2324, 2394, 2456, 3178,
    };

    public static readonly string[] MonthNames =
    {
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند",
    };

    // ---------- conversions ----------

    /// <summary>Converts a Gregorian date to its Jalali (year, month, day).</summary>
    public static (int Year, int Month, int Day) ToJalali(DateTime gregorianDate)
    {
        var date = gregorianDate.Date;
        var jdn = GregorianToDayNumber(date.Year, date.Month, date.Day);
        return DayNumberToJalali(jdn);
    }

    /// <summary>Converts a Jalali (year, month, day) to the Gregorian date at midnight.</summary>
    public static DateTime ToGregorian(int jalaliYear, int jalaliMonth, int jalaliDay)
    {
        if (!IsValid(jalaliYear, jalaliMonth, jalaliDay))
            throw new ArgumentOutOfRangeException(
                nameof(jalaliYear),
                $"Invalid Jalali date {jalaliYear:0000}/{jalaliMonth:00}/{jalaliDay:00}.");

        var jdn = JalaliToDayNumber(jalaliYear, jalaliMonth, jalaliDay);
        var g = DayNumberToGregorian(jdn);
        return new DateTime(g.Year, g.Month, g.Day);
    }

    /// <summary>Leap Jalali years have 30 days in Esfand (month 12).</summary>
    public static bool IsLeapYear(int jalaliYear) => JalCal(jalaliYear).Leap == 0;

    public static int DaysInMonth(int jalaliYear, int jalaliMonth)
        => jalaliMonth <= 6
            ? 31
            : jalaliMonth <= 11
                ? 30
                : IsLeapYear(jalaliYear) ? 30 : 29;

    public static bool IsValid(int jalaliYear, int jalaliMonth, int jalaliDay)
        => jalaliYear >= MinYear
            && jalaliYear <= MaxYear
            && jalaliMonth >= 1
            && jalaliMonth <= 12
            && jalaliDay >= 1
            && jalaliDay <= DaysInMonth(jalaliYear, jalaliMonth);

    // ---------- formatting / parsing ----------

    /// <summary>Formats a Gregorian date as <c>1405/06/21</c>.</summary>
    public static string Format(DateTime gregorianDate)
    {
        var (y, m, d) = ToJalali(gregorianDate);
        return $"{y:0000}/{m:00}/{d:00}";
    }

    public static string Format(DateTime? gregorianDate, string fallback = "—")
        => gregorianDate is null ? fallback : Format(gregorianDate.Value);

    /// <summary>
    /// Parses Jalali input such as <c>1405/06/21</c>, <c>1405-6-21</c>,
    /// two-digit years (<c>05/06/21</c>) and Persian digits into a Gregorian date.
    /// </summary>
    public static bool TryParse(string? text, out DateTime gregorian)
    {
        gregorian = default;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = NormalizePersianDigits(text.Trim());

        var parts = text.Split('/', '-', '.');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out var year)
            || !int.TryParse(parts[1], out var month)
            || !int.TryParse(parts[2], out var day))
        {
            return false;
        }

        if (year < 100)
            year += year < 50 ? 1400 : 1300;

        if (!IsValid(year, month, day))
            return false;

        gregorian = ToGregorian(year, month, day);
        return true;
    }

    /// <summary>Converts Persian digits (۰-۹) to ASCII digits.</summary>
    public static string NormalizePersianDigits(string text)
    {
        const string persian = "۰۱۲۳۴۵۶۷۸۹";
        const string arabic = "٠١٢٣٤٥٦٧٨٩";

        foreach (var c in text)
        {
            var index = persian.IndexOf(c);
            if (index >= 0)
                text = text.Replace(c, (char)('0' + index));
            else
            {
                index = arabic.IndexOf(c);
                if (index >= 0)
                    text = text.Replace(c, (char)('0' + index));
            }
        }

        return text;
    }

    // ---------- core algorithm (port of jalaali-js) ----------

    private static (int Leap, int Gy, int March) JalCal(int jy)
    {
        var bl = Breaks.Length;
        var gy = jy + 621;
        var leapJ = -14;
        var jp = Breaks[0];
        int jump = 0;

        if (jy < jp || jy >= Breaks[bl - 1])
            throw new ArgumentOutOfRangeException(
                nameof(jy),
                $"Jalali year {jy} is outside the supported range {MinYear}..{MaxYear - 1}.");

        for (var i = 1; i < bl; i++)
        {
            var jm = Breaks[i];
            jump = jm - jp;

            if (jy < jm)
                break;

            leapJ += Div(jump, 33) * 8 + Div(jump % 33, 4);
            jp = jm;
        }

        var n = jy - jp;
        leapJ += Div(n, 33) * 8 + Div(n % 33 + 3, 4);

        if (jump % 33 == 4 && jump - n == 4)
            leapJ += 1;

        var leapG = Div(gy, 4) - Div((Div(gy, 100) + 1) * 3, 4) - 150;
        var march = 20 + leapJ - leapG;

        if (jump - n < 6)
            n = n - jump + Div(jump + 4, 33) * 33;

        var leap = (n + 1) % 33 - 1;
        leap %= 4;

        if (leap == -1)
            leap = 4;

        return (leap, gy, march);
    }

    private static int JalaliToDayNumber(int jy, int jm, int jd)
    {
        var r = JalCal(jy);
        return GregorianToDayNumber(r.Gy, 3, r.March)
            + (jm - 1) * 31
            - Div(jm, 7) * (jm - 7)
            + jd - 1;
    }

    private static (int Year, int Month, int Day) DayNumberToJalali(int jdn)
    {
        var gy = DayNumberToGregorian(jdn).Year;
        var jy = gy - 621;
        var r = JalCal(jy);
        var jdn1f = GregorianToDayNumber(gy, 3, r.March);
        var k = jdn - jdn1f;

        if (k >= 0)
        {
            if (k <= 185)
                return (jy, 1 + Div(k, 31), k % 31 + 1);

            k -= 186;
        }
        else
        {
            jy -= 1;
            k += 179;

            if (r.Leap == 1)
                k += 1;
        }

        return (jy, 7 + Div(k, 30), k % 30 + 1);
    }

    private static int GregorianToDayNumber(int gy, int gm, int gd)
    {
        var d = Div((gy + Div(gm - 8, 6) + 100100) * 1461, 4)
            + Div(153 * ((gm + 9) % 12) + 2, 5)
            + gd
            - 34840408;

        d = d - Div(Div(gy + 100100 + Div(gm - 8, 6), 100) * 3, 4) + 752;
        return d;
    }

    private static (int Year, int Month, int Day) DayNumberToGregorian(int jdn)
    {
        var j = 4 * jdn + 139361631;
        j = j + Div(Div(4 * jdn + 183187720, 146097) * 3, 4) * 4 - 3908;

        var i = Div(j % 1461, 4) * 5 + 308;
        var gd = Div(i % 153, 5) + 1;
        var gm = Div(i, 153) % 12 + 1;
        var gy = Div(j, 1461) - 100100 + Div(8 - gm, 6);

        return (gy, gm, gd);
    }

    /// <summary>Truncating division, matching the JS reference implementation.</summary>
    private static int Div(int a, int b) => a / b;
}
