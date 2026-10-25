// How a soldier on a stage stands (a cutscene, the den, its rooms, the bay, a den beat, the debrief): which legs and which top
// (a frame of the crew's figure, CrewFigure), where in their walk, and the top's nudge. Every screen poses its figures through this, and StageFigure draws it.
namespace PooketSlug
{
    /// <summary>A stage soldier's legs.</summary>
    public enum StageLegs { Idle, Walk, Crouch, Sit, Jump, Hidden }

    /// <summary>A stage soldier's top half: facing the camera (with their gun), off duty, lounging, holding a pad, or repairing.</summary>
    public enum StageTorso { Forward, Unarmed, Lounge, Pad, Repair }

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

        public StagePose(StageLegs legs, StageTorso torso, float torsoX = 0f, float torsoY = 0f)
        {
            Legs = legs;
            Torso = torso;
            WalkT = 0f;
            WalkFps = Clips.StageWalk.Fps;
            TorsoX = torsoX;
            TorsoY = torsoY;
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
                    default: return CrewBody.Forward;
                }
            }
        }

        /// <summary>The walk's frame, of <paramref name="frames"/>.</summary>
        public int WalkFrame(int frames) => new Clip("walk", frames, WalkFps).Frame(WalkT);

        /// <summary>
        /// A cutscene's pose by its name in the scene files: <c>run</c>, <c>crouch</c>, <c>spray</c> (the top leans into
        /// the spray), <c>hatch</c> (just their top half, in a tabk's hatch), <c>repair</c> (crouched, working a wrench);
        /// anything else stands.
        /// </summary>
        public static StagePose Cutscene(string pose, float t, float runFps)
        {
            var p = new StagePose(StageLegs.Idle, StageTorso.Forward) { WalkT = t, WalkFps = runFps };
            switch (pose)
            {
                case "run": p.Legs = StageLegs.Walk; break;
                case "crouch": p.Legs = StageLegs.Crouch; break;
                case "spray": p.TorsoX = -1f; break;
                case "hatch": p.Legs = StageLegs.Hidden; break;
                case "repair": return Repairing(t);
            }
            return p;
        }
    }
}
