using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DecayingStructures; 
public class Options {
    private static SettingsClass obj;

    public static float DecayRate 
        => obj.DecayRate;

    public static int DecayStep 
        => obj.DecayStep;

    public static bool ShouldAffectThing(Thing thing) 
        => obj.ShouldAffectThing(thing);

    private static DesignationCategoryDef Structure;

    public static void Setup(ModMain main) {
        Structure = DefDatabase<DesignationCategoryDef>.GetNamed("Structure");
        obj = main.GetSettings<SettingsClass>();
    }

    public static void DoGUI(Rect inRect) 
        => obj.DoGUI(inRect);

    public enum HomeArea { All, Inside, Outside }

    public class SettingsClass : ModSettings {
        private readonly List<ILimitation> limitations;
        private readonly List<ISetting> settings;

        private readonly Setting<int> decayRate;
        private readonly Setting<int> decayStep;
        private readonly Setting<int> variance;

        private static readonly float[] decayRates = [
            0.1f, 0.25f, 0.5f, 1f, 2f, 3f, 6f, 12f
        ];

        public SettingsClass() {
            decayRate = new("decayRate", Strings.DecayRate_title, Strings.DecayRate_desc, 2,
                (r, s) => DrawIntSlider(r, ref s.value, v => decayRates[v].ToString(), max: decayRates.Length - 1));
            decayStep = new("decayStep", Strings.DecayStep_title, Strings.DecayStep_desc, 1,
                (r, s) => DrawIntSlider(r, ref s.value, v => v.ToString(), min: 1, max: 20));
            variance  = new("variance",  Strings.Variance_title,  Strings.Variance_desc,  20,
                (r, s) => DrawIntSlider(r, ref s.value, v => $"{v}%", max: 90, onChange: updateVariance));
            limitations = [
                new BoolLimitiation("structures", Strings.Structures_title, Strings.Structures_desc, false,
                    t => t.def.designationCategory == Structure),
                new BoolLimitiation("outside", Strings.Outside_title, Strings.Outside_desc, false,
                    t => t.IsOutside()),
                new BoolLimitiation("owned", Strings.Owned_title, Strings.Owned_desc, false,
                    t => t.Faction == Faction.OfPlayer),
                new BoolLimitiation("ancient", Strings.Ancient_title, Strings.Ancient_desc, false,
                    t => !AncientDangerParts.Includes[t], true),
                new Limitation<HomeArea>("home", Strings.HomeArea_title, Strings.HomeArea_desc, HomeArea.All,
                    (v, t) => v.Matches(t.Map.areaManager.Home[t.PositionHeld]),
                    (r, s) => s.value.SelectButton(r, x => s.value = x)),
            ];
            settings = [
                decayRate,
                decayStep,
                variance,
                .. limitations,
            ];

            updateVariance(variance.value);

            static void updateVariance(int v) 
                => CompDecay.SetVariance(v / 100f);
        }

        public void DoGUI(Rect rect) {
            float valueX = settings.Max(x => x.LabelWidth) + 4f;
            Rect row = rect.TopPartPixels(Widgets.CheckboxSize);
            foreach (var s in settings) {
                s.DoGUI(row, valueX);
                row.y += row.height + 4f;
            }
        }

        public float DecayRate => decayRates[decayRate.value];

        public int DecayStep => decayStep.value;


        public override void ExposeData() 
            => settings.ForEach(x => x.ExposeData());

        public bool ShouldAffectThing(Thing thing) =>
            !thing.Destroyed && thing.Spawned && thing.def.IsBuildingArtificial &&
            thing.def.category == ThingCategory.Building &&
            limitations.All(x => x.Test(thing));

        public void VarianceUpdated() 
            => CompDecay.SetVariance(variance.value / 100f);

        public static bool DrawIntSlider(Rect rect,
                                         ref int setting,
                                         Func<int, string> toString,
                                         int max,
                                         int min = 0,
                                         Action<int> onChange = null) {
            if (rect.width > 400f) rect.width = 400f;
            int old = setting;
            Rect label = rect.LeftPartPixels(30f);
            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(label, toString(old));
            Text.Anchor = anchor;
            Rect slider = rect;
            slider.xMin += label.width;
            slider.yMin += 2f;
            setting = (int) Widgets.HorizontalSlider(slider, old, min, max, true, roundTo: 1f);
            if (setting != old) onChange?.Invoke(setting);
            return setting != old;
        }

        private interface ISetting {
            public void ExposeData();
            public void DoGUI(Rect row, float valueX);
            public float LabelWidth { get; }
        }

        private interface ILimitation : ISetting {
            public bool Test(Thing t);
        }

        private class Setting<T>(string name,
                                 string label,
                                 string description,
                                 T defaultValue,
                                 Action<Rect, Setting<T>> valueCtrl) : ISetting {
            protected readonly string name = name;
            protected readonly string label = label;
            protected readonly string description = description;
            protected readonly T defaultValue = defaultValue;
            protected readonly Action<Rect, Setting<T>> valueCtrl = valueCtrl;
            public T value = defaultValue;

            public float LabelWidth 
                => Text.CalcSize(label).x;

            public void DoGUI(Rect row, float valueX) {
                Widgets.Label(row, label);
                TooltipHandler.TipRegion(row, description);
                row.xMin = valueX;
                valueCtrl(row, this);
            }

            public void ExposeData() 
                => Scribe_Values.Look(ref value, name, defaultValue);
        }

        private class Limitation<T>(string name,
                                    string label,
                                    string description,
                                    T defaultValue,
                                    Func<T, Thing, bool> check,
                                    Action<Rect, Setting<T>> valueCtrl) 
                : Setting<T>(name, label, description, defaultValue, valueCtrl), ILimitation {

            protected readonly Func<T, Thing, bool> check = check;

            public bool Test(Thing t) 
                => check(value, t);
        }

        private class BoolLimitiation(string name,
                                      string label,
                                      string description,
                                      bool defaultValue,
                                      Func<Thing, bool> check,
                                      bool invert = false)
            : Limitation<bool>(name, label, description, defaultValue, (v, t) => v == invert || check(t), Ctrl) {
            protected readonly bool invert = invert;

            private static void Ctrl(Rect r, Setting<bool> s) {
                Widgets.Checkbox(r.min, ref s.value, r.height);
            }
        }
    }
}

public static class OptionsExtensionMethods {
    private static readonly float AreaButtonSize = 100f;
    private static readonly (string label, Options.HomeArea value)[] areaChoices = [
        (Strings.HomeArea_All,     Options.HomeArea.All), 
        (Strings.HomeArea_Inside,  Options.HomeArea.Inside), 
        (Strings.HomeArea_Outside, Options.HomeArea.Outside)
    ];

    public static bool Matches(this Options.HomeArea setting, bool home)
        => setting switch { 
            Options.HomeArea.All     => true,
            Options.HomeArea.Inside  => home,
            Options.HomeArea.Outside => !home,
            _ => false,
        };

    public static void SelectButton(this Options.HomeArea setting, Rect r, Action<Options.HomeArea> set) {
        if (Widgets.ButtonText(r.LeftPartPixels(AreaButtonSize), areaChoices[(int) setting].label)) {
            Find.WindowStack.Add(new FloatMenu(areaChoices.Select(
                    x => new FloatMenuOption(x.label, () => set(x.value))
                ).ToList()));
        }
    }

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
