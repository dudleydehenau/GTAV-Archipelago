using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;

namespace GameEnvironment.Core
{
    public sealed class GunShop : Script
    {
        private const int InteriorInterval = 500;
        private const string GameShopScript = "gunclub_shop";
        private const string Armor = "Body Armor";
        private const int ArmorPrice = 2500;
        private static readonly int[] ShopInteriors =
        {
            unchecked((int)StringHash.AtStringHash("v_gun", 0u)),
            unchecked((int)StringHash.AtStringHash("v_gun2", 0u)),
        };
        private static readonly Control[] Blocked =
        {
            Control.Attack, Control.Attack2, Control.Aim, Control.MeleeAttackLight, Control.MeleeAttackHeavy, Control.MeleeAttackAlternate,
            Control.MeleeBlock, Control.MoveLeftRight, Control.MoveUpDown, Control.LookLeftRight, Control.LookUpDown, Control.Jump,
            Control.Sprint, Control.Enter, Control.SelectWeapon, Control.WeaponWheelNext, Control.WeaponWheelPrev, Control.NextCamera,
            Control.Phone, Control.Duck, Control.Cover, Control.Reload, Control.FrontendPause, Control.FrontendPauseAlternate,
            Control.CharacterWheel, Control.SelectCharacterMichael, Control.SelectCharacterFranklin, Control.SelectCharacterTrevor,
        };

        private readonly ObjectPool _pool = new ObjectPool();
        private readonly List<Snapshot> _snapshot = new List<Snapshot>();
        private NativeMenu _main;
        private NativeMenu _parts;
        private NativeListItem<string> _category;
        private string[] _categories;
        private ShopEntry _current;
        private ShopEntry _previewed;
        private int _lastMain = -1;
        private int _lastPart = -1;
        private int _nextInterior;
        private int _display;
        private ShopEntry _pendingEntry;
        private List<int> _pendingParts = new List<int>();
        private List<int> _pendingModels = new List<int>();
        private int _pendingUntil;
        private int _leftShopAt = -100000;
        private Vector3 _displayPosition;
        private float _displayHeading;
        private float _displayAngle;
        private bool _inside;
        private int _cam;
        private int _heldBefore;
        private float _headingBefore;
        private bool _wasMainVisible;
        private bool _wasPartsVisible;

        public GunShop()
        {
            Tick += OnTick;
            Aborted += (s, e) => Close();
        }

        internal static bool IsOpen { get; private set; }

        private static EnvironmentScript Core => EnvironmentScript.Instance;

        private void OnTick(object sender, EventArgs e)
        {
            EnvironmentScript core = Core;
            if (core == null || !core.Config.CustomGunShop || core.Mode != EnvironmentMode.Sterile)
            {
                if (IsOpen) Close();
                return;
            }
            try
            {
                StopGameShop();
                if (IsOpen)
                {
                    Browse();
                    return;
                }
                Ped ped = Game.Player.Character;
                if (ped == null || !ped.Exists() || ped.IsDead) return;
                if (Game.GameTime >= _nextInterior)
                {
                    _nextInterior = Game.GameTime + InteriorInterval;
                    bool wasInside = _inside;
                    _inside = !ped.IsInVehicle() && InShop(ped);
                    if (wasInside && !_inside) _leftShopAt = Game.GameTime;
                    if (!_inside && Game.GameTime - _leftShopAt < 10000) ReleasePlayer(ped);
                }
                if (!_inside) return;
                if (Game.Player.Wanted.WantedLevel > 0) return;
                GTA.UI.Screen.ShowHelpTextThisFrame("Press ~INPUT_CONTEXT~ to browse the weapons.");
                if (Game.IsControlJustPressed(Control.Context)) Open(ped);
            }
            catch (Exception ex)
            {
                Log.Error("Gun shop error.", ex);
                Close();
            }
        }

