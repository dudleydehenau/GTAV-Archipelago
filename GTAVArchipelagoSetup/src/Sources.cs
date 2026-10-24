using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace GTAVArchipelagoSetup
{
    internal sealed class Component
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Repo { get; set; }
        public string Asset { get; set; }
        public string Url { get; set; }
        public bool Prerelease { get; set; }
        public string Target { get; set; } = "Mods";
        public List<string> Files { get; set; }

        [ScriptIgnore]
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Url) || (!string.IsNullOrWhiteSpace(Repo) && !Repo.StartsWith("YOUR_GITHUB_NAME", StringComparison.OrdinalIgnoreCase));

        public override string ToString() => string.IsNullOrEmpty(Description) ? Name : $"{Name}  -  {Description}";
    }

    internal sealed class SourceList
    {
        public List<Component> Components { get; set; } = new List<Component>();

        public static SourceList Load(out string origin)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sources.json");
            if (File.Exists(path))
            {
                try
                {
                    SourceList list = new JavaScriptSerializer().Deserialize<SourceList>(File.ReadAllText(path));
                    if (list?.Components != null && list.Components.Count > 0)
                    {
                        origin = path;
                        return list;
                    }
                }
                catch (Exception ex)
                {
                    origin = "built-in list (sources.json is invalid: " + ex.Message + ")";
                    return new JavaScriptSerializer().Deserialize<SourceList>(Defaults.Sources);
                }
            }
            origin = "built-in list";
            return new JavaScriptSerializer().Deserialize<SourceList>(Defaults.Sources);
        }
    }
}
