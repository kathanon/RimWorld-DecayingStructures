using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace DecayingStructures;
public static class Ticker {
    private const int Slots = 600;
    private const int Interval = 1;

    private static readonly List<CompDecay>[] lists = new List<CompDecay>[Slots + 1];
    private static readonly HashSet<CompDecay> present = [];
    private static readonly HashSet<Map> seenMaps = [];
    private static int nextSlot = 0;
    private static int lastUpdate = 0;
    private static int nextAdd = 0;

    public static void Setup() {
        for (int i = 0; i < Slots + 1; i++) {
            lists[i] = [];
        }
    }

    public static void Reset() {
        seenMaps.Clear();
        Rebuild();
    }

    public static void VisitMap(Map map) {
        seenMaps.Add(map);
    }

    public static void Rebuild() {
        present.Clear();
        foreach (var list in lists) {
            list.Clear();
        }
        foreach (var map in seenMaps) {
            if (map.listerThings == null) {
                seenMaps.Remove(map);
            } else {
                AddFromMap(map);
            }
        }
    }

    private static void AddFromMap(Map map) =>
        AddAll(map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial));

    public static void AddAll(IEnumerable<Thing> things) {
        var comps = things
            .Select(t => t.TryGetComp<CompDecay>())
            .Where(c => c?.ShouldAffect() ?? false);
        foreach (CompDecay comp in comps) {
            Add(comp);
        }
    }

    public static void Add(Thing thing) {
        var comp = thing.TryGetComp<CompDecay>();
        if (comp != null && Options.ShouldAffectThing(thing)) {
            Add(comp);
        }
    }

    public static void Add(CompDecay comp) {
        if (present.Add(comp)) {
            lists[nextAdd].Add(comp);
            nextAdd = (nextAdd + 1) % Slots;
        }
    }

    public static void Tick(int currentTick) {
        if (currentTick - lastUpdate >= Interval) {
            lastUpdate += Interval;
            TickList(currentTick, nextSlot);
            nextSlot = (nextSlot + 1) % Slots;
        }
    }

    private static void TickList(int currentTick, int list) {
        var cur = lists[list];
        bool dirty = false;
        foreach (var comp in cur) {
            if (comp.ShouldAffect()) {
                comp.Tick(currentTick);
            } else {
                dirty = true;
            }
        }
        if (dirty) {
            lists[Slots].AddRange(cur.Where(c => c.ShouldRemove()));
            lists[list] = lists[Slots];
            lists[Slots] = cur;
            cur.Clear();
        }
    }
}

[HarmonyPatch]
public static class TickerHooks {
    [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
    [HarmonyPostfix]
    public static void SpawnSetup(Thing __instance) {
        Ticker.Add(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
    public static void Map_FinalizeInit(Map __instance) {
        Ticker.VisitMap(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Game))]
#if VERSION_1_5
    [HarmonyPatch(nameof(Game.DeinitAndRemoveMap))]
#else
    [HarmonyPatch(nameof(Game.DeinitAndRemoveMap_NewTemp))]
#endif
    public static void DeinitAndRemoveMap() {
        Ticker.Rebuild();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
    public static void Tick(int ___ticksGameInt) {
        Ticker.Tick(___ticksGameInt);
    }
}