        private static void ReleasePlayer(Ped ped)
        {
            Function.Call(Hash.SET_PED_CAN_SWITCH_WEAPON, ped, true);
            Function.Call(Hash.HIDE_PED_WEAPON_FOR_SCRIPTED_CUTSCENE, ped, false);
            Function.Call(Hash.SET_PED_CURRENT_WEAPON_VISIBLE, ped, true, true, true, false);
            Function.Call(Hash.SET_PED_STEALTH_MOVEMENT, ped, false, 0);
            Function.Call(Hash.SET_PED_CONFIG_FLAG, ped, 185, false);
            if (!Function.Call<bool>(Hash.IS_PLAYER_CONTROL_ON, Game.Player)) Function.Call(Hash.SET_PLAYER_CONTROL, Game.Player, true, 0);
        }

        private static readonly int GameShopHash = unchecked((int)StringHash.AtStringHash(GameShopScript, 0u));

        private static void StopGameShop()
        {
            if (Function.Call<int>(Hash.GET_NUMBER_OF_THREADS_RUNNING_THE_SCRIPT_WITH_THIS_HASH, GameShopHash) > 0)
                Function.Call(Hash.TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME, GameShopScript);
        }

        private static bool InShop(Ped ped)
        {
            int interior = Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY, ped);
            if (interior == 0) return false;
            var position = new OutputArgument();
            var name = new OutputArgument();
            Function.Call(Hash.GET_INTERIOR_LOCATION_AND_NAMEHASH, interior, position, name);
            return ShopInteriors.Contains(name.GetResult<int>());
        }

        private void Open(Ped ped)
        {
            if (_main == null) Build();
            Core.Economy.PauseAmmoTracking();
            TakeSnapshot(ped);
            _heldBefore = Function.Call<int>(Hash.GET_SELECTED_PED_WEAPON, ped);
            _headingBefore = ped.Heading;
            ped.Heading = _headingBefore + 180f;
            ped.IsPositionFrozen = true;
            Vector3 forward = ped.ForwardVector;
            Vector3 right = ped.RightVector;
            Function.Call(Hash.SET_CURRENT_PED_WEAPON, ped, unchecked((uint)WeaponHash.Unarmed), true);
            _displayPosition = ped.Position + forward * 0.6f + right * 0.2f + new Vector3(0f, 0f, 0.3f);
            _displayHeading = ped.Heading + 90f;
            _displayAngle = 0f;
            Vector3 camera = ped.Position + forward * 1.9f + right * 0.55f + new Vector3(0f, 0f, 0.45f);
            Vector3 target = ped.Position + forward * 0.4f + right * 0.15f + new Vector3(0f, 0f, 0.25f);
            Function.Call(Hash.SET_PED_CURRENT_WEAPON_VISIBLE, ped, true, true, true, false);
            _cam = Function.Call<int>(Hash.CREATE_CAM_WITH_PARAMS, "DEFAULT_SCRIPTED_CAMERA", camera.X, camera.Y, camera.Z, 0f, 0f, 0f, 45f, false, 2);
            Function.Call(Hash.POINT_CAM_AT_COORD, _cam, target.X, target.Y, target.Z);
            Function.Call(Hash.SET_CAM_ACTIVE, _cam, true);
            Function.Call(Hash.RENDER_SCRIPT_CAMS, true, true, 600, true, false, 0);
            IsOpen = true;
            _previewed = null;
            _lastMain = -1;
            _lastPart = -1;
            FillMain();
            _main.Visible = true;
            _wasMainVisible = true;
            _wasPartsVisible = false;
            Log.Info("Gun shop opened.");
        }

        private void Build()
        {
            _categories = ShopCatalog.Categories.Concat(new[] { "Gear" }).Distinct().ToArray();
            _main = new NativeMenu("Ammu-Nation", "Weapons");
            _parts = new NativeMenu("Ammu-Nation", "Upgrades");
            _category = new NativeListItem<string>("Category", "Left and right to change the category.", _categories);
            _category.ItemChanged += (s, a) =>
            {
                FillMain();
                _lastMain = -1;
            };
            _pool.Add(_main);
            _pool.Add(_parts);
        }

