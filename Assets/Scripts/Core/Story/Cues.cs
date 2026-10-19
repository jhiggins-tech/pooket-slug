using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>The sounds a cutscene cue can play. The view maps each to its clip.</summary>
    public enum CueSound { None, Thump, Glitch, Splat, Pickup, Hit, Servo, RocketRumble, Charge, Siren, IntroRise, Intro }

    /// <summary>The one-off staging a cue can start on the cutscene's stage, besides its sound.</summary>
    [Flags]
    public enum CueStaging
    {
        None = 0,
        /// <summary>The Trollograms on stage flicker for a moment. (The rest of the staging is the scene's own: <see cref="Staging"/>.)</summary>
        Flicker = 1,
    }

    /// <summary>What a cue does: up to two sounds at their volumes, and any staging.</summary>
    public readonly struct CueEffect
    {
        public readonly CueSound Sound, Sound2;
        public readonly float Volume, Volume2;
        public readonly CueStaging Staging;

        public CueEffect(CueSound sound, float volume, CueStaging staging = CueStaging.None, CueSound sound2 = CueSound.None, float volume2 = 0f)
        {
            Sound = sound;
            Volume = volume;
            Staging = staging;
            Sound2 = sound2;
            Volume2 = volume2;
        }
    }

    /// <summary>
    /// The cutscene cues (<see cref="CutsceneCue"/>): their ids, and what each one does, so a scene can only name a cue
    /// that exists and every cue has an effect (a test holds every scene to it). A <c>say:</c> cue has the announcer
    /// read a line (<see cref="Say"/>) instead. Scene scripts name them (<c>2.2 cue drop</c>), and a script naming one
    /// that isn't here has a problem (<see cref="SceneScript"/>).
    /// </summary>
    public static class Cues
    {
        // the opening
        public const string Door = "door", Static = "static", Drop = "drop", Goon = "goon", Jet = "jet", Glitch = "glitch";
        public const string CardDrop = "card-drop", CardUp = "card-up", Crush = "crush", Card = "card";
        // the motor pool
        public const string Shutter = "shutter", Hum = "hum", Engine = "engine", Hatch = "hatch", Slam = "slam", Turret = "turret";
        // freeing kie
        public const string Cage = "cage", KieDrop = "kie-drop", Snacks = "snacks", Rip = "rip", Drone = "drone", Lift = "lift";
        public const string Remote = "remote", Alarm = "alarm";
        // freeing kcaj
        public const string ColdDoor = "cold-door", Frost = "frost", Freezer = "freezer", Munch = "munch", Note = "note";
        /// <summary>A character's intro: its build-up starts (the riser), and it lands (the musical hit).</summary>
        public const string IntroBuild = "intro-build", Intro = "intro";

        /// <summary>A cue that has the announcer say a line: the prefix, then the line's id.</summary>
        public const string SayPrefix = "say:";
        /// <summary>The announcer's "mission start", as the opening ends.</summary>
        public const string SayMissionStart = SayPrefix + "mission-start";

        static readonly Dictionary<string, CueEffect> s_Effects = new Dictionary<string, CueEffect>
        {
            { Door, new CueEffect(CueSound.Thump, 0.7f) },
            { Static, new CueEffect(CueSound.Glitch, 0.4f, CueStaging.Flicker) },
            { Drop, new CueEffect(CueSound.Splat, 0.6f) },
            { Goon, new CueEffect(CueSound.Glitch, 0.5f) },
            { Jet, new CueEffect(CueSound.Splat, 0.8f) },
            { Glitch, new CueEffect(CueSound.Glitch, 0.7f) },
            { CardDrop, new CueEffect(CueSound.Pickup, 0.6f) },
            { CardUp, new CueEffect(CueSound.Pickup, 0.9f) },
            { Crush, new CueEffect(CueSound.Hit, 0.7f) },
            { Card, new CueEffect(CueSound.Thump, 1f) },
            { Shutter, new CueEffect(CueSound.Servo, 0.8f, sound2: CueSound.RocketRumble, volume2: 0.35f) },
            { Hum, new CueEffect(CueSound.Charge, 0.25f) },
            { Engine, new CueEffect(CueSound.RocketRumble, 0.7f) },
            { Hatch, new CueEffect(CueSound.Thump, 0.5f) },
            { Slam, new CueEffect(CueSound.Thump, 1f, sound2: CueSound.Hit, volume2: 0.5f) },
            { Turret, new CueEffect(CueSound.Servo, 0.7f) },
            { Cage, new CueEffect(CueSound.Charge, 0.6f) },
            { KieDrop, new CueEffect(CueSound.Thump, 0.9f) },
            { Snacks, new CueEffect(CueSound.Pickup, 0.5f) },
            { Rip, new CueEffect(CueSound.Glitch, 0.6f, sound2: CueSound.Hit, volume2: 0.5f) },
            { Drone, new CueEffect(CueSound.RocketRumble, 0.5f) },
            { Lift, new CueEffect(CueSound.Servo, 0.8f) },
            { Remote, new CueEffect(CueSound.Pickup, 0.8f) },
            { Alarm, new CueEffect(CueSound.Siren, 0.4f) },
            { ColdDoor, new CueEffect(CueSound.Servo, 0.7f, sound2: CueSound.RocketRumble, volume2: 0.25f) },
            { Frost, new CueEffect(CueSound.Charge, 0.3f) },
            { Freezer, new CueEffect(CueSound.Thump, 0.6f) },
            { Munch, new CueEffect(CueSound.Splat, 0.5f) },
            { Note, new CueEffect(CueSound.Pickup, 0.6f) },
            { IntroBuild, new CueEffect(CueSound.IntroRise, 0.5f) },
            { Intro, new CueEffect(CueSound.Intro, 0.5f) },
        };

        /// <summary>Every cue with an effect (all but the <c>say:</c> ones), by id.</summary>
        public static IReadOnlyDictionary<string, CueEffect> Effects => s_Effects;

        /// <summary>A cue that has the announcer say <paramref name="line"/>.</summary>
        public static string Say(string line) => SayPrefix + line;

        /// <summary>The line a <c>say:</c> cue has the announcer read, or null for any other cue.</summary>
        public static string SaidLine(string id) =>
            id != null && id.Length > SayPrefix.Length && id.StartsWith(SayPrefix, StringComparison.Ordinal) ? id.Substring(SayPrefix.Length) : null;

        public static bool TryEffect(string id, out CueEffect effect)
        {
            if (id != null) return s_Effects.TryGetValue(id, out effect);
            effect = default;
            return false;
        }

        /// <summary>The cue does something: it has an effect, or it says a line.</summary>
        public static bool Handled(string id) => id != null && s_Effects.ContainsKey(id) || SaidLine(id) != null;
    }
}
