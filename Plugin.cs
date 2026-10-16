using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

[assembly: InternalsVisibleTo("TentandPlank.Tests")]
[assembly: InternalsVisibleTo("TentandPlank.QA")]

namespace TentandPlank
{
    // Tent and Plank: EconomyOverhaul's canvas pack and tarps, wood pole, wood planks, water damage and fragile cargo as their own
    // mod (TENTANDPLANK_DRAFT.md). EconomyOverhaul has the same features and items (slots 640-644): with it installed, BepInEx
    // skips this mod (the user, 2026-10-15: players use one or the other).
    [BepInPlugin(Guid, "Tent and Plank", Version)]
    [BepInIncompatibility("DogEggz.EconomyOverhaul")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "DogEggz.TentandPlank";
        public const string Version = "1.0.0";
        internal static ManualLogSource Log;
        internal static Plugin Instance;
        private Harmony harmony;
        private void Awake()
        {
            Instance = this; Log = Logger;
            try
            {
                Settings.Bind(Config);
                CargoStainMaterial.Load();
                harmony = new Harmony(Guid);
                harmony.PatchAll(typeof(Plugin).Assembly);
                Logger.LogInfo("Tent and Plank " + Version + " loaded.");
            }
            catch (Exception e)
            {
                harmony?.UnpatchSelf();
                Logger.LogError("Tent and Plank could not initialize. No partial patch set retained. " + e);
                enabled = false;
            }
        }
        private void Update() => WoodPlanks.Tick();
        private System.Collections.IEnumerator Start()
        {
            yield return null; yield return null;
            foreach (var water in UnityEngine.Object.FindObjectsOfType<BoatDamageWater>()) BoatWater.Register(water);
        }
        private void OnDestroy() => harmony?.UnpatchSelf();
    }

    internal static class Fields
    {
        private static readonly Dictionary<string, FieldInfo> cache = new Dictionary<string, FieldInfo>();
        public static FieldInfo Find(Type type, string name)
        {
            string key = type.FullName + "." + name;
            if (!cache.TryGetValue(key, out FieldInfo f)) cache[key] = f = AccessTools.Field(type, name) ?? throw new MissingFieldException(key);
            return f;
        }
        public static T Get<T>(object obj, string name) => (T)Find(obj.GetType(), name).GetValue(obj);
        public static void Set(object obj, string name, object value) => Find(obj.GetType(), name).SetValue(obj, value);
        public static object Call(object obj, string name, params object[] args) => AccessTools.Method(obj.GetType(), name).Invoke(obj, args);
    }
    internal static class Embedded
    {
        // The rows of a CSV file in Data/ (embedded in the DLL), split at commas; with a header, its first line is skipped.
        internal static IEnumerable<string[]> Csv(string name, bool header = true)
        {
            using (var reader = new StreamReader(typeof(Embedded).Assembly.GetManifestResourceStream("TentandPlank.Data." + name)))
            {
                if (header) reader.ReadLine();
                for (string line; (line = reader.ReadLine()) != null;) yield return line.Split(',');
            }
        }
    }

