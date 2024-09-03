using Blum.Models;

namespace Blum.Utilities
{
    public class RandomUtility
    {
        public class Random
        {
            public static (int Min, int Max) Range { get; set; } = TelegramSettings.PointsRange;

            public enum Delay
            {
                Account,
                Play,
                ClaimGame,
                ErrorPlay
            }

            public static int RandomPoints() => System.Random.Shared.Next(Range.Min, Range.Max);

            public static int RandomDelayMilliseconds(Delay type) => type switch
            {
                Delay.Account => (int)((System.Random.Shared.NextDouble() * (15.0 - 5.0) + 5.0) * 1000),
                Delay.Play => (int)((System.Random.Shared.NextDouble() * (15.0 - 5.0) + 5.0) * 1000),
                Delay.ClaimGame => (int)((System.Random.Shared.NextDouble() * (50.0 - 40.0) + 40) * 1000),
                Delay.ErrorPlay => (int)((System.Random.Shared.NextDouble() * (180.0 - 60.0) + 60.0) * 1000),
                _ => (int)((System.Random.Shared.NextDouble() * (40.0 - 30.0) + 30) * 1000)
            };

            public static int RandomDelayMilliseconds(double minSeconds, double maxSeconds) =>
                (int)((System.Random.Shared.NextDouble() * (maxSeconds - minSeconds) + minSeconds) * 1000);

            public static int RandomDelayMilliseconds(TimeSpan min, TimeSpan max)
            {
                double minMilliseconds = min.TotalMilliseconds;
                double maxMilliseconds = max.TotalMilliseconds;
                double randomMilliseconds = System.Random.Shared.NextDouble() * (maxMilliseconds - minMilliseconds) + minMilliseconds;
                return (int)randomMilliseconds;
            }
        }
    }
}
