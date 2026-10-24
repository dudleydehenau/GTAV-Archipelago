using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class VehicleLook
    {
        private const int ModCount = 50;
        private const int FirstToggleMod = 17;
        private const int LastToggleMod = 22;
        private const int FrontWheels = 23;
        private const int RearWheels = 24;
        private const int ExtraCount = 14;

        public int Primary { get; set; }
        public int Secondary { get; set; }
        public int Pearlescent { get; set; }
        public int Rim { get; set; }
        public int Dashboard { get; set; }
        public int Trim { get; set; }
        public bool HasCustomPrimary { get; set; }
        public int CustomPrimary { get; set; }
        public bool HasCustomSecondary { get; set; }
        public int CustomSecondary { get; set; }
        public int WheelType { get; set; }
        public int WindowTint { get; set; }
        public int Livery { get; set; } = -1;
        public int PlateStyle { get; set; }
        public string Plate { get; set; }
        public int Xenon { get; set; } = 255;
        public int Neons { get; set; }
        public int NeonColor { get; set; }
        public int SmokeColor { get; set; }
        public List<int> Mods { get; set; } = new List<int>();
        public int CustomTires { get; set; }
        public int Toggles { get; set; }
        public int Extras { get; set; }

        public static VehicleLook Capture(Vehicle vehicle)
        {
            var look = new VehicleLook
            {
                Primary = (int)vehicle.Mods.PrimaryColor,
                Secondary = (int)vehicle.Mods.SecondaryColor,
                Pearlescent = (int)vehicle.Mods.PearlescentColor,
                Rim = (int)vehicle.Mods.RimColor,
                Dashboard = (int)vehicle.Mods.DashboardColor,
                Trim = (int)vehicle.Mods.TrimColor,
                HasCustomPrimary = vehicle.Mods.IsPrimaryColorCustom,
                HasCustomSecondary = vehicle.Mods.IsSecondaryColorCustom,
                WheelType = Function.Call<int>(Hash.GET_VEHICLE_WHEEL_TYPE, vehicle),
                WindowTint = Function.Call<int>(Hash.GET_VEHICLE_WINDOW_TINT, vehicle),
                Livery = Function.Call<int>(Hash.GET_VEHICLE_LIVERY, vehicle),
                PlateStyle = Function.Call<int>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, vehicle),
                Plate = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, vehicle),
                Xenon = Function.Call<int>(Hash.GET_VEHICLE_XENON_LIGHT_COLOR_INDEX, vehicle),
                NeonColor = vehicle.Mods.NeonLightsColor.ToArgb(),
                SmokeColor = vehicle.Mods.TireSmokeColor.ToArgb(),
            };
            if (look.HasCustomPrimary) look.CustomPrimary = vehicle.Mods.CustomPrimaryColor.ToArgb();
            if (look.HasCustomSecondary) look.CustomSecondary = vehicle.Mods.CustomSecondaryColor.ToArgb();
            for (int i = 0; i < 4; i++)
                if (Function.Call<bool>(Hash.GET_VEHICLE_NEON_ENABLED, vehicle, i)) look.Neons |= 1 << i;
            for (int type = 0; type < ModCount; type++)
            {
                if (type >= FirstToggleMod && type <= LastToggleMod)
                {
                    look.Mods.Add(-1);
                    if (Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON, vehicle, type)) look.Toggles |= 1 << (type - FirstToggleMod);
                    continue;
                }
                look.Mods.Add(Function.Call<int>(Hash.GET_VEHICLE_MOD, vehicle, type));
            }
            if (Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION, vehicle, FrontWheels)) look.CustomTires |= 1;
            if (Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION, vehicle, RearWheels)) look.CustomTires |= 2;
            for (int extra = 1; extra <= ExtraCount; extra++)
                if (Function.Call<bool>(Hash.DOES_EXTRA_EXIST, vehicle, extra) && Function.Call<bool>(Hash.IS_VEHICLE_EXTRA_TURNED_ON, vehicle, extra))
                    look.Extras |= 1 << extra;
            return look;
        }

        public void Apply(Vehicle vehicle)
        {
            Function.Call(Hash.SET_VEHICLE_MOD_KIT, vehicle, 0);
            bool bike = vehicle.Model.IsBike;
            int wheelType = bike ? 6 : WheelType;
            bool wheelTypeValid = wheelType >= 0 && wheelType <= 12 && (bike || wheelType != 6);
            if (wheelTypeValid) Function.Call(Hash.SET_VEHICLE_WHEEL_TYPE, vehicle, wheelType);
            for (int type = 0; type < ModCount && type < Mods.Count; type++)
            {
                if (type >= FirstToggleMod && type <= LastToggleMod)
                {
                    Function.Call(Hash.TOGGLE_VEHICLE_MOD, vehicle, type, (Toggles & (1 << (type - FirstToggleMod))) != 0);
                    continue;
                }
                int index = Mods[type];
                bool wheels = type == FrontWheels || type == RearWheels;
                if (index >= Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, vehicle, type) || (wheels && !wheelTypeValid)) index = -1;
                if (index < 0)
                {
                    if (Function.Call<int>(Hash.GET_VEHICLE_MOD, vehicle, type) >= 0) Function.Call(Hash.REMOVE_VEHICLE_MOD, vehicle, type);
                    continue;
                }
                bool customTires = (type == FrontWheels && (CustomTires & 1) != 0) || (type == RearWheels && (CustomTires & 2) != 0);
                Function.Call(Hash.SET_VEHICLE_MOD, vehicle, type, index, customTires);
            }
            vehicle.Mods.PrimaryColor = (VehicleColor)Primary;
            vehicle.Mods.SecondaryColor = (VehicleColor)Secondary;
            vehicle.Mods.PearlescentColor = (VehicleColor)Pearlescent;
            vehicle.Mods.RimColor = (VehicleColor)Rim;
            vehicle.Mods.DashboardColor = (VehicleColor)Dashboard;
            vehicle.Mods.TrimColor = (VehicleColor)Trim;
            if (HasCustomPrimary) vehicle.Mods.CustomPrimaryColor = Color.FromArgb(CustomPrimary);
            else vehicle.Mods.ClearCustomPrimaryColor();
            if (HasCustomSecondary) vehicle.Mods.CustomSecondaryColor = Color.FromArgb(CustomSecondary);
            else vehicle.Mods.ClearCustomSecondaryColor();
            Function.Call(Hash.SET_VEHICLE_WINDOW_TINT, vehicle, WindowTint);
            if (Livery >= 0) Function.Call(Hash.SET_VEHICLE_LIVERY, vehicle, Livery);
            Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, vehicle, PlateStyle);
            if (!string.IsNullOrEmpty(Plate)) Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, vehicle, Plate);
            Function.Call(Hash.SET_VEHICLE_XENON_LIGHT_COLOR_INDEX, vehicle, Xenon);
            vehicle.Mods.NeonLightsColor = Color.FromArgb(NeonColor);
            for (int i = 0; i < 4; i++)
                Function.Call(Hash.SET_VEHICLE_NEON_ENABLED, vehicle, i, (Neons & (1 << i)) != 0);
            vehicle.Mods.TireSmokeColor = Color.FromArgb(SmokeColor);
            for (int extra = 1; extra <= ExtraCount; extra++)
                if (Function.Call<bool>(Hash.DOES_EXTRA_EXIST, vehicle, extra))
                    Function.Call(Hash.SET_VEHICLE_EXTRA, vehicle, extra, (Extras & (1 << extra)) == 0);
        }

        public string Signature() => new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(this);
    }
}
