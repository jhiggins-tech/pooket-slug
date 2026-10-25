// The playable crew's figure (tones2, kie, kcaj and anyone freed later): one shared set of named clips, and the layers
// each frame is built from. Every crewmate stands on the same legs and the same body, in their own colours, with a head
// of their own on top that follows the body's frames; a crewmate can swap in a body or legs part of their own where
// their art calls for it (the art side: CrewArt). A move is written here once, so a new crewmate inherits all of them.
// Goons, pilots' own brains and bosses keep their own drawing.
using UnityEngine;

namespace PooketSlug
{
    /// <summary>The crew's named moves: what a figure is doing. Shooting and getting hurt ride over any of them (<see cref="CrewFrame"/>).</summary>
    /// <see cref="FaceFall"/> is the stage's only: a soldier in play never plays it.
    public enum CrewMove { Idle, Run, Jump, Crouch, Down, Repair, FaceFall }

    /// <summary>The shared legs' frames.</summary>
    public enum CrewLegs { Idle, Run1, Run2, Run3, Run4, Jump, Crouch, Sit }

    /// <summary>
    /// The shared body's frames: aiming (forward, up, down, at 45°), off duty in the den, the shared moves' (repairing;
    /// a face falling: grinning, the grin gone, glum, then slumped), and carrying something with no gun (a cutscene's
    /// <c>arms out</c>). New frames go on the end: a skin's sprites are indexed by the value.
    /// </summary>
    public enum CrewBody { Forward, Up, Down, UpDiag, DownDiag, Unarmed, Pad, Lounge, Repair1, Repair2, Repair3, Repair4, Grin, Blank, Glum, Slump, Carry }

    /// <summary>
    /// A crewmate's own head frames: facing forward, looking down at their work, and a face falling (grinning, the grin
    /// gone, glum). A crewmate who hasn't drawn a frame shows their forward one.
    /// </summary>
    public enum CrewHead { Front, Down, Grin, Blank, Glum }

    /// <summary>Where a body frame wants the head: which of the head's frames, and nudged how far from its usual spot (art pixels, right and down).</summary>
    public readonly struct HeadPlace
    {
        public readonly CrewHead Head;
        public readonly int X, Y;

        public HeadPlace(CrewHead head, int x = 0, int y = 0)
        {
            Head = head;
            X = x;
            Y = y;
        }
    }

    /// <summary>One named clip of the crew's: its timing, and the legs and body it steps through (null body: the aim picks it).</summary>
    public sealed class CrewClip
    {
        public readonly CrewMove Move;
        public readonly Clip Clip;
        readonly CrewLegs[] m_Legs;
        readonly CrewBody[] m_Body;
        readonly int[] m_Lift;

        public CrewClip(CrewMove move, Clip clip, CrewLegs[] legs, CrewBody[] body = null, int[] lift = null)
        {
            Move = move;
            Clip = clip;
            m_Legs = legs;
            m_Body = body;
            m_Lift = lift;
        }

        /// <summary>The legs <paramref name="t"/> seconds in.</summary>
        public CrewLegs Legs(float t) => Clip.Pick(m_Legs, t);

        /// <summary>The body <paramref name="t"/> seconds in, or <paramref name="aimed"/> for a move that shoots.</summary>
        public CrewBody Body(float t, CrewBody aimed) => m_Body == null ? aimed : Clip.Pick(m_Body, t);

        /// <summary>Does the body follow the aim (a move you can shoot in)?</summary>
        public bool Aims => m_Body == null;

        /// <summary>How far the body sits above its usual spot <paramref name="t"/> seconds in (art pixels; the run's bob).</summary>
        public int Lift(float t) => m_Lift == null ? 0 : Clip.Pick(m_Lift, t);
    }

    /// <summary>What a crewmate's figure shows this frame: the move, its legs and body, the body's lift, and what rides over them.</summary>
    public struct CrewFrame
    {
        public CrewMove Move;
        public CrewLegs Legs;
        public CrewBody Body;
        /// <summary>Art pixels the body (and the head on it) sits above its usual spot.</summary>
        public int Lift;
        /// <summary>Shooting: the muzzle flash at the gun.</summary>
        public bool Muzzle;
        /// <summary>Hurt: drawn white.</summary>
        public bool Flash;
        /// <summary>Not drawn this frame: blinking while it can't be hurt, flickering out when down.</summary>
        public bool Hidden;
        /// <summary>Degrees the figure has toppled (down), the way it faces.</summary>
        public float Tilt;
    }

    /// <summary>The crew's clips, and which plays for a soldier's pose (<see cref="AvatarPose"/>).</summary>
    public static class CrewFigure
    {
        /// <summary>How far the body drops onto crouching legs (art pixels).</summary>
        public const int CrouchDrop = 6;