        private void Browse()
        {
            Ped ped = Game.Player.Character;
            if (ped == null || !ped.Exists() || ped.IsDead)
            {
                Close();
                return;
            }
            Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
            Function.Call(Hash.SET_PED_CURRENT_WEAPON_VISIBLE, ped, true, true, true, false);
            CreatePending();
            if (_display != 0)
            {
                _displayAngle = (_displayAngle + 45f * Game.LastFrameTime) % 360f;
                Function.Call(Hash.SET_ENTITY_ROTATION, _display, 0f, 0f, _displayHeading + _displayAngle, 2, true);
            }
            foreach (Control control in Blocked) Game.DisableControlThisFrame(control);
            _pool.Process();
            if (_main.Visible && _main.SelectedIndex != _lastMain)
            {
                _lastMain = _main.SelectedIndex;
                PreviewWeapon(SelectedEntry());
            }
            if (_parts.Visible && _parts.SelectedIndex != _lastPart)
            {
                _lastPart = _parts.SelectedIndex;
                PreviewPart();
            }
            bool mainVisible = _main.Visible;
            bool partsVisible = _parts.Visible;
            if (!mainVisible && !partsVisible)
            {
                if (_wasPartsVisible)
                {
                    FillMain();
                    _main.Visible = true;
                    _lastMain = -1;
                }
                else if (_wasMainVisible) Close();
            }
            _wasMainVisible = _main.Visible;
            _wasPartsVisible = _parts.Visible;
        }

        private ShopEntry SelectedEntry() => _main.SelectedItem?.Tag as ShopEntry;

        private int Price(int basePrice) => Math.Max(1, (int)Math.Round(basePrice * Math.Max(10, EnvironmentApi.ShopPriceMultiplier) / 100.0));

        private bool Owned(ShopEntry entry) => _snapshot.Any(s => s.Hash == entry.Hash);

        private static bool Allowed(ShopEntry entry)
        {
            Func<string, bool> rule = EnvironmentApi.CanBuyWeapon;
            try
            {
                return rule == null || rule(entry.Id);
            }
            catch (Exception ex)
            {
                Log.Error("CanBuyWeapon failed.", ex);
                return true;
            }
        }

        private static bool UpgradesAllowed(ShopEntry entry)
        {
            Func<string, bool> rule = EnvironmentApi.CanBuyUpgrades;
            try
            {
                return rule == null || rule(entry.Id);
            }
            catch (Exception ex)
            {
                Log.Error("CanBuyUpgrades failed.", ex);
                return true;
            }
        }

        private static string Note(ShopEntry entry)
        {
            try
            {
                return EnvironmentApi.WeaponNote?.Invoke(entry.Id);
            }
            catch
            {
                return null;
            }
        }

        private static string Note(ShopEntry entry, ShopPart part)
        {
            try
            {
                return EnvironmentApi.UpgradeNote?.Invoke(entry.Id, part.Component);
            }
            catch
            {
                return null;
            }
        }

