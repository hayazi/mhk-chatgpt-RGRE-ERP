using RGRE.ERP.Domain.Common;

namespace RGRE.ERP.Web.Components.Services;

/// <summary>Small helper for Jalali date binding in page components.</summary>
public static class JalaliUi
{
    /// <summary>Today's date as a Jalali input default (e.g. "1405/06/21").</summary>
    public static string TodayJalali => Format(DateTime.Today);

    public static string Format(DateTime gregorian) => JalaliDate.Format(gregorian);

    public static string Format(DateTime? gregorian, string fallback = "—")
        => JalaliDate.Format(gregorian, fallback);

    /// <summary>Parses Jalali input ("1405/06/21", Persian digits ok) to Gregorian.</summary>
    public static bool TryParse(string? text, out DateTime gregorian)
        => JalaliDate.TryParse(text, out gregorian);
}
