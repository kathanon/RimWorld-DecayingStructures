using UnityEngine;
using Verse;

namespace DecayingStructures {
    public class CompDecay : ThingComp {
        public static DamageDef Deterioration = DefDatabase<DamageDef>.GetNamed("Deterioration");

        public const int TicksPerDay = 60000;

        private int lastUpdate;
        private float leftoverDamage = 0;

        public CompProperties_Decay Props => props as CompProperties_Decay;

        public CompDecay() {
            lastUpdate = Find.TickManager.TicksGame;
        }

        public void Tick(int currentTick) {
            var ticksPassed = currentTick - lastUpdate;
            lastUpdate = currentTick;
            var rate = Find.TickManager.Paused ? 0f : 1f;
            var variation = Random.Range(0.8f, 1.2f);
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
