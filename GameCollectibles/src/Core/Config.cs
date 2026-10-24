using System;
using System.IO;
using System.Web.Script.Serialization;

namespace GameCollectibles.Core
{
    internal enum BlipMode
    {
        None,
        Nearby,
        All,
    }

    internal sealed class CollectiblesConfig
    {
        public bool EnableMenu { get; set; }
        public string MenuKey { get; set; } = "F10";
        public bool VerboseLog { get; set; }
        public string Model { get; set; } = "prop_ld_case_01";
        public float Size { get; set; } = 1f;
        public float HoverHeight { get; set; } = 0.6f;
        public float SpinSpeed { get; set; } = 90f;
        public bool Glow { get; set; } = true;
        public float StreamDistance { get; set; } = 150f;
        public float PickupRadius { get; set; } = 1.6f;
        public float VehiclePickupRadius { get; set; } = 3f;
        public float UnderwaterPickupRadius { get; set; } = 4.5f;
        public string Blips { get; set; } = "Nearby";
        public float BlipDistance { get; set; } = 250f;
        public bool HideGameCollectibles { get; set; } = true;
        public bool Notify { get; set; } = true;
        public bool SpaceshipParts { get; set; } = true;
        public bool LetterScraps { get; set; } = true;
        public bool NuclearWaste { get; set; } = true;
        public bool SubmarineParts { get; set; } = true;

        internal BlipMode BlipMode
        {
            get => Enum.TryParse(Blips, true, out BlipMode mode) ? mode : BlipMode.Nearby;
            set => Blips = value.ToString();
        }

        public static CollectiblesConfig Load(string directory)
        {
            string path = Path.Combine(directory, "config.json");
            CollectiblesConfig config = null;
            if (File.Exists(path))
            {
                try
                {
                    config = new JavaScriptSerializer().Deserialize<CollectiblesConfig>(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                    Log.Error("config.json is invalid, using defaults.", ex);
                }
            }
            else
            {
                Log.Info("config.json not found, using defaults.");
            }
            config = config ?? new CollectiblesConfig();
            if (string.IsNullOrWhiteSpace(config.Model)) config.Model = "prop_ld_case_01";
            if (config.Size <= 0.05f || config.Size > 5f) config.Size = 1f;
            if (config.StreamDistance < 30f) config.StreamDistance = 30f;
            if (config.PickupRadius <= 0f) config.PickupRadius = 1.6f;
            config._path = path;
            return config;
        }

        private string _path;

        public void Save()
        {
            if (_path == null) return;
            try
            {
                File.WriteAllText(_path, new JavaScriptSerializer().Serialize(this));
            }
            catch (Exception ex)
            {
                Log.Error("Could not write config.json.", ex);
            }
        }
    }
}
