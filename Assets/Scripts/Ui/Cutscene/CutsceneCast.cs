using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes' cast and props, staged as the shot's script has them (<see cref="ShotStage"/>): the soldiers
    /// (tones2, kie, kcaj, the goon: drawn by <see cref="CutsceneSoldiers"/>), the tabks (tones2's, kie's and kcaj's), and the props (<see cref="Staging.Things"/>), each placed, turned, sized and
    /// coloured, with the few that have looks of their own: the sign's words, the clamp kie rips off, the stack of tubs that rattles, the cargo drone (the
    /// game's own, its rotors turning) and its cable, the alarm's beacon.
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
        Image m_KieHull, m_KieGun, m_KcajHull, m_KcajGun;

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
        /// part), a chest freezer in the yard and the stack of tubs beside the doors (<see cref="Art.TubStack"/>). Made
        /// before kcaj's tabk, so it rolls out in front of them.
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
            Sprite tubs = Art.TubStack[0];
            PaintedProp("tubs", tubs, tubs.rect.width, tubs.rect.height);
        }

        /// <summary>tones2's tabk, in his orange and black, parked in the motor pool's first space.</summary>
        public void MakeTones2Tabk()
        {
            Sprite tabk = Tones2Art.TabkParked;
            m_Props["tones2-tabk"] = Picture("tones2-tabk", tabk, 0f, 0f, tabk.rect.width, tabk.rect.height, Color.white);
        }

        /// <summary>kie, kie's tabk (a hull and a turret that turns), the clamp, and the cargo drone (drawn as the game draws it, <see cref="Tones2Art.TabkDrone"/>) with its cable.</summary>
        public void MakeKieAndTheDrone()
        {
            m_Kie = new StageFigure(m_World, "kie");
            m_KieTabk = Tabk("kie-tabk", TabkKit.Kie, out m_KieHull, out m_KieGun, out m_KieTurret);
            Prop("clamp", 30f, 6f);
            Prop("cable", 1f, 10f);
            Sprite drone = Tones2Art.TabkDrone[0];
            PaintedProp("drone", drone, drone.rect.width, drone.rect.height);
        }

        /// <summary>
        /// kcaj's tabk, SOFT SERVE, the boss's as it rolls out of the cold store. Made after the front cast, so Trollogram
        /// kcaj rises from behind its hull into the hatch.
        /// </summary>
        public void MakeKcajTabk()
        {
            m_KcajTabk = Tabk("kcaj-tabk", TabkKit.Kcaj, out m_KcajHull, out m_KcajGun, out m_KcajTurret);
        }

        /// <summary>
        /// A crewmate's tabk on the stage, drawn as it is in the game (<paramref name="kit"/>): its hull, and its gun on a
        /// turret that turns (the script's <c>turret</c>).
        /// </summary>
        RectTransform Tabk(string name, TabkKit kit, out Image hull, out Image gun, out RectTransform turret)
        {
            Sprite barrel = kit.Barrel, body = kit.Hull;
            RectTransform tabk = Ui.Rect(m_World, name, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            turret = Ui.Rect(tabk, "Turret", new Vector2(0.5f, 0f), new Vector2(barrel.pivot.x / barrel.rect.width, 0.5f),
                kit.Pivot * Px, new Vector2(barrel.rect.width, barrel.rect.height) * Px);
            gun = Ui.Picture(turret, "Gun", barrel, Ui.Middle, Ui.Middle, Vector2.zero, new Vector2(barrel.rect.width, barrel.rect.height) * Px, Color.white);
            hull = Ui.Picture(tabk, "Hull", body, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(body.rect.width, body.rect.height) * Px, Color.white);
            return tabk;
        }

        /// <summary>The cargo drone's remote and the alarm's beacon.</summary>
        public void MakeRemoteAndBeacon()
        {
            Prop("remote", 4f, 6f);
            Prop("beacon", 8f, 5f);
        }

        /// <summary>The goon, kcaj, tones2, and what they carry: the bag (the crisp packet: <see cref="CutsceneSnacks"/>), the cup, the keycard, the tub, the slushie, the cones, the sticky note, and SOFT SERVE's two soft-serve cones.</summary>
        public void MakeFrontCast()
        {
            m_Goon = new StageFigure(m_World, "goon");
            m_Kcaj = new StageFigure(m_World, "kcaj");
            m_Tones2 = new StageFigure(m_World, "tones2");
            Sprite packet = CutsceneSnacks.Packet;
            PaintedProp("bag", packet, packet.rect.width, packet.rect.height); // tones2's crisp packet, hand-drawn
            Sprite slushie = CutsceneSnacks.Slushie; // his blue raspberry slushie, hand-drawn
            PaintedProp("cup", slushie, slushie.rect.width, slushie.rect.height);
            Prop("card", 8f, 5f);
            Prop("tub", 8f, 6f);
            PaintedProp("slushie", slushie, slushie.rect.width, slushie.rect.height);
            Sprite cone = KcajArt.Cone;
            PaintedProp("cones", cone, cone.rect.width, cone.rect.height);
            Prop("note", 4f, 4f);
            Sprite serve = CutsceneSnacks.SoftServe;
            PaintedProp("softserve", serve, serve.rect.width, serve.rect.height);
            PaintedProp("softserve2", serve, serve.rect.width, serve.rect.height);
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
            Crush(stage, t);
            Rattle(stage, t, now);
            TurnRound(stage, t);
            Squash("softserve", stage, t);
            Squash("softserve2", stage, t);
            Drone(stage, t, now, ground);
            Beacon(stage, t);

            CutsceneSoldiers.Soldier(m_Tones2, "tones2", Art.SkinFor(Content.Tones2), 10f, stage, t, now, ground);
            CutsceneSoldiers.Soldier(m_Kie, "kie", Art.SkinFor(Content.Kie), 8f, stage, t, now, ground);
            CutsceneSoldiers.Soldier(m_Kcaj, "kcaj", Art.SkinFor(Content.Kcaj), 9f, stage, t, now, ground);
            CutsceneSoldiers.Goon(m_Goon, stage, t, now, ground);
            Tabk(m_KieTabk, m_KieHull, m_KieGun, m_KieTurret, "kie-tabk", stage, t, now, ground);
            Tabk(m_KcajTabk, m_KcajHull, m_KcajGun, m_KcajTurret, "kcaj-tabk", stage, t, now, ground);
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

        /// <summary>The cup squashed by the script (<c>cup size 1 0.5</c>): drawn crushed instead, full size.</summary>
        void Crush(ShotStage stage, float t)
        {
            if (!m_Props.TryGetValue("cup", out Image cup) || !cup.gameObject.activeSelf) return;
            bool crushed = stage.Size("cup", t).y < 0.99f;
            cup.sprite = crushed ? CutsceneSnacks.Crushed : CutsceneSnacks.Slushie;
            if (crushed) cup.rectTransform.localScale = Vector3.one;
        }

        /// <summary>The tub stack rattling (<c>tubs rattle</c>): its top tub hops on the beat <see cref="ShotStage.Rattle"/> keeps.</summary>
        void Rattle(ShotStage stage, float t, float now)
        {
            if (m_Props.TryGetValue("tubs", out Image tubs) && tubs.gameObject.activeSelf) tubs.sprite = Art.TubStack[stage.Rattle("tubs", t, now)];
        }

        /// <summary>
        /// The packet turned round (<c>bag turn-round</c>, over a window): its front narrowing to its edge, then its back, the
        /// shop's look-alike stripes, widening out, and staying round (<see cref="SnackShow"/>).
        /// </summary>
        void TurnRound(ShotStage stage, float t)
        {
            Image bag = m_Props["bag"];
            if (!bag.gameObject.activeSelf) return;
            float k = stage.Progress("bag", "turn-round", t);
            bag.sprite = k >= 0f && SnackShow.LabelOut(k) ? CutsceneSnacks.PacketBack : CutsceneSnacks.Packet;
            if (k <= 0f || k >= 1f) return;
            Vector3 scale = bag.rectTransform.localScale;
            scale.x *= SnackShow.BagWidth(k);
            bag.rectTransform.localScale = scale;
        }

        /// <summary>A soft-serve cone, whole until it's splatted (<c>squash</c>), then squashed flat and dripping, the same size.</summary>
        void Squash(string name, ShotStage stage, float t)
        {
            Image cone = m_Props[name];
            if (cone.gameObject.activeSelf) cone.sprite = stage.Since(name, "squash", t) >= 0f ? CutsceneSnacks.Squashed : CutsceneSnacks.SoftServe;
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
            c.a *= Fx.Step(now, 6f, 4) == 0 ? 0.5f : 0.9f;
            clamp.color = c;
            RectTransform rt = clamp.rectTransform;
            rt.anchoredPosition += new Vector2(-rip * 40f, Mathf.Sin(rip * Mathf.PI) * 24f) * Px;
            rt.localRotation = Quaternion.Euler(0f, 0f, rip * 220f);
        }

        /// <summary>The cargo drone's rotors turning (its two frames, as in the game), and its cable down to the top of tones2's tabk.</summary>
        void Drone(ShotStage stage, float t, float now, float ground)
        {
            Image drone = m_Props["drone"];
            if (drone.gameObject.activeSelf) drone.sprite = Tones2Art.TabkDrone[Fx.Rotor.Step(now)];
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
        /// A tabk (kie's or kcaj's): its hull and gun in its colour (white, or a wreck's), its gun turned as the script has
        /// the turret, and kept the right way up when it swings round to point right (the art points left).
        /// </summary>
        static void Tabk(RectTransform tabk, Image hull, Image gun, RectTransform turret, string name, ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown(name, t);
            Ui.Show(tabk, on);
            if (!on) return;
            Vector2 at = stage.Where(name, t, now);
            tabk.anchoredPosition = new Vector2(at.x, ground + at.y) * Px;
            hull.color = gun.color = stage.Colour(name, t, now);
            float turn = stage.Turn("turret", t);
            turret.localRotation = Quaternion.Euler(0f, 0f, turn);
            turret.localScale = new Vector3(1f, Mathf.Cos(turn * Mathf.Deg2Rad) < 0f ? -1f : 1f, 1f);
        }
    }
}
