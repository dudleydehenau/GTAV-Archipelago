using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace MissionLauncher.Core
{
    internal sealed class MarkerPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int Sprite { get; set; }
    }

    internal sealed class MarkerMission
    {
        public string Code { get; set; }
        public string Category { get; set; }
        public int GameIndex { get; set; }
        public List<string> Trigger { get; set; } = new List<string>();
        public Dictionary<string, MarkerPoint> Points { get; set; } = new Dictionary<string, MarkerPoint>();
    }

    internal sealed class MarkerFile
    {
        public List<MarkerMission> Missions { get; set; } = new List<MarkerMission>();
    }

    internal static class MarkerData
    {
        public static List<MarkerMission> Load(string directory)
        {
            string path = Path.Combine(directory, "missions.json");
            try
            {
                string json;
                if (File.Exists(path))
                {
                    json = File.ReadAllText(path, Encoding.UTF8);
                    Log.Info("Mission start points loaded from missions.json.");
                }
                else
                {
                    using (Stream stream = typeof(MarkerData).Assembly.GetManifestResourceStream("MissionLauncher.missions.json"))
                    {
                        if (stream == null) return new List<MarkerMission>();
                        using (var reader = new StreamReader(stream)) json = reader.ReadToEnd();
                    }
                }
                MarkerFile file = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<MarkerFile>(json);
                List<MarkerMission> missions = file?.Missions ?? new List<MarkerMission>();
                foreach (MarkerMission mission in missions)
                {
                    if (mission.Trigger == null) mission.Trigger = new List<string>();
                    mission.Points = mission.Points == null
                        ? new Dictionary<string, MarkerPoint>(StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, MarkerPoint>(mission.Points, StringComparer.OrdinalIgnoreCase);
                }
                return missions;
            }
            catch (Exception ex)
            {
                Log.Error("Could not read the mission start points.", ex);
                return new List<MarkerMission>();
            }
        }
    }

    internal sealed class MarkerStore
    {
        private readonly string _path;
        private readonly Dictionary<int, MissionMarkerState> _states = new Dictionary<int, MissionMarkerState>();

        public MarkerStore(string directory)
        {
            _path = Path.Combine(directory, "markers.json");
            Load();
        }

        public int Revision { get; private set; }

        public MissionMarkerState Get(int number) => _states.TryGetValue(number, out MissionMarkerState state) ? state : MissionMarkerState.Hidden;

        public void Set(int number, MissionMarkerState state)
        {
            if (Get(number) == state) return;
            if (state == MissionMarkerState.Hidden) _states.Remove(number);
            else _states[number] = state;
            Revision++;
            Save();
        }

        public void Clear()
        {
            if (_states.Count == 0) return;
            _states.Clear();
            Revision++;
            Save();
        }

        private void Load()
        {
            if (!File.Exists(_path)) return;
            try
            {
                var raw = new JavaScriptSerializer().Deserialize<Dictionary<string, int>>(File.ReadAllText(_path, Encoding.UTF8));
                if (raw == null) return;
                foreach (KeyValuePair<string, int> kv in raw)
                    if (int.TryParse(kv.Key, out int number) && Enum.IsDefined(typeof(MissionMarkerState), kv.Value))
                        _states[number] = (MissionMarkerState)kv.Value;
            }
            catch (Exception ex)
            {
                Log.Error("markers.json is invalid, ignored.", ex);
            }
        }

        private void Save()
        {
            try
            {
                var raw = new Dictionary<string, int>();
                foreach (KeyValuePair<int, MissionMarkerState> kv in _states) raw[kv.Key.ToString()] = (int)kv.Value;
                string temp = _path + ".tmp";
                File.WriteAllText(temp, new JavaScriptSerializer().Serialize(raw), Encoding.UTF8);
                File.Copy(temp, _path, true);
                File.Delete(temp);
            }
            catch (Exception ex)
            {
                Log.Error("Could not write markers.json.", ex);
            }
        }
    }
}
