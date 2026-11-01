using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>One once-only beat: the key the profile keeps once it has happened (<see cref="Profile.seen"/>), and what it is.</summary>
    public readonly struct OnceOnlyBeat
    {
        /// <summary>The key in players' saves. It never changes.</summary>
        public readonly string Key;
        /// <summary>What the beat is, in a few words.</summary>
        public readonly string What;

        public OnceOnlyBeat(string key, string what)
        {
            Key = key;
            What = what;
        }

        public override string ToString() => Key + " (" + What + ")";
    }

    /// <summary>
    /// Every once-only key the game saves, in one list: what the story check reads for "every reveal is in the plot"
    /// (<c>docs/design/story.md</c>, Reveals are story beats). The keys are set from six places, and this list takes
    /// each from its source rather than spelling it again: <see cref="Reveals.Coop"/>, a scene's
    /// <see cref="SceneScript.IdOf"/> for each of <see cref="SceneLibrary.Names"/>, <see cref="StoryRecord"/>'s three,
    /// <see cref="DenBeats"/> (the first visit, and a move-in for each crewmate in <see cref="Crew.All"/> who wasn't home for
    /// it), <see cref="KieHomecoming.Reveal"/>, the den's skits (<see cref="Skits.All"/>) and the skits that play in a mission or its debrief
    /// (<see cref="KeepItDown"/>, <see cref="TrackingUs"/>). <c>OnceOnlyTests</c> holds every place the game marks a key to this list,
    /// and pins the keys as they are in saves.
    /// </summary>
    public static class OnceOnly
    {
        static List<OnceOnlyBeat> s_All;

        /// <summary>The beats, by source: the co-op reveal, the story's three, the scenes, the den's, the bay's, the den's skits, the missions' skits.</summary>
        public static IReadOnlyList<OnceOnlyBeat> All => s_All ?? (s_All = Build());

        /// <summary><paramref name="key"/> is one the game saves.</summary>
        public static bool Has(string key)
        {
            foreach (OnceOnlyBeat beat in All)
                if (beat.Key == key) return true;
            return false;
        }

        static List<OnceOnlyBeat> Build()
        {
            var all = new List<OnceOnlyBeat>
            {
                new OnceOnlyBeat(Reveals.Coop, "co-op has opened: Mission one beaten solo"),
                new OnceOnlyBeat(StoryRecord.Coda, "the coda's been cleared, so its prompts don't show again"),
                new OnceOnlyBeat(StoryRecord.Debrief, "the first debrief has played"),
                new OnceOnlyBeat(StoryRecord.Super, "the super has charged once"),
            };
            foreach (string name in SceneLibrary.Names)
                all.Add(new OnceOnlyBeat(SceneScript.IdOf(name), "the scene " + name + " has played to the end"));
            all.Add(new OnceOnlyBeat(DenBeats.FirstVisit, "the den's first visit has played"));
            foreach (CrewMember m in Crew.All)
                if (!DenBeats.Starter(m.Soldier))
                    all.Add(new OnceOnlyBeat(DenBeats.MovedIn(m.Soldier), m.Name + " has moved into the den"));
            all.Add(new OnceOnlyBeat(KieHomecoming.Reveal, "kie's tabk has come home to the bay"));
            foreach ((string key, string what) in Skits.All) all.Add(new OnceOnlyBeat(key, what));
            all.Add(new OnceOnlyBeat(KeepItDown.Key, "the skit Keep it down has played in Mission one's coda"));
            all.Add(new OnceOnlyBeat(TrackingUs.Key, "the skit They're tracking us has played at Mission two's debrief"));
            return all;
        }
    }
}
