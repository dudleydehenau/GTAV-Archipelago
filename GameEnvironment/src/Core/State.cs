using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace GameEnvironment.Core
{
    internal sealed class EnvironmentState
    {
        public int Version { get; set; } = 1;
        public bool Sterile { get; set; }
        public string CreatedAt { get; set; }
        public string Owner { get; set; }
        public int StartCharacter { get; set; } = -1;
        public int PendingStartCharacter { get; set; } = -1;
        public int LastCharacter { get; set; } = -1;
        public List<CharacterRecord> Characters { get; set; } = new List<CharacterRecord>();
        public List<VehicleRecord> Vehicles { get; set; } = new List<VehicleRecord>();
        public int NextVehicleId { get; set; } = 1;
        public bool VehicleHelpShown { get; set; }

        public CharacterRecord For(PlayableCharacter character) => Characters[(int)character];

        public VehicleRecord FindVehicle(int id) => Vehicles.Find(v => v.Id == id);

        public void Normalize()
        {
            if (Characters == null) Characters = new List<CharacterRecord>();
            while (Characters.Count < 3) Characters.Add(new CharacterRecord());
            if (Characters.Count > 3) Characters.RemoveRange(3, Characters.Count - 3);
            foreach (CharacterRecord character in Characters)
            {
                if (character.Weapons == null) character.Weapons = new List<WeaponRecord>();
                if (character.Money < 0) character.Money = 0;
            }
            if (Vehicles == null) Vehicles = new List<VehicleRecord>();
            foreach (VehicleRecord vehicle in Vehicles)
                if (vehicle.Id >= NextVehicleId) NextVehicleId = vehicle.Id + 1;
        }
    }

    internal sealed class CharacterRecord
    {
        public bool Unlocked { get; set; }
        public int Money { get; set; }
        public List<WeaponRecord> Weapons { get; set; } = new List<WeaponRecord>();
    }

    internal sealed class WeaponRecord
    {
        public int Hash { get; set; }
        public int Ammo { get; set; }
    }

    internal sealed class VehicleRecord
    {
        public int Id { get; set; }
        public int Model { get; set; }
        public VehicleStatus Status { get; set; }
        public bool DeliveryPending { get; set; }
        public VehicleLook Look { get; set; }
    }

    internal sealed class StateStore
    {
        private readonly string _directory;
        private readonly string _path;

        public StateStore(string directory)
        {
            _directory = directory;
            _path = Path.Combine(directory, "state.json");
            State = new EnvironmentState();
            State.Normalize();
        }

        public EnvironmentState State { get; private set; }

        public void Load()
        {
            if (!File.Exists(_path))
            {
                Log.Info("state.json not found, starting with an empty state.");
                return;
            }
            try
            {
                EnvironmentState state = new JavaScriptSerializer().Deserialize<EnvironmentState>(File.ReadAllText(_path, Encoding.UTF8));
                if (state == null) return;
                state.Normalize();
                State = state;
                Log.Info($"state.json loaded (sterile={State.Sterile}, start={State.StartCharacter}, vehicles={State.Vehicles.Count}).");
            }
            catch (Exception ex)
            {
                Log.Error("state.json is invalid, starting with an empty state.", ex);
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(_directory);
                string temp = _path + ".tmp";
                File.WriteAllText(temp, new JavaScriptSerializer().Serialize(State), Encoding.UTF8);
                File.Copy(temp, _path, true);
                File.Delete(temp);
            }
            catch (Exception ex)
            {
                Log.Error("Could not write state.json.", ex);
            }
        }

        public void Reset()
        {
            int pending = State.PendingStartCharacter;
            if (File.Exists(_path))
            {
                try
                {
                    string backup = Path.Combine(_directory, $"state.{DateTime.Now:yyyyMMdd-HHmmss}.bak.json");
                    File.Copy(_path, backup, true);
                    Log.Info("Previous state backed up to " + Path.GetFileName(backup) + ".");
                }
                catch (Exception ex)
                {
                    Log.Error("Could not back up state.json.", ex);
                }
            }
            State = new EnvironmentState { PendingStartCharacter = pending };
            State.Normalize();
        }
    }
}
