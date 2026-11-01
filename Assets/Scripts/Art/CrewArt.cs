// How the playable crew look: each crewmate's own layers (their head, and any body or legs part of their own) over the
// shared figure in ArtData, and the sprite set built from them for each crewmate in their paint (one each: there's no second paint). The frames
// and clips are CrewFigure's (Core); a new crewmate gets every one of them by giving a look here.
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A crewmate's own layers: their head's frames (hair, face, hats), and any shared body or legs frame they swap for
    /// one of their own. A head frame they haven't drawn falls back to their forward one.
    /// </summary>
    public sealed class CrewLook
    {
        readonly IReadOnlyDictionary<CrewHead, string[]> m_Heads;
        readonly IReadOnlyDictionary<CrewBody, string[]> m_Bodies;
        readonly IReadOnlyDictionary<CrewLegs, string[]> m_Legs;

        public CrewLook(IReadOnlyDictionary<CrewHead, string[]> heads, IReadOnlyDictionary<CrewBody, string[]> bodies = null,
            IReadOnlyDictionary<CrewLegs, string[]> legs = null)
        {
            m_Heads = heads;
            m_Bodies = bodies ?? new Dictionary<CrewBody, string[]>();
            m_Legs = legs ?? new Dictionary<CrewLegs, string[]>();
        }

        /// <summary>The bandana head, on the shared body and legs: anyone without a look of their own.</summary>
        public static readonly CrewLook Shared = new CrewLook(new Dictionary<CrewHead, string[]>
        {
            { CrewHead.Front, ArtData.Head }, { CrewHead.Down, ArtData.HeadDown },
        });

        public string[] Head(CrewHead head) => m_Heads.TryGetValue(head, out string[] rows) ? rows : m_Heads[CrewHead.Front];

        /// <summary>The body frame's rows: their own, if they've one, else the shared body's.</summary>
        public string[] Body(CrewBody body) => m_Bodies.TryGetValue(body, out string[] rows) ? rows : CrewArt.SharedBody(body);

        public string[] Legs(CrewLegs legs) => m_Legs.TryGetValue(legs, out string[] rows) ? rows : CrewArt.SharedLegs(legs);
    }

    /// <summary>One crewmate's sprites in one paint: a whole figure (head and body) per body frame, and the legs'.</summary>
    public sealed class PlayerSkin
    {
        readonly Sprite[] m_Body, m_Legs;

        public PlayerSkin(Sprite[] body, Sprite[] legs)
        {
            m_Body = body;
            m_Legs = legs;
        }

        /// <summary>The top half for <paramref name="body"/>: the shared body, with their head on it where the frame puts it.</summary>
        public Sprite Body(CrewBody body) => m_Body[(int)body];

        public Sprite Legs(CrewLegs legs) => m_Legs[(int)legs];

        /// <summary>Facing forward with the gun: the figure's picture on cards and in the HUD.</summary>
        public Sprite Portrait => Body(CrewBody.Forward);

        /// <summary>The run's legs, <paramref name="frame"/> of four.</summary>
        public Sprite RunLegs(int frame) => Legs(CrewLegs.Run1 + frame);
    }

    /// <summary>The shared figure's rows by frame, and the crew's looks.</summary>
    public static class CrewArt
    {
        /// <summary>Each crewmate's look; anyone not here wears <see cref="CrewLook.Shared"/>.</summary>
        static readonly Dictionary<SoldierDef, CrewLook> s_Looks = new Dictionary<SoldierDef, CrewLook>
        {
            { Content.Tones2, HeadOf(ArtData.Tones2Head, ArtData.Tones2HeadDown) },
            { Content.Kie, new CrewLook(new Dictionary<CrewHead, string[]>
                {
                    { CrewHead.Front, ArtData.KieHead }, { CrewHead.Down, ArtData.KieHeadDown },
                    { CrewHead.Grin, ArtData.KieHeadGrin }, { CrewHead.Blank, ArtData.KieHeadBlank }, { CrewHead.Glum, ArtData.KieHeadGlum },
                }) },
            { Content.Kcaj, HeadOf(ArtData.KcajHead, ArtData.KcajHeadDown) },
        };

        /// <summary>A look that's only a head of their own (facing forward, and bowed) on the shared body and legs.</summary>
        static CrewLook HeadOf(string[] front, string[] down) =>
            new CrewLook(new Dictionary<CrewHead, string[]> { { CrewHead.Front, front }, { CrewHead.Down, down } });

        public static CrewLook LookOf(SoldierDef soldier) =>
            soldier != null && s_Looks.TryGetValue(soldier, out CrewLook look) ? look : CrewLook.Shared;

        /// <summary>The shared body's rows for <paramref name="body"/> (in <see cref="CrewBody"/>'s order).</summary>
        public static string[] SharedBody(CrewBody body) => s_Bodies[(int)body];

        /// <summary>The canvas row <paramref name="body"/>'s rows start on.</summary>
        public static int BodyTop(CrewBody body) => s_BodyTops[(int)body];

        public static string[] SharedLegs(CrewLegs legs) => s_Legs[(int)legs];

        public static int LegsTop(CrewLegs legs) => legs == CrewLegs.Crouch ? ArtData.LegsCrouchTop
            : legs == CrewLegs.Sit ? ArtData.LegsSitTop : ArtData.LegsTop;

        static readonly string[][] s_Bodies =
        {
            ArtData.TorsoForward, ArtData.TorsoUp, ArtData.TorsoDown, ArtData.TorsoUpDiag, ArtData.TorsoDownDiag,
            ArtData.TorsoUnarmed, ArtData.TorsoPad, ArtData.TorsoLounge,
            ArtData.Repair1, ArtData.Repair2, ArtData.Repair3, ArtData.Repair4,
            ArtData.TorsoUnarmed, ArtData.TorsoUnarmed, ArtData.TorsoUnarmed, ArtData.TorsoSlump,
            ArtData.TorsoCarry,
            ArtData.TorsoPoint, ArtData.TorsoPointUp,
        };

        static readonly int[] s_BodyTops =
        {
            ArtData.TorsoForwardTop, ArtData.TorsoUpTop, ArtData.TorsoDownTop, ArtData.TorsoUpDiagTop, ArtData.TorsoDownDiagTop,
            ArtData.TorsoForwardTop, ArtData.TorsoForwardTop, ArtData.TorsoForwardTop,
            ArtData.TorsoForwardTop, ArtData.TorsoForwardTop, ArtData.TorsoForwardTop, ArtData.TorsoForwardTop,
            ArtData.TorsoForwardTop, ArtData.TorsoForwardTop, ArtData.TorsoForwardTop, ArtData.TorsoForwardTop,
            ArtData.TorsoForwardTop,
            ArtData.TorsoForwardTop, ArtData.TorsoForwardTop,
        };

        static readonly string[][] s_Legs =
        {
            ArtData.LegsIdle, ArtData.LegsRun1, ArtData.LegsRun2, ArtData.LegsRun3, ArtData.LegsRun4,
            ArtData.LegsJump, ArtData.LegsCrouch, ArtData.LegsSit,
        };
    }

    public static partial class Art
    {
        /// <summary>
        /// The top half for <paramref name="body"/> in <paramref name="look"/>: the head first, where the frame puts it,
        /// then the body over it, outlined as one.
        /// </summary>
        static Sprite Figure(CrewLook look, CrewBody body, Dictionary<char, Color32> pal)
        {
            HeadPlace at = CrewFigure.HeadOn(body);
            var c = new PixelCanvas(24, 32);
            c.Ascii(look.Head(at.Head), pal, at.X, ArtData.HeadTop + at.Y);
            c.Ascii(look.Body(body), pal, 0, CrewArt.BodyTop(body));
            return Rimed(c);
        }

        /// <summary>A figure's legs, as <see cref="Part"/> makes them, with their frosted look.</summary>
        static Sprite RimedPart(string[] rows, int top, Dictionary<char, Color32> pal)
        {
            var c = new PixelCanvas(24, 32);
            c.Ascii(rows, pal, 0, top);
            return Rimed(c);
        }

        /// <summary>Outlines <paramref name="c"/> into a figure's sprite (flash and Trollogram copies too), with its frosted look beside it.</summary>
        static Sprite Rimed(PixelCanvas c)
        {
            c.Outline(OutlineColor);
            var pivot = new Vector2(0.5f, 0f);
            Sprite sprite = ToSprite(c, pivot, withFlash: true);
            var looks = new Sprite[Fx.FrostTwinkle.Of];
            for (int i = 0; i < looks.Length; i++)
                looks[i] = Sprite.Create(ToTexture(Rime(c, i)), new Rect(0, 0, c.W, c.H), pivot, PPU, 0, SpriteMeshType.FullRect);
            s_Rime[sprite] = looks;
            return sprite;
        }

        static readonly Dictionary<Sprite, Sprite[]> s_Rime = new Dictionary<Sprite, Sprite[]>();

        /// <summary>
        /// A crewmate's figure sprite white with frost (a cutscene's <c>frosted</c>), its sparkles where they are at
        /// <paramref name="now"/> (<see cref="Fx.FrostTwinkle"/>); null for a sprite with no frosted look. Laid over the
        /// figure, it covers it: see-through, it's a frosting.
        /// </summary>
        public static Sprite Frosted(Sprite s, float now) =>
            s != null && s_Rime.TryGetValue(s, out Sprite[] looks) ? looks[Fx.FrostTwinkle.Step(now)] : null;

        static readonly Color32 IceWhite = Hex(0xf4fbff), IceDeep = Hex(0x3f86e8), IceEdge = Hex(0x2c4c70), Sparkle = Hex(0xffffff);

        /// <summary>
        /// A figure's frosted look: in their own colours, every one washed half the way to ice, the outline gone a deep ice blue, a crust of
        /// white rime on whatever faces up (the cap, the shoulders, the arms, the boots) and dusting the sides, and a few
        /// sparkles on the rime that move on with <paramref name="twinkle"/>. Fixed pixels, so every phone frosts the same.
        /// </summary>
        static PixelCanvas Rime(PixelCanvas c, int twinkle)
        {
            var rime = new PixelCanvas(c.W, c.H);
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                {
                    Color32 p = c.Get(x, y);
                    if (p.a == 0) continue;
                    bool top = c.Get(x, y + 1).a == 0, under = !top && c.Get(x, y + 2).a == 0;
                    bool side = c.Get(x - 1, y).a == 0 || c.Get(x + 1, y).a == 0;
                    int hash = ((x * 73856093) ^ (y * 19349663)) & 0x7fffffff;
                    float lum = (0.3f * p.r + 0.59f * p.g + 0.11f * p.b) / 255f;
                    bool edge = lum < 0.15f;
                    Color32 o = edge ? IceEdge : Color32.Lerp(p, Color32.Lerp(IceDeep, IceWhite, lum), 0.55f); // half ice, shaded as it was
                    if (top) o = IceWhite;
                    else if (under && hash % 3 != 0) o = Color32.Lerp(o, IceWhite, 0.8f);
                    else if (side && hash % 3 == 0) o = Color32.Lerp(o, IceWhite, 0.6f);
                    if (!edge && !top && hash % 29 == twinkle * 9) o = Sparkle;
                    o.a = 255;
                    rime.Px[y * c.W + x] = o;
                }
            return rime;
        }

        /// <summary>Every frame of <paramref name="look"/> in <paramref name="pal"/>.</summary>
        static PlayerSkin BuildSkin(CrewLook look, Dictionary<char, Color32> pal)
        {
            var body = new Sprite[s_BodyFrames];
            for (int i = 0; i < body.Length; i++) body[i] = Figure(look, (CrewBody)i, pal);
            var legs = new Sprite[s_LegsFrames];
            for (int i = 0; i < legs.Length; i++) legs[i] = RimedPart(look.Legs((CrewLegs)i), CrewArt.LegsTop((CrewLegs)i), pal);
            return new PlayerSkin(body, legs);
        }

        static readonly int s_BodyFrames = System.Enum.GetValues(typeof(CrewBody)).Length;
        static readonly int s_LegsFrames = System.Enum.GetValues(typeof(CrewLegs)).Length;
    }
}