        private void FillMain()
        {
            int keep = Math.Max(0, _main.SelectedIndex);
            _main.Clear();
            _main.Add(_category);
            string category = _category.SelectedItem;
            _main.Name = category;
            int money = Game.Player.Money;
            if (category == "Gear")
            {
                var armor = new NativeItem(Armor, "Full body armor.", GameText.Money(Price(ArmorPrice)));
                armor.Activated += (s, a) => BuyArmor();
                _main.Add(armor);
            }
            foreach (ShopEntry entry in ShopCatalog.Weapons.Where(w => w.Category == category))
            {
                bool owned = Owned(entry);
                bool allowed = Allowed(entry);
                string note = Note(entry);
                string status = owned ? (entry.Parts.Length > 0 ? "Upgrades >" : "Ammo " + GameText.Money(AmmoPrice(entry)))
                    : !allowed ? "~r~Locked"
                    : GameText.Money(Price(entry.Price));
                string help = owned ? "You own it." + (entry.Parts.Length > 0 ? " Enter: upgrades and ammo." : " Enter: buy ammo.")
                    : !allowed ? "Locked: you need \"Weapon: " + entry.Name + "\" from the multiworld."
                    : "Enter: buy it." + (Price(entry.Price) > money ? " ~r~Not enough money." : string.Empty);
                if (!string.IsNullOrEmpty(note)) help += " " + note;
                var item = new NativeItem((owned ? string.Empty : !allowed ? "~c~" : string.Empty) + entry.Name, help, status) { Tag = entry };
                item.Activated += (s, a) => Choose(entry);
                _main.Add(item);
            }
            _main.SelectedIndex = Math.Min(keep, _main.Items.Count - 1);
        }

        private void FillParts(ShopEntry entry)
        {
            int keep = Math.Max(0, _parts.SelectedIndex);
            _parts.Clear();
            _parts.Name = entry.Name;
            var ammo = new NativeItem("Ammo", "Fills the ammo of this weapon.", GameText.Money(AmmoPrice(entry)));
            ammo.Activated += (s, a) => BuyAmmo(entry);
            _parts.Add(ammo);
            Snapshot snap = _snapshot.FirstOrDefault(x => x.Hash == entry.Hash);
            bool allowed = UpgradesAllowed(entry);
            foreach (ShopPart part in entry.Parts)
            {
                bool owned = snap != null && snap.Owned.Contains(part.Hash);
                bool equipped = snap != null && snap.Equipped.Contains(part.Hash);
                string status = owned ? (equipped ? "~g~Equipped" : "Owned") : !allowed ? "~r~Locked" : GameText.Money(Price(part.Price));
                string help = owned ? "Enter: " + (equipped ? "take it off." : "put it on.")
                    : !allowed ? "Locked: you need \"Upgrades: " + entry.Name + "\" from the multiworld."
                    : "Enter: buy it.";
                string note = Note(entry, part);
                if (!string.IsNullOrEmpty(note)) help += " " + note;
                var item = new NativeItem(part.Name, help, status) { Tag = part };
                item.Activated += (s, a) => ChoosePart(entry, part);
                _parts.Add(item);
            }
            _parts.SelectedIndex = Math.Min(keep, _parts.Items.Count - 1);
        }

