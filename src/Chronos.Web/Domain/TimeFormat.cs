namespace Chronos.Web.Domain;

public static class TimeFormat
{
    public static string Hmm(int minutes)
    {
        var sign = minutes < 0 ? "-" : "";
        var m = Math.Abs(minutes);
        return $"{sign}{m / 60}:{m % 60:00}";
    }
}
