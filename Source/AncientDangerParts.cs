using System.Collections.Generic;
using Verse;

namespace DecayingStructures;
public class AncientDangerParts : GameComponent, AncientDangerParts.IIncludesThing {
    public interface IIncludesThing {
        bool this[Thing thing] { get; }
    }

    private class AlwaysFalse : IIncludesThing {
        public bool this[Thing _] => false;
    }

    private const int FlushInterval = 200;
    private static readonly List<Thing> cache = new List<Thing>();
    private static IIncludesThing current = new AlwaysFalse();

    private HashSet<Thing> parts = new HashSet<Thing>();
    private long lastTick = -FlushInterval;

    public AncientDangerParts(Game _) {}

    public static IIncludesThing Includes => current;

    public bool this[Thing thing] => parts.Contains(thing);

    public override void ExposeData() {
        if (Scribe.mode == LoadSaveMode.Saving) {
            Flush();
        }
        Scribe_Collections.Look(ref parts, "parts", LookMode.Reference);
    }

    public override void GameComponentTick() {
        parts.AddRange(cache);
        cache.Clear();
        if (Find.TickManager.TicksGame - lastTick >= FlushInterval) {
            Flush();
            lastTick = Find.TickManager.TicksGame;
        }
    }

    public override void FinalizeInit() => current = this;

    public override void LoadedGame() => cache.Clear();

    public static void Register(IEnumerable<Thing> things) => cache.AddRange(things);

    private void Flush() => parts.RemoveWhere(t => t?.Destroyed ?? true);
}
