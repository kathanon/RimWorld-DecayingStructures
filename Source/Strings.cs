using System.Collections.Generic;
using Verse;

namespace DecayingStructures
{
    public static class Strings
    {
        // Non-translated constants
        public const string MOD_IDENTIFIER = "kathanon.DecayingStructures";
        public const string PREFIX = MOD_IDENTIFIER + ".";

        public static readonly string DecayRate_title  = (PREFIX + "DecayRate.title" ).Translate();
        public static readonly string DecayRate_desc   = (PREFIX + "DecayRate.desc"  ).Translate();
        public static readonly string Structures_title = (PREFIX + "Structures.title").Translate();
        public static readonly string Structures_desc  = (PREFIX + "Structures.desc" ).Translate();
        public static readonly string Outside_title    = (PREFIX + "Outside.title"   ).Translate();
        public static readonly string Outside_desc     = (PREFIX + "Outside.desc"    ).Translate();
        public static readonly string Owned_title      = (PREFIX + "Owned.title"     ).Translate();
        public static readonly string Owned_desc       = (PREFIX + "Owned.desc"      ).Translate();
        public static readonly string Variance_title   = (PREFIX + "Variance.title"  ).Translate();
        public static readonly string Variance_desc    = (PREFIX + "Variance.desc"   ).Translate();
    }
}