        private void Choose(ShopEntry entry)
        {
            if (Owned(entry))
            {
                if (entry.Parts.Length == 0)
                {
                    BuyAmmo(entry);
                    return;
                }
                _current = entry;
                FillParts(entry);
                _main.Visible = false;
                _parts.SelectedIndex = 0;
                _lastPart = -1;
                _parts.Visible = true;
                _wasMainVisible = false;
                _wasPartsVisible = true;
                return;
            }
            if (!Allowed(entry))
            {
                Tell($"~r~Locked:~s~ you need \"Weapon: {entry.Name}\" from the multiworld.");
                return;
            }
            if (!Core.Economy.TrySpend(Price(entry.Price), out string error))
            {
                Tell("~r~" + error);
                return;
            }
            Ped ped = Game.Player.Character;
            uint hash = unchecked((uint)entry.Hash);
            if (!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, hash, false))
                Function.Call(Hash.GIVE_WEAPON_TO_PED, ped, hash, 1, false, false);
            int ammo = MaxAmmo(ped, entry);
            Function.Call(Hash.SET_PED_AMMO, ped, hash, ammo, false);
            foreach (PlayableCharacter character in Characters.All)
                Core.Economy.GiveWeaponTo(character, (WeaponHash)hash, ammo, false);
            _snapshot.Add(new Snapshot { Hash = entry.Hash, Ammo = ammo });
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "WEAPON_PURCHASE", "HUD_AMMO_SHOP_SOUNDSET", true);
            Tell($"~g~Bought:~s~ {entry.Name}");
            Log.Info($"Bought {entry.Id}.");
            EnvironmentApi.RaiseWeaponBought(entry.Id);
            _previewed = entry;
            FillMain();
        }

        private void ChoosePart(ShopEntry entry, ShopPart part)
        {
            Snapshot snap = _snapshot.FirstOrDefault(x => x.Hash == entry.Hash);
            if (snap == null) return;
            Ped ped = Game.Player.Character;
            uint weapon = unchecked((uint)entry.Hash);
            uint component = unchecked((uint)part.Hash);
            if (snap.Owned.Contains(part.Hash))
            {
                ApplySnapshot(ped, snap);
                if (snap.Equipped.Contains(part.Hash)) Function.Call(Hash.REMOVE_WEAPON_COMPONENT_FROM_PED, ped, weapon, component);
                else Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, ped, weapon, component);
                ReadEquipped(ped, entry, snap);
                Core.Economy.SetEquipped(entry.Hash, snap.Equipped);
                FillParts(entry);
                return;
            }
            if (!UpgradesAllowed(entry))
            {
                Tell($"~r~Locked:~s~ you need \"Upgrades: {entry.Name}\" from the multiworld.");
                return;
            }
            if (!Core.Economy.TrySpend(Price(part.Price), out string error))
            {
                Tell("~r~" + error);
                return;
            }
            ApplySnapshot(ped, snap);
            Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, ped, weapon, component);
            snap.Owned.Add(part.Hash);
            ReadEquipped(ped, entry, snap);
            if (!snap.Equipped.Contains(part.Hash)) snap.Equipped.Add(part.Hash);
            Core.Economy.AddPart(entry.Hash, part.Hash);
            Core.Economy.SetEquipped(entry.Hash, snap.Equipped);
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "WEAPON_PURCHASE", "HUD_AMMO_SHOP_SOUNDSET", true);
            Tell($"~g~Bought:~s~ {entry.Name}, {part.Name}");
            Log.Info($"Bought {entry.Id} {part.Component}.");
            EnvironmentApi.RaiseUpgradeBought(entry.Id, part.Component);
            FillParts(entry);
        }

        private int AmmoPrice(ShopEntry entry) => Math.Max(50, Price(entry.Price) / 10);

        private void BuyAmmo(ShopEntry entry)
        {
            Snapshot snap = _snapshot.FirstOrDefault(x => x.Hash == entry.Hash);
            if (snap == null || entry.IsGadget) return;
            Ped ped = Game.Player.Character;
            int max = MaxAmmo(ped, entry);
            if (snap.Ammo >= max)
            {
                Tell("Your ammo is already full.");
                return;
            }
            if (!Core.Economy.TrySpend(AmmoPrice(entry), out string error))
            {
                Tell("~r~" + error);
                return;
            }
            snap.Ammo = max;
            Function.Call(Hash.SET_PED_AMMO, ped, unchecked((uint)entry.Hash), max, false);
            Core.Economy.SetAmmo(entry.Hash, max);
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "WEAPON_AMMO_PURCHASE", "HUD_AMMO_SHOP_SOUNDSET", true);
            Tell($"~g~Ammo filled:~s~ {entry.Name}");
        }

        private void BuyArmor()
        {
            Ped ped = Game.Player.Character;
            if (ped.Armor >= 100)
            {
                Tell("Your armor is already full.");
                return;
            }
            if (!Core.Economy.TrySpend(Price(ArmorPrice), out string error))
            {
                Tell("~r~" + error);
                return;
            }
            ped.Armor = 100;
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "WEAPON_PURCHASE", "HUD_AMMO_SHOP_SOUNDSET", true);
        }

        private static int MaxAmmo(Ped ped, ShopEntry entry)
        {
            var max = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_MAX_AMMO, ped, unchecked((uint)entry.Hash), max))
            {
                int value = max.GetResult<int>();
                if (value > 0) return value;
            }
            return entry.IsGadget ? 1 : 250;
        }

        private void PreviewWeapon(ShopEntry entry)
        {
            _previewed = entry;
            Snapshot snap = entry == null ? null : _snapshot.FirstOrDefault(x => x.Hash == entry.Hash);
            ShowDisplay(entry, snap?.Equipped ?? new List<int>());
        }

        private void PreviewPart()
        {
            ShopEntry entry = _current;
            Snapshot snap = entry == null ? null : _snapshot.FirstOrDefault(x => x.Hash == entry.Hash);
            var parts = new List<int>(snap?.Equipped ?? new List<int>());
            if (_parts.SelectedItem?.Tag is ShopPart part && !parts.Contains(part.Hash)) parts.Add(part.Hash);
            ShowDisplay(entry, parts);
        }

        private void ShowDisplay(ShopEntry entry, List<int> parts)
        {
            DeleteDisplay();
            if (entry == null || entry.IsGadget) return;
            uint hash = unchecked((uint)entry.Hash);
            Function.Call(Hash.REQUEST_WEAPON_ASSET, hash, 31, 0);
            var models = new List<int>();
            foreach (int part in parts)
            {
                int model = Function.Call<int>(Hash.GET_WEAPON_COMPONENT_TYPE_MODEL, unchecked((uint)part));
                if (model == 0) continue;
                Function.Call(Hash.REQUEST_MODEL, model);
                models.Add(model);
            }
            _pendingEntry = entry;
            _pendingParts = parts;
            _pendingModels = models;
            _pendingUntil = Game.GameTime + 3000;
        }

        private void CreatePending()
        {
            ShopEntry entry = _pendingEntry;
            if (entry == null) return;
            uint hash = unchecked((uint)entry.Hash);
            bool ready = Function.Call<bool>(Hash.HAS_WEAPON_ASSET_LOADED, hash)
                && _pendingModels.All(m => Function.Call<bool>(Hash.HAS_MODEL_LOADED, m));
            if (!ready && Game.GameTime < _pendingUntil) return;
            List<int> parts = _pendingParts;
            List<int> models = _pendingModels;
            _pendingEntry = null;
            Vector3 p = _displayPosition;
            _display = Function.Call<int>(Hash.CREATE_WEAPON_OBJECT, hash, 1, p.X, p.Y, p.Z, true, 1f, 0, 0, 1);
            if (_display == 0)
            {
                Log.Warn($"Could not show {entry.Id}.");
                return;
            }
            foreach (int part in parts)
                Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_WEAPON_OBJECT, _display, unchecked((uint)part));
            Function.Call(Hash.FREEZE_ENTITY_POSITION, _display, true);
            Function.Call(Hash.SET_ENTITY_COLLISION, _display, false, false);
            Function.Call(Hash.SET_ENTITY_ROTATION, _display, 0f, 0f, _displayHeading + _displayAngle, 2, true);
            foreach (int model in models) Function.Call(Hash.SET_MODEL_AS_NO_LONGER_NEEDED, model);
        }

        private void DeleteDisplay()
        {
            _pendingEntry = null;
            if (_display == 0) return;
            Entity entity = Entity.FromHandle(_display);
            if (entity != null && entity.Exists()) entity.Delete();
            _display = 0;
        }

        private static void ApplySnapshot(Ped ped, Snapshot snap)
        {
            uint weapon = unchecked((uint)snap.Hash);
            if (!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, weapon, false))
                Function.Call(Hash.GIVE_WEAPON_TO_PED, ped, weapon, Math.Max(1, snap.Ammo), false, false);
            ShopEntry entry = ShopCatalog.ByHash(snap.Hash);
            if (entry != null)
            {
                foreach (ShopPart part in entry.Parts)
                {
                    uint component = unchecked((uint)part.Hash);
                    bool want = snap.Equipped.Contains(part.Hash);
                    bool has = Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON_COMPONENT, ped, weapon, component);
                    if (has && !want) Function.Call(Hash.REMOVE_WEAPON_COMPONENT_FROM_PED, ped, weapon, component);
                }
                foreach (int part in snap.Equipped)
                {
                    uint component = unchecked((uint)part);
                    if (!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON_COMPONENT, ped, weapon, component))
                        Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, ped, weapon, component);
                }
            }
            Function.Call(Hash.SET_PED_AMMO, ped, weapon, snap.Ammo, false);
        }

        private static void ReadEquipped(Ped ped, ShopEntry entry, Snapshot snap)
        {
            uint weapon = unchecked((uint)entry.Hash);
            snap.Equipped = entry.Parts.Where(p => snap.Owned.Contains(p.Hash)
                && Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON_COMPONENT, ped, weapon, unchecked((uint)p.Hash))).Select(p => p.Hash).ToList();
        }

        private void TakeSnapshot(Ped ped)
        {
            _snapshot.Clear();
            PlayableCharacter? character = Characters.Current;
            foreach (ShopEntry entry in ShopCatalog.Weapons)
            {
                uint hash = unchecked((uint)entry.Hash);
                if (!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, hash, false)) continue;
                var snap = new Snapshot { Hash = entry.Hash, Ammo = Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON, ped, hash) };
                WeaponRecord record = character == null ? null : Core.Economy.Record(character.Value, entry.Hash);
                if (record != null) snap.Owned.AddRange(record.Owned);
                foreach (ShopPart part in entry.Parts)
                {
                    if (!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON_COMPONENT, ped, hash, unchecked((uint)part.Hash))) continue;
                    snap.Equipped.Add(part.Hash);
                    if (!snap.Owned.Contains(part.Hash)) snap.Owned.Add(part.Hash);
                }
                _snapshot.Add(snap);
            }
        }

        private void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            try
            {
                if (_main != null) _main.Visible = false;
                if (_parts != null) _parts.Visible = false;
                Ped ped = Game.Player.Character;
                if (ped != null && ped.Exists())
                {
                    foreach (ShopEntry entry in ShopCatalog.Weapons)
                    {
                        uint hash = unchecked((uint)entry.Hash);
                        Snapshot snap = _snapshot.FirstOrDefault(x => x.Hash == entry.Hash);
                        if (snap != null) ApplySnapshot(ped, snap);
                        else if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, hash, false)) Function.Call(Hash.REMOVE_WEAPON_FROM_PED, ped, hash);
                    }
                    if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, unchecked((uint)_heldBefore), false))
                        Function.Call(Hash.SET_CURRENT_PED_WEAPON, ped, unchecked((uint)_heldBefore), true);
                    else Function.Call(Hash.SET_CURRENT_PED_WEAPON, ped, unchecked((uint)WeaponHash.Unarmed), true);
                    ped.IsPositionFrozen = false;
                    ped.Heading = _headingBefore;
                    ReleasePlayer(ped);
                }
                Function.Call(Hash.RENDER_SCRIPT_CAMS, false, true, 600, true, false, 0);
                if (_cam != 0) Function.Call(Hash.DESTROY_CAM, _cam, false);
                _cam = 0;
                DeleteDisplay();
                _previewed = null;
                _current = null;
                Core?.Economy.ScheduleWeaponCheck(500);
                Log.Info("Gun shop closed.");
            }
            catch (Exception ex)
            {
                Log.Error("Could not close the gun shop cleanly.", ex);
            }
        }

        private static void Tell(string text) => GTA.UI.Notification.PostTicker(text, false);

        private sealed class Snapshot
        {
            public int Hash;
            public int Ammo;
            public List<int> Owned = new List<int>();
            public List<int> Equipped = new List<int>();
        }
    }
}
