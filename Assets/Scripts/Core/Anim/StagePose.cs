// How a soldier on a stage stands (a cutscene, the den, the bay, a den beat, the debrief): which legs and which top,
// where in their walk, and the top's nudge. Every screen poses its figures through this, and StageFigure draws it.
namespace PooketSlug
{
    /// <summary>A stage soldier's legs.</summary>
    public enum StageLegs { Idle, Walk, Crouch, Sit, Jump, Hidden }

    /// <summary>A stage soldier's top half: facing the camera (with their gun), off duty, lounging, or holding a pad.</summary>
    public enum StageTorso { Forward, Unarmed, Lounge, Pad }

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

        /// <summary>The walk's frame, of <paramref name="frames"/>.</summary>
        public int WalkFrame(int frames) => new Clip("walk", frames, WalkFps).Frame(WalkT);

        /// <summary>
        /// A cutscene's pose by its name in the scene files: <c>run</c>, <c>crouch</c>, <c>spray</c> (the top leans into
        /// the spray), <c>hatch</c> (just their top half, in a tabk's hatch); anything else stands.
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
            }
            return p;
        }
    }
}
