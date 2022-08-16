using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DecayingStructures {
    public static class Ticker {
        private const int Slots = 600;
        private const int Interval = 1;

        private static readonly List<CompDecay>[] lists = new List<CompDecay>[Slots + 1];
        private static readonly HashSet<CompDecay> present = new HashSet<CompDecay>();
        private static readonly HashSet<Map> seenMaps = new HashSet<Map>();
        private static int nextSlot = 0;
        private static int lastUpdate = 0;
        private static int nextAdd = 0;

        public static void Setup() {
            for (int i = 0; i < Slots + 1; i++) {
                lists[i] = new List<CompDecay>();
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
                AddFromMap(map);
            }
        }

        private static void AddFromMap(Map map) =>
            AddAll(map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial));

        public static void AddAll(IEnumerable<Thing> things) {
            var comps = things
                .Select(t => t.TryGetComp<CompDecay>())
                .Where(c => c != null && Options.ShouldAffectThing(c.parent));
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
                if (Options.ShouldAffectThing(comp.parent)) {
                    comp.Tick(currentTick);
                } else {
                    dirty = true;
                }
            }
            if (dirty) {
                lists[Slots].AddRange(cur.Where(c => !c.parent.Destroyed));
                lists[list] = lists[Slots];
                lists[Slots] = cur;
                cur.Clear();
            }
        }
    }

    [HarmonyPatch]
    public static class AddItemHook {
        [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
        [HarmonyPostfix]
        public static void SpawnSetup(Thing __instance) {
            Ticker.Add(__instance);
        }
    }
}
