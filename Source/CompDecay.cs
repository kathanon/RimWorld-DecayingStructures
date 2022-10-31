using UnityEngine;
using Verse;

namespace DecayingStructures {
    public class CompDecay : ThingComp {
        public static DamageDef Deterioration = DefDatabase<DamageDef>.GetNamed("Deterioration");

        public const float VariationDrift = 0.01f;

        private static float minVariation = 0.8f;
        private static float maxVariation = 1.2f;
        private static int rerollIndex = 0;

        public static void SetVariance(float var) {
            minVariation = 1f - var;
            maxVariation = 1f + var;
            rerollIndex++;
        }

        public const int TicksPerDay = 60000;

        private int lastUpdate;
        private int lastReroll = 0;
        private float leftoverDamage = 0;
        private float variation = 1f;

        private float Variation { 
            get { 
                if (lastReroll != rerollIndex) {
                    lastReroll = rerollIndex;
                    variation = Random.Range(minVariation, maxVariation);
                } else {
                    float updated = variation + Random.Range(-VariationDrift, VariationDrift);
                    variation = Mathf.Clamp(updated, minVariation, maxVariation);
                }
                return variation;
            } 
        }

        public CompProperties_Decay Props => props as CompProperties_Decay;

        public CompDecay() {
            lastUpdate = Find.TickManager.TicksGame;
        }

        public void Tick(int currentTick) {
            var ticksPassed = currentTick - lastUpdate;
            lastUpdate = currentTick;
            var rate = Find.TickManager.Paused ? 0f : 1f;
            float variation = Variation;
            var rawDamage =
                ticksPassed * rate / TicksPerDay * variation *
                Options.DecayRate * Props.decayMultiplier +
                leftoverDamage;
            var damage = Mathf.Floor(rawDamage);
            leftoverDamage = rawDamage - damage;

            if (damage > 0) {
                DamageInfo di = new DamageInfo(Deterioration, damage, spawnFilth: false);
                parent.TakeDamage(di);
            }
        }
    }
}
