using UnityEngine;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes' soldiers (tones2, kie, kcaj and the opening's goon), drawn as the shot's script has them
    /// (<see cref="ShotStage"/>): posed, tinted, turned to face the way they're told, and stood with their feet where the
    /// scene puts them. A scene's places are in the plates' pixels, so a soldier stands by <see cref="StageFigure.AtPlate"/>:
    /// they're drawn a fifth again bigger (<see cref="StageKit.FigureScale"/>), but not moved. Split out of
    /// <see cref="CutsceneCast"/>, which makes the figures and calls these each frame.
    /// </summary>
    static class CutsceneSoldiers
    {
        /// <summary>
        /// A soldier, drawn from their legs and torso as in the game: walking, standing, crouched, spraying, or just their
        /// top half in a tabk's hatch; plain, or as a Trollogram crewmate (Trollogram kie or kcaj: cyan and flickering).
        /// A hidden one is still stood in place, for their bubble.
        /// </summary>
        public static void Soldier(StageFigure f, string name, PlayerSkin skin, float runRate, ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown(name, t);
            Ui.Show(f.Rt, on);
            Vector2 at = stage.Where(name, t, now);
            // stood where the shot puts them even while hidden: an off-screen speaker's bubble points to them (the
            // keycard's and the string board's close-ups), not to wherever the shot before left them
            f.AtPlate(at.x, ground + at.y);
            if (!on) return;
            StagePose pose = StagePose.Cutscene(stage.Pose(name, t), t, runRate, Mathf.Max(0f, stage.Since(name, "pose", t)));
            f.Pose(skin, pose);
            Color tint = stage.Colour(name, t, now);
            if (stage.Look(name, t) == "trollo")
            {
                bool flick = Fx.TrolloFlicker.On(now);
                tint = new Color(0.45f, 1f, 1f, flick ? 0.35f : 0.85f);
                if (Fx.TrolloJitter.On(now)) at.x += 1f;
            }
            f.Tint(tint);
            f.Rt.localScale = new Vector3(stage.Facing(name, t), 1f, 1f);
            f.Rt.localRotation = Quaternion.Euler(0f, 0f, pose.Tilt * stage.Facing(name, t)); // down: on their back, as the game topples a soldier
            f.AtPlate(at.x, ground + at.y); // where the scene puts them, in the plate's pixels; only the figure is drawn bigger
        }

        /// <summary>
        /// The opening's straggler: a Trollogram that flickers up (its <c>appear</c>), and dies the goons' own death (its
        /// <c>glitch-out</c>): a flash, tearing, then squashed to a line and gone.
        /// </summary>
        public static void Goon(StageFigure goon, ShotStage stage, float t, float now, float ground)
        {
            float died = stage.Since("goon", "glitch-out", t);
            bool on = stage.Shown("goon", t) && died <= 0.6f;
            Ui.Show(goon.Rt, on);
            if (!on) return;
            goon.Legs.sprite = Art.Holo(Art.SoldierLegsIdle);
            goon.Torso.sprite = Art.Holo(Art.SoldierTorso);
            Ui.Show(goon.Legs, true);
            Vector2 at = stage.Where("goon", t, now);
            float rise = stage.Progress("goon", "appear", t);
            if (rise < 0f) rise = 1f;
            float jitter = Fx.TrolloJitter.On(now) ? 1f : 0f;
            Color tint = new Color(0.45f, 1f, 1f, rise < 1f ? (Fx.TrolloFizz.On(now) ? rise : 0.2f) : 0.9f);
            float facing = stage.Facing("goon", t);
            Vector3 scale = new Vector3(facing, 1f, 1f);
            if (died >= 0f)
            {
                tint = died < 0.08f ? Color.white : Color.Lerp(Cyan, new Color(1f, 0.4f, 0.9f), Fx.TrolloFizz.Step(now));
                jitter = Mathf.Sin(now * 90f) * died * 14f;
                scale = new Vector3(facing * (1f + died * 0.6f), Mathf.Max(0.04f, 1f - died * 1.6f), 1f);
                tint.a = 1f - Mathf.Clamp01((died - 0.4f) / 0.2f);
            }
            goon.Tint(tint);
            goon.Rt.localScale = scale;
            goon.AtPlate(at.x + jitter, ground + at.y);
        }
    }
}
