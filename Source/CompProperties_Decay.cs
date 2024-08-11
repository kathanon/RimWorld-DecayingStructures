using Verse;

namespace DecayingStructures;
public class CompProperties_Decay : CompProperties {
    public float decayMultiplier = 1f;

    public CompProperties_Decay() {
        compClass = typeof(CompDecay);
    }
}
