#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Diagnostics;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal static class StopwatchExtension
{
    extension(Stopwatch)
    {
        public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) => TimeSpan.FromSeconds((double)(endingTimestamp - startingTimestamp) / Stopwatch.Frequency);
        
        public static TimeSpan GetElapsedTime(long startingTimestamp) => GetElapsedTime(startingTimestamp, Stopwatch.GetTimestamp());
    }
}
