// How a soldier on a stage stands (a cutscene, the den, its rooms, the bay, a den beat, the debrief): which legs and which top
// (a frame of the crew's figure, CrewFigure), where in their walk, the top's nudge, and how far they've toppled (down). Every screen poses its figures through this, and StageFigure draws it.
namespace PooketSlug
{
    /// <summary>A stage soldier's legs.</summary>
    public enum StageLegs { Idle, Walk, Crouch, Sit, Jump, Hidden }

    /// <summary>
    /// A stage soldier's top half: facing the camera (with their gun), off duty, lounging, holding a pad, or repairing; or off
    /// duty and grinning, their face falling (<see cref="CrewFigure.FaceFall"/>), or glum, slumped at its end.
    /// </summary>
    public enum StageTorso { Forward, Unarmed, Lounge, Pad, Repair, Grin, FaceFall, Glum }

    /// <summary>A stage soldier's pose: legs, top, where in the walk, and how far the top's nudged (art pixels).</summary>
    public struct StagePose
    {
        public StageLegs Legs;
        public StageTorso Torso;
        /// <summary>Seconds into the walk (only while walking).</summary>
        public float WalkT;
        /// <summary>The walk's frames a second.</summary>
        public float WalkFps;
        public float TorsoX, TorsoY;
        /// <summary>
        /// Degrees the whole figure has toppled onto its back (the crew's down, <see cref="CrewFigure.DownTilt"/>): 0 upright,
        /// 90 flat. The screen turns it about the feet the way the soldier faces, as the game turns a downed soldier
        /// (<see cref="CrewFrame.Tilt"/>: this times their facing, +1 right).
        /// </summary>
        public float Tilt;

        public StagePose(StageLegs legs, StageTorso torso, float torsoX = 0f, float torsoY = 0f)
        {
            Legs = legs;
            Torso = torso;
            WalkT = 0f;
            WalkFps = Clips.StageWalk.Fps;
            TorsoX = torsoX;
            TorsoY = torsoY;
            Tilt = 0f;
        }

        /// <summary>Standing still, facing the camera.</summary>
        public static StagePose Stand => new StagePose(StageLegs.Idle, StageTorso.Forward);

        /// <summary>Walking (or not), <paramref name="t"/> seconds in, at the den's pace unless it says otherwise.</summary>
        public static StagePose Walking(bool walking, float t, StageTorso torso = StageTorso.Unarmed, float fps = 0f)
        {
            var p = new StagePose(walking ? StageLegs.Walk : StageLegs.Idle, torso) { WalkT = t };
            if (fps > 0f) p.WalkFps = fps;
            return p;
        }

        /// <summary>Crouched over a tabk with a wrench, <paramref name="t"/> seconds into the crew's repair (<see cref="CrewFigure.Repair"/>).</summary>
        public static StagePose Repairing(float t) =>
            new StagePose(StageLegs.Crouch, StageTorso.Repair, 0f, CrewFigure.Repair.Lift(t)) { WalkT = t };

        /// <summary>
        /// On a rope or a ladder: the legs bent round it and the top reaching up, as the den's crew climbs its ladder.
        /// <paramref name="t"/> seconds in, for the hand-over-hand shiver.
        /// </summary>
        public static StagePose Climbing(float t) => new StagePose(StageLegs.Jump, StageTorso.Lounge) { WalkT = t };

        /// <summary>Grinning ear to ear, off duty: standing, or walking <paramref name="t"/> seconds in (kie at a sprint, Does it do cones?).</summary>
        public static StagePose Grinning(bool walking, float t, float fps = 0f) => Walking(walking, t, StageTorso.Grin, fps);

        /// <summary>
        /// The face falling, <paramref name="since"/> seconds since it started (<see cref="CrewFigure.FaceFall"/>): the grin, gone,
        /// glum, then slumped, and held there.
        /// </summary>
        public static StagePose FaceFalling(float since) => new StagePose(StageLegs.Idle, StageTorso.FaceFall) { WalkT = since };

        /// <summary>Glum and slumped, the hands in the pockets (the face fallen): standing, or walking slowly away.</summary>
        public static StagePose Glum(bool walking, float t, float fps = 0f) => Walking(walking, t, StageTorso.Glum, fps);

        /// <summary>
        /// Down in a heap: the crew's down frame (<see cref="CrewFigure.Down"/>: legs kicked up, the body as it was), toppled
        /// flat onto their back. No art of its own.
        /// </summary>
        public static StagePose Down => new StagePose(StageLegs.Jump, StageTorso.Forward) { Tilt = CrewFigure.DownTilt(float.PositiveInfinity) };

        /// <summary>The crew figure's body frame the top shows (repairing steps through its clip, <see cref="WalkT"/> seconds in).</summary>
        public CrewBody Body
        {
            get
            {
                switch (Torso)
                {
                    case StageTorso.Unarmed: return CrewBody.Unarmed;
                    case StageTorso.Lounge: return CrewBody.Lounge;
                    case StageTorso.Pad: return CrewBody.Pad;
                    case StageTorso.Repair: return CrewFigure.Repair.Body(WalkT, CrewBody.Forward);
                    case StageTorso.Grin: return CrewBody.Grin;
                    case StageTorso.FaceFall: return CrewFigure.FaceFall.Body(WalkT, CrewBody.Grin);
                    case StageTorso.Glum: return CrewBody.Slump;
                    default: return CrewBody.Forward;
                }
            }
        }

        /// <summary>The walk's frame, of <paramref name="frames"/>.</summary>
        public int WalkFrame(int frames) => new Clip("walk", frames, WalkFps).Frame(WalkT);

        /// <summary>
        /// A cutscene's pose by its name in the scene files: <c>run</c>, <c>crouch</c>, <c>spray</c> (the top leans into
        /// the spray), <c>hatch</c> (just their top half, in a tabk's hatch), <c>repair</c> (crouched, working a wrench),
        /// <c>down</c> (in a heap on their back, <see cref="Down"/>), <c>grin</c> (grinning ear to ear), <c>fall</c> (the face
        /// falling, from the grin, <paramref name="since"/> seconds since the pose was struck: <see cref="FaceFalling"/>),
        /// <c>glum</c> (slumped, where the fall ends); anything else stands.
        /// </summary>
        public static StagePose Cutscene(string pose, float t, float runFps, float since = 0f)
        {
            var p = new StagePose(StageLegs.Idle, StageTorso.Forward) { WalkT = t, WalkFps = runFps };
            switch (pose)
            {
                case "run": p.Legs = StageLegs.Walk; break;
                case "crouch": p.Legs = StageLegs.Crouch; break;
                case "spray": p.TorsoX = -1f; break;
                case "hatch": p.Legs = StageLegs.Hidden; break;
                case "repair": return Repairing(t);
                case "down": return Down;
                case "grin": return Grinning(false, t);
                case "fall": return FaceFalling(since);
                case "glum": return Glum(false, t);
            }
            return p;
        }
    }
}
