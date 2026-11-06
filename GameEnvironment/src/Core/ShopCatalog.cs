using System;
using System.Collections.Generic;
using System.Linq;

namespace GameEnvironment.Core
{
    internal sealed class ShopPart
    {
        public ShopPart(string component, string name, int price)
        {
            Component = component;
            Name = name;
            Price = price;
            Hash = unchecked((int)GTA.StringHash.AtStringHash(component, 0u));
        }

        public string Component { get; }
        public string Name { get; }
        public int Price { get; }
        public int Hash { get; }
    }

    internal sealed class ShopEntry
    {
        public ShopEntry(string id, string name, string category, int price, ShopPart[] parts)
        {
            Id = id;
            Name = name;
            Category = category;
            Price = price;
            Parts = parts ?? Array.Empty<ShopPart>();
            Hash = unchecked((int)GTA.StringHash.AtStringHash(id, 0u));
        }

        public string Id { get; }
        public string Name { get; }
        public string Category { get; }
        public int Price { get; }
        public ShopPart[] Parts { get; }
        public int Hash { get; }
        public bool IsGadget => Id.StartsWith("GADGET_", StringComparison.Ordinal);
    }

    internal static partial class ShopCatalog
    {
        private static Dictionary<int, ShopEntry> _byHash;

        public static IEnumerable<string> Categories => Weapons.Select(w => w.Category).Distinct();

        public static ShopEntry ByHash(int hash)
        {
            if (_byHash == null) _byHash = Weapons.GroupBy(w => w.Hash).ToDictionary(g => g.Key, g => g.First());
            return _byHash.TryGetValue(hash, out ShopEntry entry) ? entry : null;
        }

        public static ShopEntry ById(string id) => Weapons.FirstOrDefault(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
