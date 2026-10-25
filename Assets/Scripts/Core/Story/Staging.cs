using System;
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>How a timed change runs over its window: steadily, speeding up, slowing down, or both.</summary>
    public enum Ease { Linear, In, Out, Smooth }

    /// <summary>
    /// One staging line in a scene script (<see cref="SceneScript"/>): from <see cref="From"/> seconds into its shot
    /// (to <see cref="To"/>, for a change that runs over a window), <see cref="Thing"/> does <see cref="Verb"/>.
    /// </summary>
    public sealed class StageStep
    {
        /// <summary>Where it's written in its file, from 1.</summary>
        public int Line;
        public float From, To;
        /// <summary>It runs over From..To; otherwise it happens at From and holds.</summary>
        public bool Window;
        public string Thing, Verb;
        /// <summary>Its numbers, in the order written.</summary>
        public float[] N = new float[0];
        /// <summary>Its word (a pose, a side, a look, a figure to ride on), if it takes one.</summary>
        public string Word;
        /// <summary>The things a <c>holds</c> takes.</summary>
        public string[] Things = new string[0];
        /// <summary>Its colours, for <c>colour</c> and <c>flicker</c>.</summary>
        public Color A, B;
        public bool HasB;
        public Ease Ease;

        /// <summary>How far through its window it is at <paramref name="t"/>, eased (1 once it's past, or for an instant step).</summary>
        public float Progress(float t)
        {
            if (!Window || To <= From) return t >= From ? 1f : 0f;
            return Staging.Apply(Ease, Mathf.Clamp01((t - From) / (To - From)));
        }

        public override string ToString() => (Window ? From + "-" + To : From.ToString()) + " " + Thing + " " + Verb;
    }

    /// <summary>
    /// The staging vocabulary a scene script is written in (<c>docs/scenes.md</c>): the sets, the things on them
    /// (figures, props and named effects), the verbs, the colours. A scene can only name what's here, so a typo is a
    /// problem the check reports rather than something that silently does nothing. Places are in art pixels on the
    /// 320 x 180 stage: x from its left, heights from the set's floor.
    /// </summary>
    public static class Staging
    {
        /// <summary>A set: its painted backdrop (drawn by the cutscene view) and the height of its floor.</summary>
        public static readonly Dictionary<string, int> Sets = new Dictionary<string, int>
        {
            { "street", 34 }, { "den", 15 }, { "bay", 12 }, { "motorpool", 18 }, { "coldstore", 16 },
        };

        public enum Kind { Figure, Prop, Effect }

        /// <summary>A thing on the stage: what it is, where it stands unless told, its colour unless told, and its own verbs.</summary>
        public sealed class Thing
        {
            public readonly string Name;
            public readonly Kind Kind;
            public readonly Vector2 Place;
            public readonly Color Colour;
            public readonly string[] Verbs;

            public Thing(string name, Kind kind, float x, float y, Color colour, params string[] verbs)
            {
                Name = name;
                Kind = kind;
                Place = new Vector2(x, y);
                Colour = colour;
                Verbs = verbs;
            }
        }

        static readonly Color White = Color.white;
        public static readonly Color Warm = new Color(1f, 0.78f, 0.38f), Cyan = Palette.Cyan;

        /// <summary>Every thing a scene can put on the stage. The cutscene view draws each of them.</summary>
        public static readonly Dictionary<string, Thing> Things = Index(
            // the soldiers, drawn as in the game
            new Thing("tones2", Kind.Figure, 0f, 0f, White),
            new Thing("kie", Kind.Figure, 0f, 0f, White),
            new Thing("kcaj", Kind.Figure, 0f, 0f, White),
            new Thing("goon", Kind.Figure, 0f, 0f, White, "appear", "glitch-out"),
            // props
            new Thing("bag", Kind.Prop, 0f, 0f, White), // the crisp packet, painted (its own colours)
            new Thing("cup", Kind.Prop, 0f, 0f, White), // tones2's slushie, painted (its own colours)
            new Thing("card", Kind.Prop, 0f, 0f, Cyan),
            new Thing("remote", Kind.Prop, 0f, 0f, new Color(0.9f, 0.3f, 0.3f)),
            new Thing("sign", Kind.Prop, 232f, 84f, new Color(1f, 0.55f, 0.2f)),
            new Thing("window", Kind.Prop, 312f, 38f, Warm),
            new Thing("door", Kind.Prop, 267f, 0f, White),
            new Thing("doorway", Kind.Prop, 274f, 0f, Warm),
            new Thing("spark", Kind.Prop, 128f, 30f, Palette.Gold),
            new Thing("beacon", Kind.Prop, 94f, 108f, new Color(0.35f, 0.08f, 0.06f)),
            new Thing("clamp", Kind.Prop, 186f, 12f, Cyan, "rip"),
            new Thing("drone", Kind.Prop, 186f, 44f, new Color(0.32f, 0.35f, 0.4f)),
            new Thing("cable", Kind.Prop, 186f, 22f, new Color(0.1f, 0.1f, 0.12f)),
            new Thing("tones2-tabk", Kind.Prop, 186f, 2f, new Color(1f, 0.62f, 0.8f)),
            new Thing("kie-tabk", Kind.Prop, 136f, 0f, White),
            new Thing("kcaj-tabk", Kind.Prop, 261f, 0f, White), // kcaj's, as it rolls out of the cold store (a cone on top)
            new Thing("turret", Kind.Prop, 0f, 0f, White), // whichever tabk's in the shot
            new Thing("kcaj-corner", Kind.Prop, 60f, 99f, White), // his corner of the den, as it is when he's moved in
            // the creamery's cold store: the dark of its doorway as its doors part (sized open over the painted shut
            // doors), a chest freezer in the yard, and what kcaj comes out with and gets up to
            new Thing("store-opening", Kind.Prop, 261f, 11f, new Color(0.08f, 0.12f, 0.2f)),
            new Thing("freezer", Kind.Prop, 40f, 0f, new Color(0.82f, 0.9f, 0.97f)),
            new Thing("tub", Kind.Prop, 0f, 0f, new Color(0.96f, 0.93f, 0.86f)),
            new Thing("slushie", Kind.Prop, 0f, 0f, White), // the same slushie, painted
            new Thing("cones", Kind.Prop, 0f, 0f, new Color(0.88f, 0.66f, 0.36f)),
            new Thing("note", Kind.Prop, 0f, 0f, new Color(1f, 0.92f, 0.3f)),
            // named effects: bits of staging drawn by the view, timed and placed by the scene
            new Thing("snow", Kind.Effect, 0f, 0f, White),
            new Thing("searchlights", Kind.Effect, 0f, 0f, White),
            new Thing("chips", Kind.Effect, 66f, 0f, new Color(1f, 0.82f, 0.3f), "spill", "burst"),
            new Thing("jet", Kind.Effect, 0f, 0f, new Color(0.45f, 0.75f, 1f), "spray"),
            new Thing("closeup", Kind.Effect, 0f, 0f, White),
            new Thing("board", Kind.Effect, 0f, 0f, White),
            new Thing("shutter", Kind.Effect, 0f, 0f, White, "open", "up"),
            new Thing("lamps", Kind.Effect, 0f, 0f, White, "stutter"),
            new Thing("row", Kind.Effect, 0f, 0f, White),
            new Thing("junk", Kind.Effect, 0f, 0f, Cyan),
            new Thing("icicles", Kind.Effect, 0f, 0f, new Color(0.9f, 0.97f, 1f)),
            new Thing("frost", Kind.Effect, 261f, 0f, new Color(0.85f, 0.95f, 1f)),
            new Thing("steam", Kind.Effect, 136f, 0f, new Color(0.85f, 0.88f, 0.95f)),
            new Thing("cage", Kind.Effect, 100f, 0f, Cyan, "flicker-out"),
            new Thing("stars", Kind.Effect, 0f, 0f, Palette.Gold),
            new Thing("zz", Kind.Effect, 0f, 0f, new Color(1f, 0.98f, 0.92f)),
            new Thing("alarm", Kind.Effect, 0f, 0f, new Color(1f, 0.1f, 0.05f)));

        static Dictionary<string, Thing> Index(params Thing[] things)
        {
            var d = new Dictionary<string, Thing>();
            foreach (Thing t in things) d[t.Name] = t;
            return d;
        }

        /// <summary>The verbs every thing has.</summary>
        public static readonly string[] Common = { "show", "hide", "at", "move", "turn", "size", "colour", "fade", "flicker", "blink", "shake", "on" };
        /// <summary>The verbs the soldiers have besides.</summary>
        public static readonly string[] FigureVerbs = { "walk", "face", "pose", "look", "holds", "sway" };
        public static readonly string[] Poses = { "idle", "run", "crouch", "spray", "hatch", "repair" };
        public static readonly string[] Looks = { "plain", "holo", "trollo" };

        /// <summary>Colours by name; anything else is written #rrggbb or #rrggbbaa.</summary>
        public static readonly Dictionary<string, Color> Colours = new Dictionary<string, Color>
        {
            { "white", White }, { "black", Color.black }, { "warm", Warm }, { "cyan", Cyan },
            { "gold", Palette.Gold }, { "paper", new Color(1f, 0.98f, 0.92f) }, { "ink", new Color(0.1f, 0.07f, 0.1f) },
            { "water", new Color(0.45f, 0.75f, 1f) }, { "smoke", new Color(0.3f, 0.3f, 0.32f) }, { "wreck", new Color(0.4f, 0.37f, 0.36f) },
            { "red", new Color(1f, 0.25f, 0.15f) },
        };

        public static bool IsFigure(string thing) => Things.TryGetValue(thing, out Thing t) && t.Kind == Kind.Figure;

        /// <summary>Does <paramref name="thing"/> have <paramref name="verb"/>?</summary>
        public static bool Has(string thing, string verb)
        {
            if (!Things.TryGetValue(thing, out Thing t)) return false;
            if (Array.IndexOf(Common, verb) >= 0) return true;
            if (t.Kind == Kind.Figure && Array.IndexOf(FigureVerbs, verb) >= 0) return true;
            return Array.IndexOf(t.Verbs, verb) >= 0;
        }

        public static float Apply(Ease ease, float k)
        {
            switch (ease)
            {
                case Ease.In: return k * k;
                case Ease.Out: return 1f - (1f - k) * (1f - k);
                case Ease.Smooth: return k * k * (3f - 2f * k);
                default: return k;
            }
        }

        public static bool TryEase(string word, out Ease ease)
        {
            switch (word)
            {
                case "linear": ease = Ease.Linear; return true;
                case "in": ease = Ease.In; return true;
                case "out": ease = Ease.Out; return true;
                case "smooth": ease = Ease.Smooth; return true;
                default: ease = Ease.Linear; return false;
            }
        }

        /// <summary>A colour by name, or written #rrggbb / #rrggbbaa.</summary>
        public static bool TryColour(string word, out Color colour)
        {
            if (Colours.TryGetValue(word, out colour)) return true;
            colour = White;
            if (word.Length != 7 && word.Length != 9 || word[0] != '#') return false;
            var parts = new float[4] { 1f, 1f, 1f, 1f };
            for (int i = 0; i < (word.Length - 1) / 2; i++)
            {
                if (!int.TryParse(word.Substring(1 + i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out int v)) return false;
                parts[i] = v / 255f;
            }
            colour = new Color(parts[0], parts[1], parts[2], parts[3]);
            return true;
        }

        /// <summary>Where a held prop sits from its holder's feet, unless the scene says.</summary>
        public static Vector2 Hand(string prop)
        {
            switch (prop)
            {
                case "bag": return new Vector2(-6f, 9f);
                case "cup": return new Vector2(9f, 13f);
                case "remote": return new Vector2(-8f, 12f);
                case "card": return new Vector2(8f, 12f);
                case "tub": return new Vector2(8f, 11f);
                case "slushie": return new Vector2(9f, 12f);
                case "cones": return new Vector2(8f, 14f);
                default: return new Vector2(0f, 12f);
            }
        }
    }

    /// <summary>
    /// A shot's staging at a moment: where each thing is, how it looks and what it's doing, worked out from the shot's
    /// <see cref="StageStep"/>s. Each shot starts from nothing: a thing is on stage only if the shot puts it there.
    /// For each part of a thing (where it is, its turn, its colour, its pose...) the latest step to have started wins,
    /// so a later line takes over from an earlier one; a step with a window runs from one value to the other.
    /// </summary>
    public sealed class ShotStage
    {
        readonly StageStep[] m_Steps;

        public ShotStage(CutsceneShot shot)
        {
            m_Steps = shot?.Steps ?? new StageStep[0];
        }

        public ShotStage(StageStep[] steps)
        {
            m_Steps = steps ?? new StageStep[0];
        }

        /// <summary>The latest step of <paramref name="thing"/> started by <paramref name="t"/> whose verb is one of <paramref name="verbs"/>.</summary>
        public StageStep Latest(string thing, float t, params string[] verbs)
        {
            StageStep found = null;
            foreach (StageStep s in m_Steps)
                if (s.Thing == thing && s.From <= t && Array.IndexOf(verbs, s.Verb) >= 0 && (found == null || s.From >= found.From)) found = s;
            return found;
        }

        /// <summary>The thing's first step (its earliest), or null if the shot never mentions it.</summary>
        StageStep First(string thing, params string[] verbs)
        {
            StageStep found = null;
            foreach (StageStep s in m_Steps)
                if (s.Thing == thing && (verbs.Length == 0 || Array.IndexOf(verbs, s.Verb) >= 0) && (found == null || s.From < found.From)) found = s;
            return found;
        }

        /// <summary>
        /// Is it on stage? A thing the shot mentions is there from its first line. If the shot shows or hides it, those
        /// lines decide: a <c>hide</c> takes it off, a <c>show</c> puts it on (with a window, only for that window), and
        /// before the first of them it's there only if that first one is a <c>hide</c>.
        /// </summary>
        public bool Shown(string thing, float t)
        {
            StageStep first = First(thing);
            if (first == null) return false;
            StageStep last = Latest(thing, t, "show", "hide");
            if (last != null) return last.Verb == "show" && (!last.Window || t < last.To);
            StageStep firstSwitch = First(thing, "show", "hide");
            if (firstSwitch != null) return firstSwitch.Verb == "hide";
            return t >= first.From;
        }

        /// <summary>When it last came on stage (for effects that play in as they appear), or 0.</summary>
        public float ShownSince(string thing, float t)
        {
            StageStep last = Latest(thing, t, "show");
            if (last != null) return last.From;
            StageStep first = First(thing);
            return first != null ? Math.Max(0f, first.From) : 0f;
        }

        /// <summary>
        /// Where it stands at <paramref name="t"/>: x across the stage, and its height above the floor. Held things ride
        /// on whoever holds them; a soldier swaying or a tabk's engine shaking moves it a little (on the scene's own
        /// clock, <paramref name="now"/>).
        /// </summary>
        public Vector2 Where(string thing, float t, float now) => Where(thing, t, now, 0);

        Vector2 Where(string thing, float t, float now, int depth)
        {
            Vector2 at = Staging.Things.TryGetValue(thing, out Staging.Thing info) ? info.Place : Vector2.zero;
            StageStep s = Latest(thing, t, "at", "move", "walk", "on", "held");
            if (s != null)
            {
                switch (s.Verb)
                {
                    case "at":
                        at = new Vector2(s.N[0], s.N.Length > 1 ? s.N[1] : 0f);
                        break;
                    case "move":
                        at = Vector2.LerpUnclamped(new Vector2(s.N[0], s.N[1]), new Vector2(s.N[2], s.N[3]), s.Progress(t));
                        break;
                    case "walk":
                        at = new Vector2(Mathf.LerpUnclamped(s.N[0], s.N[1], s.Progress(t)), s.N.Length > 2 ? s.N[2] : HeightBefore(thing, s.From, now));
                        break;
                    case "on":
                    case "held":
                        if (depth < 4) at = Where(s.Word, t, now, depth + 1) + new Vector2(s.N[0], s.N[1]);
                        break;
                }
            }
            StageStep sway = Latest(thing, t, "sway");
            if (sway != null && (!sway.Window || t < sway.To)) at.x += Mathf.Round(Mathf.Sin(t * 3f) * 1.4f);
            StageStep shake = Latest(thing, t, "shake");
            if (shake != null && (!shake.Window || t < shake.To)) at.x += Fx.SceneShake.On(now) ? 0f : shake.N.Length > 0 ? shake.N[0] : 1f;
            return at;
        }

        /// <summary>A walk keeps the height it had before it.</summary>
        float HeightBefore(string thing, float t, float now)
        {
            StageStep s = null;
            foreach (StageStep step in m_Steps)
                if (step.Thing == thing && step.From <= t && (step.Verb == "at" || step.Verb == "move") && (s == null || step.From >= s.From)) s = step;
            if (s == null) return 0f;
            return s.Verb == "at" ? (s.N.Length > 1 ? s.N[1] : 0f) : s.N[3];
        }

        /// <summary>Its turn in degrees (anticlockwise), 0 unless the shot turns it.</summary>
        public float Turn(string thing, float t)
        {
            StageStep s = Latest(thing, t, "turn");
            if (s == null) return 0f;
            return s.N.Length > 1 ? Mathf.LerpUnclamped(s.N[0], s.N[1], s.Progress(t)) : s.N[0];
        }

        /// <summary>Its stretch across and up, 1 by 1 unless the shot sizes it.</summary>
        public Vector2 Size(string thing, float t)
        {
            StageStep s = Latest(thing, t, "size");
            if (s == null) return Vector2.one;
            Vector2 a = new Vector2(s.N[0], s.N[1]);
            return s.N.Length > 3 ? Vector2.LerpUnclamped(a, new Vector2(s.N[2], s.N[3]), s.Progress(t)) : a;
        }

        /// <summary>
        /// Its colour: its own unless the shot colours it, runs one colour into another, or flickers between two (on the
        /// scene's clock, <paramref name="now"/>); a fade sets how see-through it is.
        /// </summary>
        public Color Colour(string thing, float t, float now)
        {
            Color c = Staging.Things.TryGetValue(thing, out Staging.Thing info) ? info.Colour : Color.white;
            StageStep s = Latest(thing, t, "colour", "flicker");
            if (s != null && s.Verb == "colour") c = s.HasB ? Color.Lerp(s.A, s.B, s.Progress(t)) : s.A;
            else if (s != null) c = (!s.Window || t < s.To) && (int)(now * (s.N.Length > 0 ? s.N[0] : 14f)) % 4 == 0 ? s.A : s.B;
            StageStep fade = Latest(thing, t, "fade");
            if (fade != null) c.a *= fade.N.Length > 1 ? Mathf.LerpUnclamped(fade.N[0], fade.N[1], fade.Progress(t)) : fade.N[0];
            return c;
        }

        /// <summary>A blinking thing is lit only one beat in so many (on the scene's clock); everything else always is.</summary>
        public bool Lit(string thing, float t, float now)
        {
            StageStep s = Latest(thing, t, "blink");
            if (s == null || s.Window && t >= s.To) return true;
            return (int)(now * s.N[0]) % Math.Max(1, (int)s.N[1]) == 0;
        }

        /// <summary>A soldier's pose: running while it walks, else what the shot says (standing, unless told).</summary>
        public string Pose(string figure, float t)
        {
            if (Walking(figure, t)) return "run";
            StageStep s = Latest(figure, t, "pose");
            if (s == null || s.Window && t >= s.To) return "idle";
            return s.Word;
        }

        /// <summary>The soldier is mid-walk.</summary>
        public bool Walking(string figure, float t)
        {
            StageStep s = Latest(figure, t, "walk", "at", "move", "on", "held");
            return s != null && s.Verb == "walk" && t < s.To;
        }

        /// <summary>Which way it faces: 1 to the right (as everyone does unless told), -1 to the left.</summary>
        public int Facing(string thing, float t)
        {
            StageStep s = Latest(thing, t, "face");
            return s != null && s.Word == "left" ? -1 : 1;
        }

        /// <summary>A soldier's look: <c>plain</c>, <c>holo</c> (a Trollogram goon) or <c>trollo</c> (a Trollogram crewmate: kie or kcaj).</summary>
        public string Look(string figure, float t)
        {
            StageStep s = Latest(figure, t, "look");
            return s?.Word ?? (figure == "goon" ? "holo" : "plain");
        }

        /// <summary>How far through <paramref name="verb"/>'s window <paramref name="thing"/> is (0 before it, 1 after), or -1 if the shot never does it.</summary>
        public float Progress(string thing, string verb, float t)
        {
            StageStep s = Latest(thing, t, verb);
            if (s != null) return s.Progress(t);
            return First(thing, verb) != null ? 0f : -1f;
        }

        /// <summary>Seconds since <paramref name="thing"/> last did <paramref name="verb"/>, or -1 if it hasn't yet.</summary>
        public float Since(string thing, string verb, float t)
        {
            StageStep s = Latest(thing, t, verb);
            return s != null ? t - s.From : -1f;
        }

        /// <summary>Is <paramref name="verb"/> going on: a window it's in, or a step from then on.</summary>
        public bool Doing(string thing, string verb, float t)
        {
            StageStep s = Latest(thing, t, verb);
            return s != null && (!s.Window || t < s.To);
        }

        /// <summary>The thing's numbers for <paramref name="verb"/> (its latest started, else its first), or null.</summary>
        public float[] Numbers(string thing, string verb, float t)
        {
            StageStep s = Latest(thing, t, verb) ?? First(thing, verb);
            return s?.N;
        }

        /// <summary>
        /// The camera: how far in it is, and the point (art pixels from the stage's middle) it centres on. It starts
        /// each shot square on, and a <c>camera</c> line moves it on from wherever the one before left it.
        /// </summary>
        public void Camera(float t, out float zoom, out Vector2 look)
        {
            zoom = 1f;
            look = Vector2.zero;
            foreach (StageStep s in m_Steps)
            {
                if (s.Thing != "camera" || s.From > t) continue;
                float k = s.Progress(t);
                var to = new Vector2(s.N.Length > 1 ? s.N[1] : 0f, s.N.Length > 2 ? s.N[2] : 0f);
                zoom = Mathf.LerpUnclamped(zoom, s.N[0], k);
                look = Vector2.LerpUnclamped(look, to, k);
            }
        }
    }
}