        public static readonly CrewClip Idle = new CrewClip(CrewMove.Idle, new Clip("idle", 1, 1f), new[] { CrewLegs.Idle });
        /// <summary>Running: the legs' four frames, the body bobbing down a pixel on the first and third.</summary>
        public static readonly CrewClip Run = new CrewClip(CrewMove.Run, Clips.SoldierRun,
            new[] { CrewLegs.Run1, CrewLegs.Run2, CrewLegs.Run3, CrewLegs.Run4 }, lift: new[] { -1, 0, -1, 0 });
        public static readonly CrewClip Jump = new CrewClip(CrewMove.Jump, new Clip("jump", 1, 1f, loop: false), new[] { CrewLegs.Jump });
        public static readonly CrewClip Crouch = new CrewClip(CrewMove.Crouch, new Clip("crouch", 1, 1f, loop: false),
            new[] { CrewLegs.Crouch }, lift: new[] { -CrouchDrop });
        /// <summary>Down: legs kicked up, toppling over (<see cref="DownTilt"/>), then flickering out.</summary>
        public static readonly CrewClip Down = new CrewClip(CrewMove.Down, new Clip("down", 1, 1f, loop: false), new[] { CrewLegs.Jump });
        /// <summary>Repairing a tabk: crouched, a wrench going up and down, the head bowed over the work.</summary>
        public static readonly CrewClip Repair = new CrewClip(CrewMove.Repair, new Clip("repair", 4, 8f),
            new[] { CrewLegs.Crouch }, new[] { CrewBody.Repair1, CrewBody.Repair2, CrewBody.Repair3, CrewBody.Repair4 },
            new[] { -CrouchDrop });

        /// <summary>
        /// A face falling, on the stage only (Does it do cones?: kie's grin falls, slowly, all the way down): standing, off
        /// duty, grinning, then the grin gone, then glum, then slumped with the head bowed, held there.
        /// </summary>
        public static readonly CrewClip FaceFall = new CrewClip(CrewMove.FaceFall, new Clip("face-fall", 4, 1.5f, loop: false),
            new[] { CrewLegs.Idle }, new[] { CrewBody.Grin, CrewBody.Blank, CrewBody.Glum, CrewBody.Slump });

        /// <summary>Every clip, by its move.</summary>
        public static readonly CrewClip[] All = { Idle, Run, Jump, Crouch, Down, Repair, FaceFall };

        /// <summary>The clip for <paramref name="move"/>.</summary>
        public static CrewClip Of(CrewMove move) => All[(int)move];

        /// <summary>Where each body frame wants the head (in <see cref="CrewBody"/>'s order): repairing bows it; a face falling
        /// shows its face, and slumped sits it a row lower, on the lower shoulders.</summary>
        static readonly HeadPlace[] s_Heads =
        {
            new HeadPlace(CrewHead.Front), new HeadPlace(CrewHead.Front), new HeadPlace(CrewHead.Front),
            new HeadPlace(CrewHead.Front), new HeadPlace(CrewHead.Front), new HeadPlace(CrewHead.Front),
            new HeadPlace(CrewHead.Front), new HeadPlace(CrewHead.Front),
            new HeadPlace(CrewHead.Down, 1, 1), new HeadPlace(CrewHead.Down, 1, 1),
            new HeadPlace(CrewHead.Down, 1, 2), new HeadPlace(CrewHead.Down, 1, 1),
            new HeadPlace(CrewHead.Grin), new HeadPlace(CrewHead.Blank), new HeadPlace(CrewHead.Glum), new HeadPlace(CrewHead.Glum, 0, 1),
            new HeadPlace(CrewHead.Front),
        };

        /// <summary>Where <paramref name="body"/> wants the head.</summary>
        public static HeadPlace HeadOn(CrewBody body) => s_Heads[(int)body];

        /// <summary>The body for an aim: forward, straight up or down, or at 45°.</summary>
        public static CrewBody Aimed(int aimY, bool diagonal) =>
            aimY > 0 ? (diagonal ? CrewBody.UpDiag : CrewBody.Up)
            : aimY < 0 ? (diagonal ? CrewBody.DownDiag : CrewBody.Down)
            : CrewBody.Forward;

        /// <summary>The move a soldier's pose plays: down, in the air, crouching, running or standing.</summary>
        public static CrewMove MoveOf(in AvatarPose p) =>
            p.Dead ? CrewMove.Down : !p.Grounded ? CrewMove.Jump : p.Crouching ? CrewMove.Crouch
            : p.Running ? CrewMove.Run : CrewMove.Idle;

        /// <summary>How far a figure that went down <paramref name="downFor"/> seconds ago has toppled (degrees).</summary>
        public static float DownTilt(float downFor) => Mathf.Min(90f, downFor * 300f);

        /// <summary>Seconds down before the figure starts flickering out.</summary>
        public const float FlickerAfter = 1.2f;

        /// <summary>
        /// What a soldier's figure shows for <paramref name="p"/>, at <paramref name="now"/> (the clock its hurt blink
        /// keeps). The body bobs with the run whenever it's running, in the air too, and crouching drops it.
        /// </summary>
        public static CrewFrame Frame(in AvatarPose p, float now)
        {
            CrewClip clip = Of(MoveOf(p));
            CrewBody aimed = Aimed(p.AimY, p.Diagonal);
            int lift = p.Crouching ? Crouch.Lift(0f) : p.Running ? Run.Lift(p.Anim) : 0;
            return new CrewFrame
            {
                Move = clip.Move,
                Legs = clip.Legs(p.Anim),
                Body = clip.Body(p.Anim, aimed),
                Lift = lift,
                Muzzle = p.Muzzle && !p.Dead,
                Flash = p.Flash,
                Hidden = (p.Blink && Fx.HurtBlink.On(now)) ||
                         (p.Dead && p.DeadTime > FlickerAfter && Fx.DeathFlicker.On(p.DeadTime)) || (p.Dead && p.Gone),
                Tilt = p.Dead ? DownTilt(p.DeadTime) * p.Facing : 0f,
            };
        }
    }
}
