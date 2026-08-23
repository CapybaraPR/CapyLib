namespace Capy.Core.Features;

public class SnowflakesHelper
{
    public const long DiscordEpochMilliseconds = 1420070400000L;

    public static TimeSpan GetTimeSpanFromSnowflake(long snowflake)
    {
        long timestampMs = (snowflake >> 22) + DiscordEpochMilliseconds;
        return TimeSpan.FromMilliseconds(timestampMs);
    }

    public static DateTimeOffset GetDateTimeOffsetFromSnowflake(long snowflake)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds((snowflake >> 22) + DiscordEpochMilliseconds);
    }
}
