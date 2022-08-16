using HarmonyLib;
using RimWorld;
using RimWorld.SketchGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace DecayingStructures
{
    public class Main : HugsLib.ModBase {
        public override string ModIdentifier => Strings.MOD_IDENTIFIER;

        public override void DefsLoaded() {
            Options.Setup(Settings);
            SetupDefs();
        }

        public static void SetupDefs() {
            var defs = DefDatabase<ThingDef>.AllDefs.Where(d => d.category == ThingCategory.Building);
            foreach (var def in defs) {
                if (!def.HasComp(typeof(CompProperties_Decay))) {
                    def.comps.Add(new CompProperties_Decay());
                }
            }
            Ticker.Setup();
        }

        public override void MapLoaded(Map map) => Ticker.VisitMap(map);

        public override void MapDiscarded(Map map) => Ticker.Rebuild();

        public override void Tick(int currentTick) => Ticker.Tick(currentTick);
    }
}
