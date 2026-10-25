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
            { Content.Kie, HeadOf(ArtData.KieHead, ArtData.KieHeadDown) },
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
        };

        static readonly int[] s_BodyTops =
        {
            ArtData.TorsoForwardTop, ArtData.TorsoUpTop, ArtData.TorsoDownTop, ArtData.TorsoUpDiagTop, ArtData.TorsoDownDiagTop,
            ArtData.TorsoForwardTop, ArtData.TorsoForwardTop, ArtData.TorsoForwardTop,
            ArtData.TorsoForwardTop, ArtData.TorsoForwardTop, ArtData.TorsoForwardTop, ArtData.TorsoForwardTop,
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
            c.Outline(OutlineColor);
            return ToSprite(c, new Vector2(0.5f, 0f), withFlash: true);
        }

        /// <summary>Every frame of <paramref name="look"/> in <paramref name="pal"/>.</summary>
        static PlayerSkin BuildSkin(CrewLook look, Dictionary<char, Color32> pal)
        {
            var body = new Sprite[s_BodyFrames];
            for (int i = 0; i < body.Length; i++) body[i] = Figure(look, (CrewBody)i, pal);
            var legs = new Sprite[s_LegsFrames];
            for (int i = 0; i < legs.Length; i++) legs[i] = Part(look.Legs((CrewLegs)i), CrewArt.LegsTop((CrewLegs)i), pal);
            return new PlayerSkin(body, legs);
        }

        static readonly int s_BodyFrames = System.Enum.GetValues(typeof(CrewBody)).Length;
        static readonly int s_LegsFrames = System.Enum.GetValues(typeof(CrewLegs)).Length;
    }
}
