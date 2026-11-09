using System.Diagnostics;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal static class TickTimer
    {
        private const double SlowMs = 8.0;
        private const int LogGap = 2000;
        private static readonly Stopwatch Watch = Stopwatch.StartNew();
        private static long _start;
        private static long _mark;
        private static long _slowestTicks;
        private static string _slowest;
        private static int _nextLog;
        private static int _frame;

        public static void Begin()
        {
            _start = _mark = Watch.ElapsedTicks;
            _frame = Function.Call<int>(Hash.GET_FRAME_COUNT);
            _slowestTicks = 0;
            _slowest = null;
        }

        public static void Lap(string name)
        {
            long now = Watch.ElapsedTicks;
            long spent = now - _mark;
            _mark = now;
            if (spent <= _slowestTicks) return;
            _slowestTicks = spent;
            _slowest = name;
        }

        public static void End()
        {
            double total = Ms(Watch.ElapsedTicks - _start);
            if (total < SlowMs || System.Environment.TickCount < _nextLog) return;
            if (Function.Call<int>(Hash.GET_FRAME_COUNT) != _frame) return;
            _nextLog = System.Environment.TickCount + LogGap;
            Log.Warn($"Slow frame: GameEnvironment took {total:0.0} ms, mostly in {_slowest ?? "setup"} ({Ms(_slowestTicks):0.0} ms).");
        }

        private static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
    }
}
