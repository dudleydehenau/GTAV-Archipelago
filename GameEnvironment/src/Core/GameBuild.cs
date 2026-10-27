using System;
using System.Diagnostics;

namespace GameEnvironment.Core
{
    internal static class GameBuild
    {
        private static int? _number;
        private static string _version;

        public static int Number
        {
            get
            {
                if (_number == null) Detect();
                return _number.Value;
            }
        }

        public static string Describe()
        {
            if (_number == null) Detect();
            return _version;
        }

        private static void Detect()
        {
            _number = 0;
            _version = "unknown";
            try
            {
                FileVersionInfo info = Process.GetCurrentProcess().MainModule.FileVersionInfo;
                _version = info.FileVersion ?? "unknown";
                _number = info.FileBuildPart;
            }
            catch (Exception ex)
            {
                Log.Warn("Could not read the game version: " + ex.Message);
            }
        }
    }
}
