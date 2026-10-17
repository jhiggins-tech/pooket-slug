using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes' cast and props, staged as the shot's script has them (<see cref="ShotStage"/>): the soldiers
    /// (tones2, kie, the goon), kie's tabk, and the props (<see cref="Staging.Things"/>), each placed, turned, sized and
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
        StageFigure m_Tones2, m_Goon, m_Kie;
        RectTransform m_KieTabk, m_KieTurret;
        Image m_KieHull, m_Rotor;

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

        /// <summary>The hangout's window, sign and doors.</summary>
        public void MakeHangout()
        {
            Prop("window", 28f, 20f);
            m_Sign = Ui.Label(m_World, "sign", Vector2.zero, Vector2.zero, 20, TextAnchor.MiddleCenter, Color.white);
            m_Sign.text = "POOKET";
            Prop("doorway", 20f, 40f);
            Prop("door", 20f, 40f).rectTransform.pivot = new Vector2(0f, 0f); // it swings on its hinge, on the left
        }

        /// <summary>tones2's tabk, parked in the motor pool's first space.</summary>
        public void MakeTones2Tabk() =>
            m_Props["tones2-tabk"] = Picture("tones2-tabk", Art.Tank, 0f, 0f, Art.Tank.rect.width, Art.Tank.rect.height, Color.white);

        /// <summary>kie, kie's tabk (a hull and a turret that turns), the clamp, and the cargo drone with its cable and rotors.</summary>
        public void MakeKieAndTheDrone()
        {
            m_Kie = new StageFigure(m_World, "kie");
            m_KieTabk = Ui.Rect(m_World, "kie-tabk", Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            m_KieTurret = Ui.Rect(m_KieTabk, "Turret", new Vector2(0.5f, 0f), new Vector2(Art.TankBarrel.pivot.x / Art.TankBarrel.rect.width, 0.5f),
                new Vector2(Art.TankBarrelPivot.x, Art.TankBarrelPivot.y) * Art.PPU * Px, new Vector2(Art.TankBarrel.rect.width, Art.TankBarrel.rect.height) * Px);
            Ui.Picture(m_KieTurret, "Barrel", Art.TankBarrel, Ui.Middle, Ui.Middle, Vector2.zero, new Vector2(Art.TankBarrel.rect.width, Art.TankBarrel.rect.height) * Px, Color.white);
            m_KieHull = Ui.Picture(m_KieTabk, "Hull", Art.TankHull, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(Art.TankHull.rect.width, Art.TankHull.rect.height) * Px, Color.white);
            Prop("clamp", 30f, 6f);
            Prop("cable", 1f, 10f);
            Prop("drone", 40f, 10f);
            m_Rotor = Picture("Rotors", null, 0f, 0f, 52f, 2f, new Color(0.75f, 0.8f, 0.85f, 0.7f));
        }

        /// <summary>The cargo drone's remote and the alarm's beacon.</summary>
        public void MakeRemoteAndBeacon()
        {
            Prop("remote", 4f, 6f);
            Prop("beacon", 8f, 5f);
        }

        /// <summary>The goon, tones2, and what tones2 carries: the bag, the cup and the keycard.</summary>
        public void MakeFrontCast()
        {
            m_Goon = new StageFigure(m_World, "goon");
            m_Tones2 = new StageFigure(m_World, "tones2");
            Prop("bag", 9f, 11f);
            Prop("cup", 6f, 12f);
            Prop("card", 8f, 5f);
        }

        // ------------------------------------------------------------------ each frame

        /// <summary>A soldier on the stage, by name (tones2 for anyone else).</summary>
        public StageFigure Figure(string name)
        {
            switch (name)
            {
                case "kie": return m_Kie;
                case "goon": return m_Goon;
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
            Goon(stage, t, now, ground);
            KieTabk(stage, t, now, ground);
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
            bool on = stage.Shown("sign", t);
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

        /// <summary>kie's tabk: its hull in its colour (white, or a wreck's), its gun turned as the script has the turret.</summary>
        void KieTabk(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("kie-tabk", t);
            Ui.Show(m_KieTabk, on);
            if (!on) return;
            Vector2 at = stage.Where("kie-tabk", t, now);
            m_KieTabk.anchoredPosition = new Vector2(at.x, ground + at.y) * Px;
            m_KieHull.color = stage.Colour("kie-tabk", t, now);
            m_KieTurret.localRotation = Quaternion.Euler(0f, 0f, stage.Turn("turret", t));
        }
    }
}
