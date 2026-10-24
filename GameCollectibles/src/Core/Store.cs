using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace GameCollectibles.Core
{
    internal sealed class Store
    {
        private readonly string _path;
        private readonly HashSet<string> _collected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public Store(string directory)
        {
            _path = Path.Combine(directory, "collected.json");
            Load();
        }

        public IReadOnlyCollection<string> Collected => _collected;

        public bool Contains(string id) => _collected.Contains(id);

        public bool Add(string id)
        {
            if (!_collected.Add(id)) return false;
            Save();
            return true;
        }

        public bool Remove(string id)
        {
            if (!_collected.Remove(id)) return false;
            Save();
            return true;
        }

        public void Replace(IEnumerable<string> ids)
        {
            _collected.Clear();
            foreach (string id in ids ?? Enumerable.Empty<string>()) _collected.Add(id);
            Save();
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                List<string> saved = new JavaScriptSerializer().Deserialize<List<string>>(File.ReadAllText(_path));
                if (saved != null) foreach (string id in saved) _collected.Add(id);
            }
            catch (Exception ex)
            {
                Log.Error("collected.json is invalid, starting with nothing collected.", ex);
            }
        }

        private void Save()
        {
            try
            {
                string temp = _path + ".tmp";
                File.WriteAllText(temp, new JavaScriptSerializer().Serialize(_collected.OrderBy(id => id).ToList()));
                if (File.Exists(_path)) File.Delete(_path);
                File.Move(temp, _path);
            }
            catch (Exception ex)
            {
                Log.Error("Could not write collected.json.", ex);
            }
        }
    }
}
