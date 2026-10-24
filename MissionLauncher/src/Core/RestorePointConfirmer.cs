using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using GTA.Native;

namespace MissionLauncher.Core
{
    internal sealed class RestorePointConfirmer
    {
        private const int ArmDuration = 30000;
        private const int KeyFallbackDelay = 600;
        private const int KeyInterval = 500;
        private const int KeyHold = 80;
        private const int MaxKeys = 12;
        private const int StallDelay = 400;
        private const int WatchdogInterval = 600;
        private const int MaxWatchdogKeys = 10;
        private const int SaveInProgress = 1;
        private const int FrontendControls = 2;
        private static readonly int[] AcceptControls = { 201, 191, 215 };
        private const byte VirtualKeyReturn = 0x0D;
        private const byte ScanCodeReturn = 0x1C;
        private const uint KeyUp = 0x0002;
        private const uint ScanCode = 0x0008;

        private readonly bool _keyFallback;
        private readonly object _keyLock = new object();
        private volatile int _armedUntil = -1;
        private volatile int _lastTickAt = Environment.TickCount;
        private volatile int _watchdogKeys;
        private volatile bool _stallSeen;
        private Thread _watchdog;
        private int _watchdogReported;
        private int _pendingSince = -1;
        private int _lastKeyAt = -1;
        private int _keyDownAt = -1;
        private int _keysSent;
        private bool _warningSeen;
        private int _frame;

        public RestorePointConfirmer(bool keyFallback)
        {
            _keyFallback = keyFallback;
        }

        public void Arm()
        {
            ReleaseKey();
            _lastTickAt = Environment.TickCount;
            _pendingSince = -1;
            _lastKeyAt = -1;
            _keysSent = 0;
            _warningSeen = false;
            _watchdogKeys = 0;
            _watchdogReported = 0;
            _stallSeen = false;
            _armedUntil = Environment.TickCount + ArmDuration;
            if (_keyFallback) StartWatchdog();
        }

        public void Tick()
        {
            int now = Environment.TickCount;
            int gap = now - _lastTickAt;
            _lastTickAt = now;
            ReportWatchdog(gap);
            if (_keyDownAt >= 0 && now - _keyDownAt >= KeyHold) ReleaseKey();
            if (_armedUntil < 0) return;
            if (now > _armedUntil)
            {
                ReleaseKey();
                Log.Info($"Restore point watch ended ({_keysSent} Enter key presses from the script, {_watchdogKeys} from the watchdog, warning screen {(_warningSeen ? "seen" : "not seen")}).");
                _armedUntil = -1;
                return;
            }

            bool warning = Function.Call<bool>(Hash.IS_WARNING_MESSAGE_ACTIVE);
            int status = Function.Call<int>(Hash.GET_STATUS_OF_MISSION_REPEAT_SAVE);
            bool fadedOut = Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT);
            bool pending = warning || (status == SaveInProgress && fadedOut);

            if (!pending)
            {
                if (_pendingSince >= 0)
                {
                    ReleaseKey();
                    Log.Info($"Restore point done after {now - _pendingSince} ms (save status {status}, {_keysSent} Enter key presses from the script, {_watchdogKeys} from the watchdog, warning screen {(_warningSeen ? "seen" : "not seen")}).");
                    _armedUntil = -1;
                    _pendingSince = -1;
                }
                return;
            }

            if (_pendingSince < 0)
            {
                _pendingSince = now;
                Log.Info($"Restore point save started (save status {status}, warning screen {warning}, faded out {fadedOut}), confirming it.");
            }
            if (warning && !_warningSeen)
            {
                _warningSeen = true;
                Log.Info($"Restore point warning screen detected (message {Function.Call<int>(Hash.GET_WARNING_SCREEN_MESSAGE_HASH)}).");
            }

            _frame++;
            if ((_frame & 1) == 0)
                foreach (int control in AcceptControls)
                    Function.Call(Hash.SET_CONTROL_VALUE_NEXT_FRAME, FrontendControls, control, 1f);

            if (_keyFallback && _keyDownAt < 0 && _keysSent < MaxKeys && now - _pendingSince > KeyFallbackDelay
                && (_lastKeyAt < 0 || now - _lastKeyAt > KeyInterval) && IsGameFocused())
            {
                _lastKeyAt = now;
                _keyDownAt = now;
                _keysSent++;
                lock (_keyLock) keybd_event(VirtualKeyReturn, ScanCodeReturn, ScanCode, UIntPtr.Zero);
            }
        }

        private void ReportWatchdog(int gap)
        {
            if (_stallSeen && gap >= StallDelay)
                Log.Info($"Scripts were frozen by the game for {gap} ms while the restore point was pending.");
            _stallSeen = false;
            int keys = _watchdogKeys;
            if (keys == _watchdogReported) return;
            _watchdogReported = keys;
            Log.Info($"Restore point watchdog: {keys} Enter key presses sent while the scripts were frozen.");
        }

        private void StartWatchdog()
        {
            if (_watchdog != null && _watchdog.IsAlive) return;
            _watchdog = new Thread(WatchdogLoop) { IsBackground = true, Name = "MissionLauncher restore point watchdog" };
            _watchdog.Start();
        }

        private void WatchdogLoop()
        {
            try
            {
                while (true)
                {
                    int armedUntil = _armedUntil;
                    int now = Environment.TickCount;
                    if (armedUntil < 0 || now > armedUntil) return;
                    if (now - _lastTickAt >= StallDelay)
                    {
                        _stallSeen = true;
                        if (_watchdogKeys < MaxWatchdogKeys && IsGameFocused())
                        {
                            lock (_keyLock)
                            {
                                keybd_event(VirtualKeyReturn, ScanCodeReturn, ScanCode, UIntPtr.Zero);
                                Thread.Sleep(KeyHold);
                                keybd_event(VirtualKeyReturn, ScanCodeReturn, ScanCode | KeyUp, UIntPtr.Zero);
                            }
                            _watchdogKeys++;
                            Thread.Sleep(WatchdogInterval);
                            continue;
                        }
                    }
                    Thread.Sleep(50);
                }
            }
            catch
            {
            }
        }

        private void ReleaseKey()
        {
            if (_keyDownAt < 0) return;
            _keyDownAt = -1;
            lock (_keyLock) keybd_event(VirtualKeyReturn, ScanCodeReturn, ScanCode | KeyUp, UIntPtr.Zero);
        }

        private static bool IsGameFocused()
        {
            try
            {
                IntPtr foreground = GetForegroundWindow();
                if (foreground == IntPtr.Zero) return false;
                GetWindowThreadProcessId(foreground, out uint processId);
                return processId == (uint)Process.GetCurrentProcess().Id;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    }
}
