using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes' cast and props, staged as the shot's script has them (<see cref="ShotStage"/>): the soldiers
    /// (tones2, kie, kcaj, the goon), kie's tabk, and the props (<see cref="Staging.Things"/>), each placed, turned, sized and
    /// coloured, with the few that have looks of their own: the sign's words, the clamp kie rips off, the drone's rotors
    /// and cable, the alarm's beacon.
    /// <para>
    /// Its pieces are made by <see cref="CutsceneView"/>, in drawing order among the effects'; see there.
    /// </para>
    /// </summary>
    public sealed class CutsceneCast
    {
        readonly RectTransform m_World;
        // the props, by name: placed, turned, sized and coloured as the script has them
        readonly Dictionary<string, Image> m_Props = new Dictionary<string, Image>();
        Text m_Sign;
        StageFigure m_Tones2, m_Goon, m_Kie, m_Kcaj;
        RectTransform m_KieTabk, m_KieTurret, m_KcajTabk, m_KcajTurret;
        Image m_KieHull, m_KcajHull, m_Rotor;

        public CutsceneCast(RectTransform world)
        {
            m_World = world;
        }

        Image Picture(string name, Sprite sprite, float x, float y, float w, float h, Color color) => StageKit.Picture(m_World, name, sprite, x, y, w, h, color);

        /// <summary>A plain prop, made in its own colour; placed by the script each frame.</summary>
        public Image Prop(string name, float w, float h)
        {
            Image img = Picture(name, null, 0f, 0f, w, h, Staging.Things[name].Colour);
            m_Props[name] = img;
            return img;
        }

        // ------------------------------------------------------------------ made in drawing order (CutsceneView)

        /// <summary>The hangout's window, sign and doors. On Design's street plate (<see cref="CutsceneBackdrops"/>) the door is
        /// cut from the plate, so it swings off its painted frame, and the window is its lit panes, so they take the script's
        /// colour; the plate paints the sign itself, so the words stay off.</summary>
        public void MakeHangout()
        {
            Sprite glow = DenRoom.Painting("street-glow"), door = DenRoom.Painting("street-door");
            m_Painted = glow != null && door != null;
            if (m_Painted) PaintedProp("window", glow, glow.rect.width, glow.rect.height);
            else Prop("window", 28f, 20f);
            m_Sign = Ui.Label(m_World, "sign", Vector2.zero, Vector2.zero, 20, TextAnchor.MiddleCenter, Color.white);
            m_Sign.text = "POOKET";
            if (m_Painted)
            {
                Prop("doorway", door.rect.width, door.rect.height);
                PaintedProp("door", door, door.rect.width, door.rect.height);
            }
            else
            {
                Prop("doorway", 20f, 40f);
                Prop("door", 20f, 40f);
            }
            m_Props["door"].rectTransform.pivot = new Vector2(0f, 0f); // it swings on its hinge, on the left
        }

        bool m_Painted;

        /// <summary>A prop that's a picture: placed and tinted by the script like a plain one.</summary>
        void PaintedProp(string name, Sprite sprite, float w, float h) => m_Props[name] = Picture(name, sprite, 0f, 0f, w, h, Staging.Things[name].Colour);

        /// <summary>kcaj's corner of the den, his string board on the wall: the den's own layer for it (<see cref="DenRoom"/>).</summary>
        public void MakeKcajCorner()
        {
            Sprite corner = DenRoom.Painting("kcaj");
            m_Props["kcaj-corner"] = Picture("kcaj-corner", corner, 0f, 0f, corner != null ? corner.rect.width : 96f, corner != null ? corner.rect.height : 52f, Color.white);
        }

        /// <summary>
        /// The creamery's cold store: the dark of its doorway (sized open over the painted shut doors, so they seem to
        /// part) and a chest freezer in the yard.
        /// </summary>
        public void MakeColdStore()
        {
            Prop("store-opening", 100f, 86f);
            Image freezer = Prop("freezer", 44f, 24f);
            Image lid = Picture("Freezer lid", null, 0f, 0f, 1f, 1f, new Color(0.55f, 0.65f, 0.75f));
            lid.rectTransform.SetParent(freezer.rectTransform, false);
            lid.rectTransform.anchorMin = new Vector2(0f, 1f);
            lid.rectTransform.anchorMax = new Vector2(1f, 1f);
            lid.rectTransform.pivot = new Vector2(0.5f, 1f);
            lid.rectTransform.anchoredPosition = Vector2.zero;
            lid.rectTransform.sizeDelta = new Vector2(0f, 3f * Px);
        }

        /// <summary>tones2's tabk, in his orange and black, parked in the motor pool's first space.</summary>
        public void MakeTones2Tabk()
        {
            Sprite tabk = Tones2Art.TabkParked;
            m_Props["tones2-tabk"] = Picture("tones2-tabk", tabk, 0f, 0f, tabk.rect.width, tabk.rect.height, Color.white);
        }

        /// <summary>kie, kie's tabk (a hull and a turret that turns), the clamp, and the cargo drone with its cable and rotors.</summary>
        public void MakeKieAndTheDrone()
        {
            m_Kie = new StageFigure(m_World, "kie");
            m_KieTabk = Tabk("kie-tabk", out m_KieHull, out m_KieTurret);
            Prop("clamp", 30f, 6f);
            Prop("cable", 1f, 10f);
            Prop("drone", 40f, 10f);
            m_Rotor = Picture("Rotors", null, 0f, 0f, 52f, 2f, new Color(0.75f, 0.8f, 0.85f, 0.7f));
        }

        /// <summary>
        /// kcaj's tabk, the boss's as it rolls out of the cold store: the tabk, with a cone on top. Made after the front
        /// cast, so Trollogram kcaj rises from behind its hull into the hatch.
        /// </summary>
        public void MakeKcajTabk()
        {
            m_KcajTabk = Tabk("kcaj-tabk", out m_KcajHull, out m_KcajTurret);
            Sprite cone = KcajArt.Cone;
            Ui.Picture(m_KcajTabk, "Cone", cone, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(12f, 20f) * Px, new Vector2(cone.rect.width, cone.rect.height) * Px, Color.white);
        }

        /// <summary>A tabk on the stage: its hull, and a turret that turns (the script's <c>turret</c>).</summary>
        RectTransform Tabk(string name, out Image hull, out RectTransform turret)
        {
            RectTransform tabk = Ui.Rect(m_World, name, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            turret = Ui.Rect(tabk, "Turret", new Vector2(0.5f, 0f), new Vector2(Art.TankBarrel.pivot.x / Art.TankBarrel.rect.width, 0.5f),
                new Vector2(Art.TankBarrelPivot.x, Art.TankBarrelPivot.y) * Art.PPU * Px, new Vector2(Art.TankBarrel.rect.width, Art.TankBarrel.rect.height) * Px);
            Ui.Picture(turret, "Barrel", Art.TankBarrel, Ui.Middle, Ui.Middle, Vector2.zero, new Vector2(Art.TankBarrel.rect.width, Art.TankBarrel.rect.height) * Px, Color.white);
            hull = Ui.Picture(tabk, "Hull", Art.TankHull, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(Art.TankHull.rect.width, Art.TankHull.rect.height) * Px, Color.white);
            return tabk;
        }

        /// <summary>The cargo drone's remote and the alarm's beacon.</summary>
        public void MakeRemoteAndBeacon()
        {
            Prop("remote", 4f, 6f);
            Prop("beacon", 8f, 5f);
        }

        /// <summary>The goon, kcaj, tones2, and what they carry: the bag, the cup, the keycard, the tub, the slushie, the cones and the sticky note.</summary>
        public void MakeFrontCast()
        {
            m_Goon = new StageFigure(m_World, "goon");
            m_Kcaj = new StageFigure(m_World, "kcaj");
            m_Tones2 = new StageFigure(m_World, "tones2");
            Prop("bag", 9f, 11f);
            Prop("cup", 6f, 12f);
            Prop("card", 8f, 5f);
            Prop("tub", 8f, 6f);
            Prop("slushie", 5f, 11f);
            Sprite cone = KcajArt.Cone;
            PaintedProp("cones", cone, cone.rect.width, cone.rect.height);
            Prop("note", 4f, 4f);
        }

        // ------------------------------------------------------------------ each frame

        /// <summary>A soldier on the stage, by name (tones2 for anyone else).</summary>
        public StageFigure Figure(string name)
        {
            switch (name)
            {
                case "kie": return m_Kie;
                case "goon": return m_Goon;
                case "kcaj": return m_Kcaj;
                default: return m_Tones2;
            }
        }

        /// <summary>Puts the cast and props where the shot's script has them at <paramref name="t"/> (<paramref name="now"/> is the scene's clock).</summary>
        public void Draw(ShotStage stage, float t, float now, float ground)
        {
            foreach (KeyValuePair<string, Image> kv in m_Props) Place(kv.Value, kv.Key, stage, t, now, ground);
            Sign(stage, t, now, ground);
            Clamp(stage, t, now);
            Drone(stage, t, now, ground);
            Beacon(stage, t);

            Soldier(m_Tones2, "tones2", Art.SkinFor(Content.Tones2, 0), 10f, stage, t, now, ground);
            Soldier(m_Kie, "kie", Art.SkinFor(Content.Kie, 0), 8f, stage, t, now, ground);
            Soldier(m_Kcaj, "kcaj", Art.SkinFor(Content.Kcaj, 0), 9f, stage, t, now, ground);
            Goon(stage, t, now, ground);
            Tabk(m_KieTabk, m_KieHull, m_KieTurret, "kie-tabk", stage, t, now, ground);
            Tabk(m_KcajTabk, m_KcajHull, m_KcajTurret, "kcaj-tabk", stage, t, now, ground);
        }

        /// <summary>A plain prop: on stage (and lit, if it blinks), where it is, turned, sized and coloured.</summary>
        static void Place(Image img, string name, ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown(name, t) && stage.Lit(name, t, now);
            Ui.Show(img, on);
            if (!on) return;
            Vector2 at = stage.Where(name, t, now);
            RectTransform rt = img.rectTransform;
            rt.anchoredPosition = new Vector2(at.x, ground + at.y) * Px;
            rt.localRotation = Quaternion.Euler(0f, 0f, stage.Turn(name, t));
            Vector2 size = stage.Size(name, t);
            rt.localScale = new Vector3(size.x, size.y, 1f);
            img.color = stage.Colour(name, t, now);
        }

        /// <summary>The hangout's sign: words, not a picture.</summary>
        void Sign(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("sign", t) && !m_Painted;
            Ui.Show(m_Sign, on);
            if (!on) return;
            Vector2 at = stage.Where("sign", t, now);
            m_Sign.rectTransform.anchoredPosition = new Vector2(at.x, ground + at.y) * Px;
            m_Sign.color = stage.Colour("sign", t, now);
        }

        /// <summary>The Trollograms' clamp on tones2's tabk: flickering, until kie rips it off and flings it away.</summary>
        void Clamp(ShotStage stage, float t, float now)
        {
            Image clamp = m_Props["clamp"];
            if (!clamp.gameObject.activeSelf) return;
            float rip = Mathf.Max(0f, stage.Progress("clamp", "rip", t));
            if (rip >= 1f)
            {
                Ui.Show(clamp, false);
                return;
            }
            Color c = stage.Colour("clamp", t, now);
            c.a *= (int)(now * 6f) % 4 == 0 ? 0.5f : 0.9f;
            clamp.color = c;
            RectTransform rt = clamp.rectTransform;
            rt.anchoredPosition += new Vector2(-rip * 40f, Mathf.Sin(rip * Mathf.PI) * 24f) * Px;
            rt.localRotation = Quaternion.Euler(0f, 0f, rip * 220f);
        }

        /// <summary>The cargo drone's rotors, and its cable down to the top of tones2's tabk.</summary>
        void Drone(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("drone", t);
            Ui.Show(m_Rotor, on);
            if (on)
            {
                Vector2 at = stage.Where("drone", t, now);
                m_Rotor.rectTransform.anchoredPosition = new Vector2(at.x + (int)(now * 20f) % 2 * 2f, ground + at.y + 10f) * Px;
            }
            Image cable = m_Props["cable"];
            if (!cable.gameObject.activeSelf) return;
            float top = stage.Where("drone", t, now).y, bottom = stage.Where("tones2-tabk", t, now).y + 20f;
            Vector2 x = stage.Where("cable", t, now);
            cable.rectTransform.anchoredPosition = new Vector2(x.x, ground + bottom) * Px;
            cable.rectTransform.sizeDelta = new Vector2(1f, Mathf.Max(0f, top - bottom)) * Px;
        }

        /// <summary>The alarm's beacon: dark, and turning red once a second while the alarm's going.</summary>
        void Beacon(ShotStage stage, float t)
        {
            Image beacon = m_Props["beacon"];
            if (!beacon.gameObject.activeSelf || !stage.Shown("alarm", t)) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f);
            beacon.color = Color.Lerp(Staging.Things["beacon"].Colour, Staging.Colours["red"], pulse);
        }

        /// <summary>
        /// A soldier, drawn from their legs and torso as in the game: walking, standing, crouched, spraying, or just their
        /// top half in a tabk's hatch; plain, or as Trollogram kie (cyan, flickering, grinning).
        /// </summary>
        static void Soldier(StageFigure f, string name, PlayerSkin skin, float runRate, ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown(name, t);
            Ui.Show(f.Rt, on);
            if (!on) return;
            Vector2 at = stage.Where(name, t, now);
            string pose = stage.Pose(name, t);
            Ui.Show(f.Legs, pose != "hatch");
            f.Legs.sprite = pose == "run" ? skin.LegsRun[(int)(t * runRate) % skin.LegsRun.Length] : pose == "crouch" ? skin.LegsCrouch : skin.LegsIdle;
            f.Torso.sprite = skin.TorsoForward;
            f.Torso.rectTransform.anchoredPosition = pose == "spray" ? new Vector2(-1f, 0f) : Vector2.zero;
            Color tint = stage.Colour(name, t, now);
            if (stage.Look(name, t) == "trollo")
            {
                bool flick = (int)(now * 30f) % 9 == 0;
                tint = new Color(0.45f, 1f, 1f, flick ? 0.35f : 0.85f);
                if ((int)(now * 20f) % 7 == 0) at.x += 1f;
            }
            f.Legs.color = f.Torso.color = tint;
            f.Rt.localScale = new Vector3(stage.Facing(name, t), 1f, 1f);
            f.At(at.x, ground + at.y);
        }

        /// <summary>
        /// The opening's straggler: a Trollogram that flickers up (its <c>appear</c>), and dies the goons' own death (its
        /// <c>glitch-out</c>): a flash, tearing, then squashed to a line and gone.
        /// </summary>
        void Goon(ShotStage stage, float t, float now, float ground)
        {
            float died = stage.Since("goon", "glitch-out", t);
            bool on = stage.Shown("goon", t) && died <= 0.6f;
            Ui.Show(m_Goon.Rt, on);
            if (!on) return;
            m_Goon.Legs.sprite = Art.Holo(Art.SoldierLegsIdle);
            m_Goon.Torso.sprite = Art.Holo(Art.SoldierTorso);
            Ui.Show(m_Goon.Legs, true);
            Vector2 at = stage.Where("goon", t, now);
            float rise = stage.Progress("goon", "appear", t);
            if (rise < 0f) rise = 1f;
            float jitter = (int)(now * 20f) % 7 == 0 ? 1f : 0f;
            Color tint = new Color(0.45f, 1f, 1f, rise < 1f ? ((int)(now * 30f) % 2 == 0 ? rise : 0.2f) : 0.9f);
            float facing = stage.Facing("goon", t);
            Vector3 scale = new Vector3(facing, 1f, 1f);
            if (died >= 0f)
            {
                tint = died < 0.08f ? Color.white : Color.Lerp(Cyan, new Color(1f, 0.4f, 0.9f), (int)(now * 30f) % 2);
                jitter = Mathf.Sin(now * 90f) * died * 14f;
                scale = new Vector3(facing * (1f + died * 0.6f), Mathf.Max(0.04f, 1f - died * 1.6f), 1f);
                tint.a = 1f - Mathf.Clamp01((died - 0.4f) / 0.2f);
            }
            m_Goon.Legs.color = m_Goon.Torso.color = tint;
            m_Goon.Rt.localScale = scale;
            m_Goon.At(at.x + jitter, ground + at.y);
        }

        /// <summary>A tabk (kie's or kcaj's): its hull in its colour (white, or a wreck's), its gun turned as the script has the turret.</summary>
        static void Tabk(RectTransform tabk, Image hull, RectTransform turret, string name, ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown(name, t);
            Ui.Show(tabk, on);
            if (!on) return;
            Vector2 at = stage.Where(name, t, now);
            tabk.anchoredPosition = new Vector2(at.x, ground + at.y) * Px;
            hull.color = stage.Colour(name, t, now);
            turret.localRotation = Quaternion.Euler(0f, 0f, stage.Turn("turret", t));
        }
    }
}
