namespace OrderBookPrchkas
{
    public static class Extensions
    {
        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static double UnixTimeFromDateTime(this DateTime dateTime)
        {
            return (dateTime - unixEpoch).TotalMilliseconds / 1000;
        }
    }
}