    // ---- Settings ----
    internal static class Settings
    {
        internal static ConfigEntry<bool> WaterDamage, WaterDamageHint, CoverHint, TarpWear, CargoDamageHint, FragileCargo;
        internal static ConfigEntry<KeyboardShortcut> BraceKey;
        // Water damage and fragile cargo are off by default in this mod (the user, 2026-10-15).
        internal static bool WaterEnabled => WaterDamage?.Value ?? false;
        internal static bool FragileEnabled => FragileCargo?.Value ?? false;
        internal static bool WaterHintEnabled => WaterDamageHint?.Value ?? true;
        internal static bool CoverHintEnabled => CoverHint?.Value ?? false;
        internal static bool CargoDamageHintEnabled => CargoDamageHint?.Value ?? true;
        internal static bool TarpWearing => TarpWear?.Value ?? true;
        private static bool tarpWearWas = true;   // the option's value before its last change
        internal static void Bind(ConfigFile config)
        {
            WaterDamage = config.Bind("Cargo", "Enable water damage", false,
                "Enable cargo water damage, drying, wet appearance and condition-based value reductions. When off, saved condition pauses and cargo has full condition value. Re-enabling resumes the saved condition.");
            WaterDamageHint = config.Bind("Cargo", "Show water damage percentage", true,
                "Show remaining water condition (100% to 0%) below vulnerable cargo hints. Hidden when water damage is disabled. This setting only changes the hint, not damage or cargo value.");
            CoverHint = config.Bind("Cargo", "Show cover percentage", false,
                "Show how much of vulnerable cargo's top is under cover (a deck, a roof or a canvas tarp), 0% to 100%, below its water damage line. Hidden when water damage is disabled. This setting only changes the hint.");
            FragileCargo = config.Bind("Cargo", "Enable fragile cargo", false,
                "Fragile cargo (gems, sculptures, marble) is damaged by hull impacts and loses value. When off, saved damage pauses and cargo has full value. Re-enabling resumes the saved damage.");
            CargoDamageHint = config.Bind("Cargo", "Show cargo damage percentage", true,
                "Show the damage of fragile cargo (gems, sculptures, marble), 0% intact to 100% ruined, below its other hint lines. Hidden when fragile cargo is disabled. This setting only changes the hint.");
            BraceKey = config.Bind("Wood pole", "Brace key", new KeyboardShortcut(KeyCode.H),
                "While holding a wood pole, this key braces it (held at your side, pointing forward: with the pick-up key held (the left mouse by default) it pushes boats, docks or the sea bed) or holds it loosely again.");
            TarpWear = config.Bind("Canvas", "Tarp wearing", true,
                "Tied canvas tarps lose 10 health a game day and tear at 0. When off, every tarp's health, colour and holes stay as they are. Turning it back on continues wearing from there.");
            WaterDamage.SettingChanged += (s, e) => { foreach (var c in CargoCondition.Active) c.RefreshSettings(); };
            WaterDamageHint.SettingChanged += (s, e) => { foreach (var c in CargoCondition.Active) c.RefreshHint(); };
            CoverHint.SettingChanged += (s, e) => { foreach (var c in CargoCondition.Active) c.RefreshHint(); };
            CargoDamageHint.SettingChanged += (s, e) => { foreach (var c in CargoCondition.Active) c.RefreshHint(); };
            FragileCargo.SettingChanged += (s, e) => { foreach (var c in CargoCondition.Active) { c.RefreshHint(); c.RefreshAppearance(); } };
            TarpWear.SettingChanged += (s, e) => { tarpWearWas = Tarps.Rebase(tarpWearWas); };
            tarpWearWas = TarpWear.Value;
        }
    }

