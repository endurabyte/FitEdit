namespace Dauer.Data.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToTcx(this System.DateTime dt)
        {
            return dt.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        }
    }
}
