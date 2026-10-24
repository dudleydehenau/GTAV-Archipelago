using GTA;

namespace GameEnvironment
{
    public enum PlayableCharacter
    {
        Michael = 0,
        Franklin = 1,
        Trevor = 2,
    }

    public enum EnvironmentMode
    {
        Starting,
        Waiting,
        Sterile,
        StorySave,
        Disabled,
    }

    public enum VehicleStatus
    {
        Stored = 0,
        Delivered = 1,
        Destroyed = 2,
    }

    public sealed class OwnedVehicle
    {
        internal OwnedVehicle(int id, int model, VehicleStatus status, bool deliveryPending, int repairCost, string name)
        {
            Id = id;
            Model = (VehicleHash)unchecked((uint)model);
            Status = status;
            DeliveryPending = deliveryPending;
            RepairCost = repairCost;
            Name = name;
        }

        public int Id { get; }
        public VehicleHash Model { get; }
        public string Name { get; }
        public VehicleStatus Status { get; }
        public bool DeliveryPending { get; }
        public int RepairCost { get; }
        public bool IsDestroyed => Status == VehicleStatus.Destroyed;

        public override string ToString() => $"#{Id} {Name} ({Status})";
    }
}
