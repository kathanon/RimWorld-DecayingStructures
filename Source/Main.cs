using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace DecayingStructures;

[StaticConstructorOnStartup]
public class ModMain : Mod {
    public static ModMain Instance;

    static ModMain()
        => new Harmony(Strings.ID).PatchAll();

    public ModMain(ModContentPack content) : base(content) 
        => Instance = this;

    public void Setup() {
        Options.Setup(this);
        SetupDefs();
        Ticker.Setup();
    }

    private static void SetupDefs() {
        var defs = DefDatabase<ThingDef>.AllDefs.Where(d => d.category == ThingCategory.Building);
        foreach (var def in defs) {
            if (!def.HasComp(typeof(CompProperties_Decay))) {
                def.comps.Add(new CompProperties_Decay());
            }
        }
    }

    public override void DoSettingsWindowContents(Rect inRect) 
        => Options.DoGUI(inRect);

    public override string SettingsCategory() 
        => Strings.Name;
}

[HarmonyPatch]
public static class DefsLoadedHook {
    [HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.Init))]
    [HarmonyPostfix]
    public static void Init() 
        => ModMain.Instance.Setup();
}