    // ---- Notices ----
    // The game shows a notification as one text over a parchment of fixed size and never wraps it: each line is wrapped at
    // word gaps to 92% of the parchment's width, measured in the text's own font and units.
    internal static class Notice
    {
        internal const float Fill = .92f;
        private static float width;
        internal static void Show(string text) { if (NotificationUi.instance) NotificationUi.instance.ShowNotification(Wrap(text)); }
        internal static string Wrap(string text)
        {
            var ui = NotificationUi.instance; var mesh = ui ? Fields.Get<TextMesh>(ui, "text") : null;
            if (!mesh || !mesh.font || string.IsNullOrEmpty(text)) return text;
            if (width <= 0) width = BoxWidth(ui, mesh) * Fill;
            if (width <= 0) return text;
            return string.Join("\n", text.Split('\n').Select(line => WrapLine(mesh, line)).ToArray());
        }
        private static float Measure(TextMesh mesh, string line)
        {
            mesh.font.RequestCharactersInTexture(line, mesh.fontSize, mesh.fontStyle); float w = 0;
            foreach (char c in line) if (mesh.font.GetCharacterInfo(c, out var info, mesh.fontSize, mesh.fontStyle)) w += info.advance * .1f * mesh.characterSize;
            return w;
        }
        private static string WrapLine(TextMesh mesh, string line)
        {
            var lines = new List<string>(); string current = "";
            foreach (var word in line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string next = current.Length == 0 ? word : current + " " + word;
                if (current.Length > 0 && Measure(mesh, next) > width) { lines.Add(current); current = word; } else current = next;
            }
            if (current.Length > 0) lines.Add(current);
            return string.Join("\n", lines.ToArray());
        }
        private static float BoxWidth(NotificationUi ui, TextMesh mesh)
        {
            var box = Fields.Get<GameObject>(ui, "UI"); if (!box) return 0;
            float best = 0;
            foreach (var f in box.GetComponentsInChildren<MeshFilter>(true))
                if (f.gameObject != mesh.gameObject && f.sharedMesh && f.sharedMesh.bounds.size.x * Scale(f.transform, box.transform) > best)
                    best = f.sharedMesh.bounds.size.x * Scale(f.transform, box.transform);
            float unit = Scale(mesh.transform, box.transform);
            return unit > 0 ? best / unit : 0;
        }
        private static float Scale(Transform t, Transform root) { float s = 1; for (; t && t != root; t = t.parent) s *= Mathf.Abs(t.localScale.x); return s; }
    }

    // ---- Rules shared by the item and cargo code ----
    public static partial class Rules
    {
        public static double Clamp(double x, double lo, double hi) => Math.Max(lo, Math.Min(hi, x));
        public static int Round(double x) => checked((int)Math.Round(x, MidpointRounding.ToEven));
        // The mod's data is stale when the game saved after it without the mod: the hour stored with the data differs from the
        // save's own.
        public static bool StaleSave(double savedHour, double gameHour) => savedHour > 0 && Math.Abs(savedHour - gameHour) > .01;
    }
    // The goods' price curves, for the Luxury goods' drying ceiling (EconomyOverhaul's table, as of 0.10.0).
    public enum PriceProfile { Staple, ConsumerGoods, Industrial, Precious, Luxury }
    public static class PriceCurves
    {
        public static bool TryProfile(int good, out PriceProfile profile)
        {
            profile = PriceProfile.Luxury;
            if (good < 1 || good > 64 || good == 45 || good == 51) return false;
            switch (good)
            {
                case 1: case 2: case 3: case 4: case 9: case 10:
                case 16: case 17: case 18: case 25:
                case 42: case 54: profile = PriceProfile.Staple; break;
                case 6: case 7: case 8: case 11: case 12: case 13: case 14: case 15: case 46:
                case 19: case 26: case 27: case 29: case 36: case 43: case 48: case 49:
                case 52: case 58: case 59: case 63: profile = PriceProfile.ConsumerGoods; break;
                case 21: case 23: case 33: case 35: case 47: case 57: case 61:
                    profile = PriceProfile.Industrial; break;
                case 20: case 22: case 56: profile = PriceProfile.Precious; break;
            }
            return true;
        }
    }

    // Ships by their save index (a tied tarp remembers its ship by it), from the save manager's list: the game switches off ships
    // far from the player, and a scene search would skip them.
    internal static class Pledges
    {
        internal static IEnumerable<PurchasableBoat> All()
        {
            var objects = SaveLoadManager.instance ? SaveLoadManager.instance.GetCurrentObjects() : null;
            return objects == null ? Enumerable.Empty<PurchasableBoat>() : objects.Where(o => o && o.sceneIndex > 0).Select(o => o.GetComponent<PurchasableBoat>()).Where(b => b);
        }
        internal static PurchasableBoat Find(int index) => All().FirstOrDefault(b => Index(b) == index);
        internal static int Index(PurchasableBoat b) => b.GetComponent<SaveableObject>().sceneIndex;
    }

