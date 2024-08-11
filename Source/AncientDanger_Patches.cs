using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using System.Collections.Generic;
using Verse;

namespace DecayingStructures;
[HarmonyPatch]
public static class AncientDanger_Patches {
    private static bool genActive = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SymbolResolver_AncientTemple), nameof(SymbolResolver_AncientTemple.Resolve))]
    public static void Resolve_Pre() => genActive = true;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SymbolResolver_AncientTemple), nameof(SymbolResolver_AncientTemple.Resolve))]
    public static void Resolve_Post() => genActive = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Sketch), nameof(Sketch.Spawn))]
    public static void Spawn_Pre(ref List<Thing> spawnedThings) {
        if (genActive && spawnedThings == null) {
            spawnedThings = new List<Thing>();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Sketch), nameof(Sketch.Spawn))]
    public static void Spawn_Post(List<Thing> spawnedThings) {
        if (genActive) {
            AncientDangerParts.Register(spawnedThings);
            genActive = false;
        }
    }
}
