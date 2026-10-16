using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>A kit's button: what a move is fired with.</summary>
    public enum KitSlot { Fire, Special, Super, Ability }

    /// <summary>The keys a demo holds down (<see cref="DemoScript"/>).</summary>
    [Flags]
    public enum DemoKeys { None = 0, Left = 1, Right = 2, Up = 4, Down = 8, Jump = 16, Fire = 32, Special = 64, Super = 128, Ability = 256 }

    /// <summary>
    /// One move's demo: a little scene with goons (a map, as <see cref="Level"/> reads them) and the keys our soldier
    /// holds, second by second. CI plays each one in the game (<c>?demo=</c>) and records it as the clip the ⓘ screen
    /// shows (<c>tools/demos.mjs</c>).
    /// </summary>
    public sealed class DemoScript
    {
        /// <summary>The demo's frame rate: it runs on a fixed step, so a recording shows it at its real speed however fast the machine draws.</summary>
        public const float Fps = 30f;

        public readonly string[] Map;
        public readonly float Seconds;
        /// <summary>The super starts charged, so a demo can fire it.</summary>
        public readonly bool SuperReady;
        readonly (float from, float to, DemoKeys keys)[] m_Steps;

        public DemoScript(string[] map, float seconds, bool superReady, params (float from, float to, DemoKeys keys)[] steps)
        {
            Map = map;
            Seconds = seconds;
            SuperReady = superReady;
            m_Steps = steps;
        }

        /// <summary>The keys held at <paramref name="t"/> seconds in.</summary>
        public DemoKeys Held(float t)
        {
            DemoKeys keys = DemoKeys.None;
            if (t < 0f) return keys;
            foreach ((float from, float to, DemoKeys k) in m_Steps)
                if (t >= from && t < to) keys |= k;
            return keys;
        }

        /// <summary>
        /// Our soldier's input on frame <paramref name="frame"/> (the first is 1, at 1/<see cref="Fps"/> s): what's held, and
        /// what went down since the frame before. Counted in frames, so the frame before is always the same instant.
        /// </summary>
        public SoldierInput InputAt(int frame)
        {
            DemoKeys now = Held(frame / Fps), before = Held((frame - 1) / Fps);
            DemoKeys down = now & ~before;
            return new SoldierInput
            {
                Left = (now & DemoKeys.Left) != 0,
                Right = (now & DemoKeys.Right) != 0,
                Up = (now & DemoKeys.Up) != 0,
                Down = (now & DemoKeys.Down) != 0,
                Jump = (now & DemoKeys.Jump) != 0,
                JumpPressed = (down & DemoKeys.Jump) != 0,
                Fire = (now & DemoKeys.Fire) != 0,
                FirePressed = (down & DemoKeys.Fire) != 0,
                Special = (now & DemoKeys.Special) != 0,
                SpecialPressed = (down & DemoKeys.Special) != 0,
                Super = (now & DemoKeys.Super) != 0,
                SuperPressed = (down & DemoKeys.Super) != 0,
                Ability = (now & DemoKeys.Ability) != 0,
                AbilityPressed = (down & DemoKeys.Ability) != 0,
            };
        }

        /// <summary>
        /// A demo's map: open sky over the given ground row (40 tiles, with 'P' where our soldier starts and goons on
        /// it), and two rows of ground under it, as tall as Mission one so the screen frames it the same.
        /// <paramref name="air"/>, if given, is the row a drone flies in.
        /// </summary>
        public static string[] Ground(string row, string air = null)
        {
            string sky = new string(Level.Empty, row.Length), floor = new string(Level.Ground, row.Length);
            return new[] { sky, sky, air ?? sky, sky, sky, sky, sky, sky, sky, row, floor, floor };
        }
    }

    /// <summary>One move as the ⓘ screen explains it: its button, its name, what it does, and its demo.</summary>
    public sealed class MoveGuide
    {
        public readonly SoldierDef Soldier;
        public readonly KitSlot Slot;
        public readonly string Name, Text;
        public readonly DemoScript Demo;

        public MoveGuide(SoldierDef soldier, KitSlot slot, string name, string text, DemoScript demo)
        {
            Soldier = soldier;
            Slot = slot;
            Name = name;
            Text = text;
            Demo = demo;
        }

        /// <summary>The button's label on the touch pad: FIRE, SP, SU or AB.</summary>
        public string Button => KitGuide.ButtonOf(Slot);

        /// <summary>The demo's name, in <c>?demo=</c> and the clip's file name (<c>demos/&lt;id&gt;.mp4</c>).</summary>
        public string DemoId => Soldier.Name.ToLowerInvariant() + "-" + Slot.ToString().ToLowerInvariant();

        public override string ToString() => DemoId;
    }

    /// <summary>
    /// What the ⓘ screen says about each soldier's kit (<c>docs/design/player.md</c>, Discovering the kit; the moves are
    /// designed in <c>docs/design/kits.md</c>), with a demo for each move. Keep the words in step with the kits.
    /// </summary>
    public static class KitGuide
    {
        const float Lead = 0.5f; // each demo opens on the scene for a moment

        public static readonly IReadOnlyList<MoveGuide> Tones2 = new[]
        {
            new MoveGuide(Content.Tones2, KitSlot.Fire, "TEN-1",
                "Hold FIRE for a water jet that builds from spray to a clean arc. It trickles damage and pushes goons back; let go and the pressure resets.",
                new DemoScript(DemoScript.Ground("...P..........s....s..r................."), 5.5f, false,
                    (Lead, 3.4f, DemoKeys.Fire), (3.4f, 4.8f, DemoKeys.Fire | DemoKeys.Right))),
            new MoveGuide(Content.Tones2, KitSlot.Special, "TEN-2",
                "Shake, then rocket-jump straight up and steer in the air. The launch carpets the floor in toxic mud that poisons goons who walk through it.",
                new DemoScript(DemoScript.Ground("...P............r....r...r.............."), 5.5f, false,
                    (0.8f, 0.9f, DemoKeys.Special), (1.5f, 2.1f, DemoKeys.Right))),
            new MoveGuide(Content.Tones2, KitSlot.Super, "TEN-3",
                "A second and a half of spew that melts anything close, and you can walk while it pours. It leaves sludge that burns goons for about 15 seconds. Kills charge it.",
                new DemoScript(DemoScript.Ground("...P...........r...r.....r....r........."), 6f, true,
                    (Lead, 1.0f, DemoKeys.Right), (1.0f, 1.1f, DemoKeys.Super), (1.1f, 2.5f, DemoKeys.Right))),
        };

        public static readonly IReadOnlyList<MoveGuide> Kie = new[]
        {
            new MoveGuide(Content.Kie, KitSlot.Fire, "WEASEL POP",
                "Each press tumbles out three weasels that scurry under the nearest goon and pop. They're lobbed, so they reach goons behind cover.",
                new DemoScript(DemoScript.Ground("...P.........==..s...==..s.............."), 5.5f, false,
                    (Lead, 0.6f, DemoKeys.Fire), (1.6f, 1.7f, DemoKeys.Fire), (2.7f, 2.8f, DemoKeys.Fire))),
            new MoveGuide(Content.Kie, KitSlot.Special, "TROLLOGRAM",
                "Tap to drop a Trollogram of yourself, up to three. Goons shoot a Trollogram instead of you, and a shot one blows up. Hold to swap places with the one furthest away.",
                new DemoScript(DemoScript.Ground("....P..............s.....s.............."), 6f, false,
                    (Lead, 1.2f, DemoKeys.Right), (1.2f, 1.3f, DemoKeys.Special), (1.4f, 2.2f, DemoKeys.Left), (3.4f, 3.9f, DemoKeys.Special))),
            new MoveGuide(Content.Kie, KitSlot.Super, "STEAL",
                "A roulette spins over the toughest enemy on screen, and you lift a weapon off it: the tougher it is, the bigger the prize. Kills charge it.",
                new DemoScript(DemoScript.Ground("...P...........s......r................."), 5.5f, true,
                    (0.6f, 0.7f, DemoKeys.Super), (2.2f, 4.6f, DemoKeys.Fire))),
        };

        /// <summary>Every crewmate's moves, in crew order (<see cref="Crew.All"/>).</summary>
        public static IReadOnlyList<MoveGuide> All
        {
            get
            {
                if (s_All != null) return s_All;
                var all = new List<MoveGuide>();
                foreach (CrewMember m in Crew.All) all.AddRange(m.Moves);
                return s_All = all;
            }
        }
        static IReadOnlyList<MoveGuide> s_All;

        /// <summary>A soldier's moves, in the order of their buttons (<see cref="CrewMember.Moves"/>); empty for one who isn't crew.</summary>
        public static IReadOnlyList<MoveGuide> For(SoldierDef soldier) => Crew.Of(soldier)?.Moves ?? Array.Empty<MoveGuide>();

        /// <summary>The move with this demo id (<see cref="MoveGuide.DemoId"/>), or null.</summary>
        public static MoveGuide Find(string demoId)
        {
            if (string.IsNullOrEmpty(demoId)) return null;
            foreach (MoveGuide m in All)
                if (string.Equals(m.DemoId, demoId.Trim(), StringComparison.OrdinalIgnoreCase)) return m;
            return null;
        }

        public static string ButtonOf(KitSlot slot)
        {
            switch (slot)
            {
                case KitSlot.Fire: return "FIRE";
                case KitSlot.Special: return "SP";
                case KitSlot.Super: return "SU";
                default: return "AB";
            }
        }
    }
}