    // A ship's deck drop point (EconomyOverhaul's ForcedMove.DropPoint, copied as is): 1.5 m over a flat stretch of her walk
    // collider's top, tried outward from the middle along both axes, carried back to her real frame; with none found, 10 m over
    // her origin. Tarps fall to it (Tarps' floor search) when no deck is hit under a corner.
    internal static class ForcedMove
    {
        internal static Vector3 DropPoint(PurchasableBoat ship) => DeckDrop(ship) ?? ship.transform.position + Vector3.up * 10;
        private static Vector3? DeckDrop(PurchasableBoat ship)
        {
            var link = Link(ship);
            var walk = link ? link.walkCollider : null; var frame = link ? link.transform.parent : null;
            if (walk && frame)
                foreach (float d in new[] { 0f, 1.5f, 3f, 4.5f, 6f })
                    foreach (var at in d == 0 ? new[] { Vector2.zero } : new[] { new Vector2(d, 0), new Vector2(-d, 0), new Vector2(0, d), new Vector2(0, -d) })
                    {
                        var spot = Surface(walk, at.x, at.y); if (spot == null) continue;
                        var top = spot.Value; float y = walk.InverseTransformPoint(top.point).y;
                        bool flat = new[] { new Vector2(.4f, 0), new Vector2(-.4f, 0), new Vector2(0, .4f), new Vector2(0, -.4f) }
                            .All(o => { var s = Surface(walk, at.x + o.x, at.y + o.y); return s != null && s.Value.collider == top.collider && Mathf.Abs(walk.InverseTransformPoint(s.Value.point).y - y) <= .5f; });
                        if (flat) return frame.TransformPoint(walk.InverseTransformPoint(top.point)) + Vector3.up * 1.5f;
                    }
            return null;
        }
        // Her boarding trigger that names her walk collider, the player-sized one first (the game boards from either).
        private static BoatEmbarkCollider Link(PurchasableBoat ship) => ship.GetComponentsInChildren<BoatEmbarkCollider>(true)
            .Where(c => c.walkCollider).OrderBy(c => c.CompareTag("EmbarkColPlayer") ? 0 : 1).FirstOrDefault();
        // The top of the walk collider straight down through (x, z) of its own frame.
        private static RaycastHit? Surface(Transform walk, float x, float z)
        {
            var hits = Physics.RaycastAll(walk.TransformPoint(new Vector3(x, 15, z)), -walk.up, 40, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider.transform.IsChildOf(walk)).OrderBy(h => h.distance).ToArray();
            return hits.Length > 0 ? hits[0] : (RaycastHit?)null;
        }
    }

