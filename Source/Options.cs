using HarmonyLib;
using HugsLib.Settings;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace DecayingStructures {
    public class Options {
        private static readonly float[] decaySteps = new float[] {
            0.1f, 0.25f, 0.5f, 1f, 2f, 3f, 6f, 12f
        };

        public static float DecayRate => decaySteps[decayRate];

        public static bool ShouldAffectThing(Thing thing) => 
            !thing.Destroyed && thing.Spawned && thing.def.IsBuildingArtificial && 
            thing.def.category == ThingCategory.Building &&
            (!structures || thing.def.designationCategory == Structure) && 
            (!outside    || thing.IsOutside()) && 
            (!owned      || thing.Faction == Faction.OfPlayer);

        private static SettingHandle<int>  decayRate;
        private static SettingHandle<bool> structures;
        private static SettingHandle<bool> outside;
        private static SettingHandle<bool> owned;
        private static SettingHandle<int>  variance;

        private static readonly DesignationCategoryDef Structure = 
            DefDatabase<DesignationCategoryDef>.GetNamed("Structure");

        public static void Setup(ModSettingsPack pack) {
            decayRate = pack.GetHandle(
                "decayRate",
                Strings.DecayRate_title,
                Strings.DecayRate_desc,
                2);
            decayRate.CustomDrawer = DrawDecaySlider;

            variance = pack.GetHandle(
                "variance",
                Strings.Variance_title,
                Strings.Variance_desc,
                20);
            variance.ValueChanged += VarianceUpdated;
            variance.CustomDrawer = DrawVarianceSlider;

            structures = pack.GetHandle(
                "structures",
                Strings.Structures_title,
                Strings.Structures_desc,
                false);
            structures.ValueChanged += Updated;

            outside = pack.GetHandle(
                "outside",
                Strings.Outside_title,
                Strings.Outside_desc,
                false);
            outside.ValueChanged += Updated;

            owned = pack.GetHandle(
                "owned",
                Strings.Owned_title,
                Strings.Owned_desc,
                false);
            owned.ValueChanged += Updated;

            // Avoid any array out of bounds if we lower the number of steps
            if (decayRate >= decaySteps.Length) decayRate.Value = decaySteps.Length - 1;

            // Initialize CompDecay
            VarianceUpdated(variance);
        }

        public static void Updated(SettingHandle _) => Ticker.Rebuild();

        public static void VarianceUpdated(SettingHandle _) => CompDecay.SetVariance(variance / 100f);

        public static bool DrawVarianceSlider(Rect rect) { 
            return DrawIntSlider(rect, variance, val => $"{val}%", 90); ;
        }

        public static bool DrawDecaySlider(Rect rect) { 
            return DrawIntSlider(rect, decayRate, _ => DecayRate.ToString(), decaySteps.Length - 1);
        }

        public static bool DrawIntSlider(Rect rect, SettingHandle<int> setting, Func<int,string> toString, int max) {
            int pos = setting;
            Rect label = rect.LeftPartPixels(30f);
            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(label, toString(pos));
            Text.Anchor = anchor;
            Rect slider = rect;
            slider.xMin += label.width;
            slider.yMin += 2f;
            setting.Value = (int) Widgets.HorizontalSlider(slider, pos, 0f, max, true, roundTo: 1f);
            return setting.Value != pos;
        }
    }

    public static class OptionsExtensionMethods {
        public static bool IsOutside(this Thing thing) {
            var room = thing.GetRoom();
            if (thing.LimitsRoom()) {
                var map = thing.Map;
                var pos = thing.Position;
                foreach (var adj in GenAdj.CardinalDirections) {
                    var pos2 = pos + adj;
                    bool outdoors = pos2.GetRoom(map)?.OutdoorsForWork ?? true;
                    bool roomBorder = pos2.GetEdifice(map)?.LimitsRoom() ?? false;
                    if (outdoors && !roomBorder) {
                        return true;
                    }
                }
                return false;
            } else {
                return room?.OutdoorsForWork ?? true;
            }
        }

        public static bool LimitsRoom(this Thing thing) =>
            thing.def.Fillage == FillCategory.Full || thing is Building_Door;
    }

    //[HarmonyPatch]
    public static class TestPatch {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Thing), nameof(Thing.GetInspectString))]
        public static string Desc(string original, Thing __instance) {
            Thing t = __instance;
            Room r = t.GetRoom();
            var addition = $"Outside: {t.IsOutside()}, Room ID: {r?.ID ?? -1}, size: {r?.CellCount ?? -1}, edifice: {t.Position.GetEdifice(t.Map) == t}";
            return (original.Length > 0) ? $"{addition}\n{original}" : addition;
        }
    }

}