    // ---- Save data ----
    [Serializable] public sealed class SaveState
    {
        public int version = 1;
        public List<CargoState> cargo = new List<CargoState>();
        public List<int> removedCargo = new List<int>();
        // Optional, as EconomyOverhaul's: data without it (none written by this mod so far) reads as no tarps.
        [System.Runtime.Serialization.OptionalField] public List<TarpState> tarps = new List<TarpState>();
        public double savedHour;
    }
    internal static class StateCodec
    {
        // Not JsonUtility: Unity 2019's leaves out lists of a mod's own classes.
        private static DataContractJsonSerializer Serializer() => new DataContractJsonSerializer(typeof(SaveState),
            new DataContractJsonSerializerSettings { MaxItemsInObjectGraph = int.MaxValue });
        internal static string Write(SaveState state)
        {
            using (var stream = new MemoryStream()) { Serializer().WriteObject(stream, state); return Encoding.UTF8.GetString(stream.ToArray()); }
        }
        internal static SaveState Read(string json)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var result = (SaveState)Serializer().ReadObject(stream);
                if (result != null && result.tarps == null) result.tarps = new List<TarpState>();
                Validate(result); return result;
            }
        }
        private static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
        internal static void Validate(SaveState s)
        {
            bool valid = s != null && s.version == 1 && s.cargo != null && s.removedCargo != null && s.tarps != null && Finite(s.savedHour) && s.savedHour >= 0 &&
                s.cargo.All(c => c != null && c.id > 0 && c.good >= 1 && c.good <= 64 && c.good != 45 && Finite(c.health) && c.health >= 0 && c.health <= 100 &&
                    Finite(c.impact) && c.impact >= 0 && c.impact <= 100 && (c.stainHistory == null || c.stainHistory.Length == CargoStainHistory.Bytes)) &&
                s.cargo.Select(c => c.id).Distinct().Count() == s.cargo.Count && s.tarps.All(Tarps.ValidRecord);
            if (!valid) throw new InvalidDataException("Tent and Plank save records failed integrity checks");
        }
    }
    internal static class World
    {
        internal static SaveState State = new SaveState();
        // Ready: a game is running with the mod's data in place. SaveBlocked: the save's data could not be read; the save keeps it
        // as it was (the mod's key is left untouched) and the mod's cargo state is off for this session.
        internal static bool Ready, Loading, SaveBlocked;
        internal static readonly Dictionary<int, CargoState> Cargo = new Dictionary<int, CargoState>();
        internal static readonly HashSet<int> Removed = new HashSet<int>();
        internal static double CalendarHour => GameState.day * 24.0 + (Sun.sun ? Sun.sun.globalTime : 0);
        internal static void Reset()
        {
            Ready = false; Loading = false; SaveBlocked = false; State = new SaveState();
            Cargo.Clear(); Removed.Clear(); Tarps.Clear();
        }
        internal static void Save()
        {
            if (!Ready || SaveBlocked) return;
            foreach (var c in CargoCondition.Active) c.PrepareSave();
            CargoRecords.Prune();
            State.tarps = Tarps.Records();
            State.savedHour = CalendarHour;
            StateCodec.Validate(State);
            GameState.modData[Plugin.Guid] = StateCodec.Write(State);
        }
        internal static void Load()
        {
            State = new SaveState();
            if (GameState.modData.TryGetValue(Plugin.Guid, out string json))
            {
                try { State = StateCodec.Read(json); }
                catch (Exception e)
                {
                    SaveBlocked = true; Ready = false; Loading = false;
                    Plugin.Log.LogError("Tent and Plank data could not be read; it is kept in the save unchanged and the mod's cargo state is off until it is reloaded. " + e);
                    Notice.Show("Tent and Plank: save data unreadable; see BepInEx log.");
                    return;
                }
                // The game saved since without the mod: the data is from before, so start fresh.
                if (Rules.StaleSave(State.savedHour, CalendarHour)) { Plugin.Log.LogWarning("Tent and Plank data older than the save; starting fresh."); State = new SaveState(); }
            }
            Cargo.Clear(); Removed.Clear();
            foreach (var c in State.cargo) Cargo.Add(c.id, c);
            foreach (int id in State.removedCargo) Removed.Add(id);
            Ready = true; Loading = false;
            Tarps.Loaded();
            foreach (var c in new List<CargoCondition>(CargoCondition.Active)) c.Restore();
        }
    }
    [HarmonyPatch(typeof(StartMenu), "StartNewGame")] internal static class NewGamePatch
    {
        static void Prefix() => World.Reset();
        static void Postfix() { World.Ready = true; }
    }
    [HarmonyPatch(typeof(SaveLoadManager), "LoadGame")] internal static class BeginLoadPatch
    {
        static void Prefix() { World.Reset(); World.Loading = true; }
        static void Postfix() { World.Loading = false; }
    }
    [HarmonyPatch(typeof(SaveLoadManager), "SaveModData")] internal static class SavePatch
    {
        static void Prefix()
        {
            try { World.Save(); }
            catch (Exception e) { Plugin.Log.LogError("Tent and Plank data not saved (the save's previous copy is kept). " + e); }
        }
    }
    [HarmonyPatch(typeof(SaveLoadManager), "LoadModData")] internal static class LoadPatch { static void Postfix() => World.Load(); }

    internal static class CargoRecords
    {
        internal static CargoState Of(ShipItem item)
        {
            var save = item ? item.GetComponent<SaveablePrefab>() : null;
            return save && World.Cargo.TryGetValue(save.instanceId, out var record) ? record : null;
        }
        internal static void Destroyed(int id)
        {
            if (id <= 0 || !World.Cargo.ContainsKey(id)) return;
            if (World.Removed.Add(id)) World.State.removedCargo.Add(id);
            Prune();
        }
        // Only explicit permanent removals establish absence; cached or stored items and load teardown keep their records.
        internal static void Prune()
        {
            foreach (var c in World.State.cargo.Where(c => World.Removed.Contains(c.id)).ToArray()) { World.State.cargo.Remove(c); World.Cargo.Remove(c.id); }
        }
    }
    [HarmonyPatch(typeof(ShipItem), "DestroyItem")] internal static class RetireCargoRecordPatch
    {
        static void Prefix(ShipItem __instance, out int __state)
        {
            __state = 0;
            if (!World.Ready || World.Loading || GameState.currentlyLoading || World.SaveBlocked) return;
            var save = __instance.GetComponent<SaveablePrefab>();
            if (save && save.GetParentObject() != -2 && save.GetParentObject() != -3) __state = save.instanceId;
        }
        static void Postfix(int __state) { CargoRecords.Destroyed(__state); }
    }

    // ---- Cargo value at sale (the game's prices x the cargo's condition) ----
    // A shop's price (the game's base units; the shop converts it into local currency): an item the player sells x its
    // condition; a wood plank bundle on sale 1/10 of what the shop's market asks for one lumber (as EconomyOverhaul's, the
    // user 2026-10-15), from the market's own price report, rounded up.
    [HarmonyPatch(typeof(Shopkeeper), "GetPrice")] internal static class ShopSaleValuePatch
    {
        static void Postfix(Shopkeeper __instance, ShipItem item, ref int __result)
        {
            if (!item) return;
            if (item.sold) { __result = Mathf.RoundToInt(__result * CargoCondition.Factor(item)); return; }
            if (!WoodPlanks.IsBundle(item)) return;
            var economy = Fields.Get<IslandEconomy>(__instance, "economy"); var m = economy ? economy.GetComponent<IslandMarket>() : null;
            int lumber = LumberAsk(m); if (lumber > 0) __result = (int)Math.Ceiling(lumber / 10.0);
        }
        // The market's ask for one lumber: its own price report's buy price, or its current price when it has none.
        internal static int LumberAsk(IslandMarket m)
        {
            if (!m) return 0;
            var own = m.knownPrices != null && m.GetPortIndex() >= 0 && m.GetPortIndex() < m.knownPrices.Length ? m.knownPrices[m.GetPortIndex()] : null;
            return own?.buyPrices != null && own.buyPrices.Length > WoodPlanks.LumberGood && own.buyPrices[WoodPlanks.LumberGood] > 0
                ? own.buyPrices[WoodPlanks.LumberGood] : m.GetGoodPrice(WoodPlanks.LumberGood);
        }
    }
    // A trade-book sale: the game pays its price, then sells the first full crate of the good in the market area
    // (IslandMarketWarehouseArea.SellGood); the payment is cut to that crate's condition.
    // shortcut: the receipt and day log show the full price; replace if the trade book shows wrong totals to players.
    [HarmonyPatch(typeof(EconomyUI), "SellGood")] internal static class BookSaleValuePatch
    {
        internal struct Sale { internal int currency, price, goods; internal float factor; internal IslandMarket market; internal int good; }
        static void Prefix(EconomyUI __instance, out Sale __state)
        {
            __state = default;
            var market = Fields.Get<IslandMarket>(__instance, "currentIsland"); int good = __instance.currentSelectedGood;
            if (!market || market.currentPlayerGoods[good] <= 0) return;
            var area = Fields.Get<IslandMarketWarehouseArea>(market, "warehouseArea"); if (!area) return;
            Fields.Call(area, "ValidateList");
            Good crate = null;
            foreach (var g in Fields.Get<List<Good>>(area, "goodsInArea"))
                if ((int)Fields.Call(area, "GetGoodIndex", g) == good && (bool)Fields.Call(area, "IsGoodValid", g)) { crate = g; break; }
            float factor = crate ? CargoCondition.Factor(crate.GetComponent<ShipItem>()) : 1;
            if (factor >= 1) return;
            __state = new Sale { currency = (int)Fields.Get<Currency>(__instance, "currentPlayerCurrency"), market = market, good = good, factor = factor,
                goods = market.currentPlayerGoods[good], price = (int)Fields.Call(__instance, "GetSellPrice", market.GetPortIndex(), good) };
        }
        static void Postfix(Sale __state)
        {
            if (!__state.market || __state.market.currentPlayerGoods[__state.good] >= __state.goods) return;   // nothing sold
            PlayerGold.currency[__state.currency] -= __state.price - Mathf.RoundToInt(__state.price * __state.factor);
        }
    }

    // ---- Mission pay x the delivered crate's condition (the user, 2026-10-15) ----
    // As EconomyOverhaul's missions without modifiers: the game's gold and reputation for one delivered crate x its condition
    // (water condition when water damage is on x how intact it is when fragile cargo is on; 1 with both off).
    internal static class Delivery
    {
        [ThreadStatic] internal static Good Delivering;
        internal static float Factor(Mission m) => Delivering && Delivering.GetAssignedMission() == m ? CargoCondition.Factor(Delivering.GetComponent<ShipItem>()) : 1;
    }
    [HarmonyPatch(typeof(Good), "Deliver")] internal static class DeliveryContextPatch
    {
        static void Prefix(Good __instance, out Good __state) { __state = Delivery.Delivering; Delivery.Delivering = __instance; }
        static Exception Finalizer(Exception __exception, Good __state) { Delivery.Delivering = __state; return __exception; }
    }
    [HarmonyPatch(typeof(Mission), "GetDeliveryPrice")] internal static class MissionGoldPatch
    {
        static void Postfix(Mission __instance, ref int __result) { if (__result > 0) __result = Mathf.RoundToInt(__result * Delivery.Factor(__instance)); }
    }
    [HarmonyPatch(typeof(Mission), "GetDeliveryRep")] internal static class MissionRepPatch
    {
        [HarmonyPriority(Priority.Last)] [HarmonyAfter("com.DogEggz.postalexpansion")]
        static void Postfix(Mission __instance, ref int __result) { if (__result > 0) __result = Mathf.RoundToInt(__result * Delivery.Factor(__instance)); }
    }

    // ---- Fragile cargo and the hints (from EconomyOverhaul's NewMission.cs, without missions) ----
    public static partial class Rules
    {
        // The water hint's damage percent (CargoCondition.RefreshHint).
        public static int WaterHintPercent(float health) => Mathf.RoundToInt(100 - Mathf.Clamp(health, 0, 100));
        // Gems, sculptures and marble.
        public static bool FragileGood(int good) => good == 20 || good == 34 || good == 55;
        // A crate's impact damage 0-100 (0 intact): each counted hull hit adds the hull damage it added x 100 x 2.5.
        public static float FragileDamage(float impact, float hullAdded) => Math.Min(100, Math.Max(0, impact) + Math.Max(0, hullAdded) * 250);
        // The hint's "Cargo Damage" line counts up from 0% intact to 100% ruined (EconomyOverhaul's, 2026-10-16); the crack look
        // follows the same rounded value.
        public static int DamagePercent(float impact) => Mathf.RoundToInt(Mathf.Clamp(impact, 0, 100));
        public static float IntactFactor(float impact) => (100 - Mathf.Clamp(impact, 0, 100)) / 100f;
    }
}