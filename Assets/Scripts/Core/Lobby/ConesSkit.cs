using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// Does it do cones? (18 s, the tabk bay; <c>docs/design/cutscenes/skits.md</c>): straight on from kcaj's tabk coming
    /// home (<see cref="KcajHomecoming"/>), SOFT SERVE still on the repair stands. kcaj leans back on it polishing its nozzle;
    /// tones2 walks up and eyes the swirl on top and asks; kie sprints in from the den's door, grinning, and skids to a
    /// stop between them; kcaj swings the nozzle round onto tones2 and it pipes two soft-serve cones that splat on him, one
    /// in each hand; kie's face falls, and he walks back out the way he came, glum, while tones2, dripping, licks a cone.
    /// Then kcaj heads back to the den after kie and tones2 wanders off down the bay, a cone in each hand, and the cargo drone,
    /// out of shot since it set the wreck down, drops back to its place over the stands for the bay's own moves.
    /// Places are in the bay's pixels, the camera at the den's door, as the homecoming's are.
    /// </summary>
    public sealed class ConesSkit : Skit
    {
        public const string Id = "skit-cones";
        public const float Shot2 = 4f, Shot3 = 8.5f, Shot4 = 12.5f, Seconds = 20f;

        /// <summary>Where kcaj leans (the homecoming leaves him there), and where tones2 walks up from (where it had him) to, behind the stands.</summary>
        public const float KcajX = KcajHomecoming.KcajNozzle, Tones2From = KcajHomecoming.Tones2X, Tones2Up = 150f;
        /// <summary>Shot 1: tones2 walking up, then eyeing the swirl.</summary>
        public const float WalkFrom = 0.2f, WalkTo = 1f;

        /// <summary>Shot 2: the roller door, kie's sprint in from it, and his skid to a stop between them.</summary>
        public const float DoorAt = Shot2, RunFrom = 4.5f, SkidFrom = 5.4f, SkidTo = 5.8f, KieFrom = -14f, KieSkid = 104f, KieStop = 112f;
        /// <summary>kie's sprint: his walk's frames a second (the stage's walk is 10).</summary>
        public const float SprintFps = 16f, GlumFps = 6f;

        /// <summary>
        /// Shot 3: the nozzle swung round over the top onto tones2, the swirl wobbling and the machine's whirr, and the two
        /// cones from the nozzle to his hands, one a moment after the other.
        /// </summary>
        public const float SwingFrom = Shot3 + 0.1f, SwingTo = Shot3 + 0.8f, WobbleFrom = Shot3 + 0.8f, WobbleTo = Shot3 + 2f,
            FirstFrom = Shot3 + 1f, FirstAt = Shot3 + 1.6f, SecondFrom = Shot3 + 1.2f, SecondAt = Shot3 + 1.8f;
        /// <summary>How high a cone arcs over the line from the nozzle to the hand.</summary>
        public const float Arc = 14f;

        /// <summary>Shot 4: kie looking from the cones to tones2 and back as his face falls, then away out the door; tones2's lick.</summary>
        public const float FallFrom = Shot4 + 0.1f, LookTones2 = Shot4 + 0.5f, LookBack = Shot4 + 1f, LookedAt = Shot4 + 1.5f,
            LeaveFrom = Shot4 + 2.7f, LeaveTo = Shot4 + 5.4f, LickFrom = Shot4 + 3.5f, LickTo = Shot4 + 4.1f;

        /// <summary>
        /// The end: kcaj off the tabk and back to the den's door after kie, and tones2 off the other way, past the screen's
        /// right edge (the bay's pixels), so the bay's empty of them before it's yours to walk.
        /// </summary>
        public const float ExitFrom = Shot4 + 5.1f, ExitTo = Seconds - 0.1f, KcajOut = KieFrom, Tones2Out = KcajHomecoming.StageWidth + 16f;

        /// <summary>
        /// The cargo drone: up out of shot over the stands at the start (unless it's already out, straight on from the
        /// homecoming: <see cref="AfterHomecoming"/>), and back down to its place at the end.
        /// </summary>
        public const float DroneOutTo = 1.2f, DroneBackFrom = Seconds - 1.4f, DroneBackTo = Seconds - 0.1f;
        /// <summary>
        /// The nozzle's pivot and length on the tabk on the stands, in the bay's pixels (<c>KcajTabkArt.NozzlePivot</c> and
        /// <c>NozzleLength</c> at the bay's 1.5 times).
        /// </summary>
        public static readonly (float x, float y) Pivot = (BayMap.StandX - 5f * BayMap.Scale, BayMap.StandY + 18.5f * BayMap.Scale);
        public const float NozzleLength = 15f * BayMap.Scale;

        /// <summary>A figure's middle over its feet, and its two hands from there (the stage's hand out and hand down), in the bay's pixels.</summary>
        const float Middle = 16f * Staging.FigureGrow, HandOutX = 7f, HandOutY = -3.5f, HandDownX = 5f, HandDownY = 1f;

        static readonly SkitLine[] s_Lines =
        {
            new SkitLine(Content.Tones2, "DOES IT DO CONES?", 1.6f, Shot2),
            new SkitLine(Content.Kie, "DID SOMEONE SAY CONES?", 5.9f, Shot3),
            new SkitLine(Content.Kcaj, "IT DOES TWO.", 10.5f, Shot4),
            new SkitLine(Content.Kie, "...OH. THAT KIND.", 13.9f, 15.4f),
            new SkitLine(Content.Tones2, "...OMFG.", LickTo, ExitFrom),
        };
        static readonly SoldierDef[] s_Cast = { Content.Tones2, Content.Kcaj, Content.Kie };

        public override string Key => Id;
        public override SkitPlace Place => SkitPlace.Bay;
        public override float Length => Seconds;
        public override float[] Shots => new[] { Shot2, Shot3, Shot4 };
        public override IReadOnlyList<SkitLine> Lines => s_Lines;
        public override IReadOnlyList<SoldierDef> Cast => s_Cast;

        /// <summary>The machine on the stands, its swirl and kcaj's note on its side: the bubbles keep off them (kie's, between them, goes to his right).</summary>
        public override (float left, float right, float top)? Clear => (BayMap.StandX - 4f, BayMap.StandX + 18f, BayMap.StandY + 43f);

        /// <summary>It follows kcaj's tabk coming home on the same visit: the drone's already out of shot.</summary>
        public bool AfterHomecoming { get; set; }

        // ------------------------------------------------------------------ tones2

        /// <summary>tones2's feet: walking up to behind the stands, there through the skit, then off down the bay at the end.</summary>
        public float Tones2X => T < ExitFrom ? Lerp(Tones2From, Tones2Up, Between(WalkFrom, WalkTo)) : Lerp(Tones2Up, Tones2Out, Between(ExitFrom, ExitTo));
        public bool Tones2Walking => T >= WalkFrom && T < WalkTo || Leaving;
        /// <summary>The end: kcaj and tones2 walking off (tones2 facing right, away down the bay).</summary>
        public bool Leaving => T >= ExitFrom;
        /// <summary>Both are out of shot: nothing of them shows.</summary>
        public bool Gone => T >= ExitTo;
        /// <summary>tones2 eyeing the swirl on top (his head up), once he's there and till the nozzle turns on him.</summary>
        public bool Tones2LooksUp => T >= WalkTo && T < Shot3;

        // ------------------------------------------------------------------ kie

        /// <summary>kie's in the bay: from his sprint in till he's out of the door again.</summary>
        public bool KieIn => T >= RunFrom && T < LeaveTo;

        /// <summary>kie's feet: the sprint in from the door, the skid, standing between them, then walking back out.</summary>
        public float KieX
        {
            get
            {
                if (T < SkidFrom) return Lerp(KieFrom, KieSkid, Between(RunFrom, SkidFrom));
                if (T < LeaveFrom) return Lerp(KieSkid, KieStop, 1f - Sq(1f - Between(SkidFrom, SkidTo))); // slowing to a stop
                return Lerp(KieStop, KieFrom, Between(LeaveFrom, LeaveTo));
            }
        }

        /// <summary>kie's legs going: the sprint, and the slow walk out (the skid's his feet planted).</summary>
        public bool KieRunning => T >= RunFrom && T < SkidFrom;
        public bool KieLeaving => T >= LeaveFrom;
        /// <summary>He skids leaning back on his heels.</summary>
        public bool Skidding => T >= SkidFrom && T < SkidTo;

        /// <summary>kie's face: grinning till it falls (<see cref="FallSince"/>), glum after.</summary>
        public bool Falling => T >= FallFrom;
        /// <summary>Seconds since his face began to fall (for <c>CrewFigure.FaceFall</c>).</summary>
        public float FallSince => T - FallFrom;

        /// <summary>kie looking down at the cones (his head dipped) rather than at tones2: the cones, tones2, the cones again.</summary>
        public bool KieLooksDown => T >= FallFrom && T < LookTones2 || T >= LookBack && T < LookedAt;

        // ------------------------------------------------------------------ the nozzle and the cones

        /// <summary>The turn that aims the nozzle (pointing left at rest) round over the top at tones2's hands, in degrees (clockwise is less).</summary>
        public static float AimTurn
        {
            get
            {
                (float hx, float hy) = HandFront;
                double aim = Math.Atan2(hy - Pivot.y, hx - Pivot.x) * 180.0 / Math.PI;
                return (float)(aim - 180.0);
            }
        }

        /// <summary>How far the nozzle's turned now: 0 at rest, <see cref="AimTurn"/> on tones2 (it stays on him).</summary>
        public float NozzleTurn => AimTurn * Eased(SwingFrom, SwingTo);

        /// <summary>The swirl wobbling while the machine pipes.</summary>
        public bool Wobbling => T >= WobbleFrom && T < WobbleTo;

        /// <summary>The nozzle's mouth, aimed at tones2 (the bay's pixels).</summary>
        public static (float x, float y) Tip
        {
            get
            {
                double a = (AimTurn + 180.0) * Math.PI / 180.0;
                return (Pivot.x + (float)Math.Cos(a) * NozzleLength, Pivot.y + (float)Math.Sin(a) * NozzleLength);
            }
        }

        /// <summary>tones2's hands where he stops, facing left: the front one held out, the back one by his side.</summary>
        public static (float x, float y) HandFront => (Tones2Up - HandOutX, BayMap.FloorY(BayDeck.Floor) + Middle + HandOutY);
        public static (float x, float y) HandBack => (Tones2Up - HandDownX, BayMap.FloorY(BayDeck.Floor) + Middle + HandDownY);

        /// <summary>Cone <paramref name="i"/> (0 the first, 1 the second): -1 before it leaves the nozzle, 0 to 1 in the air, 1 once it's hit.</summary>
        public float Cone(int i) => T < (i == 0 ? FirstFrom : SecondFrom) ? -1f : i == 0 ? Between(FirstFrom, FirstAt) : Between(SecondFrom, SecondAt);

        /// <summary>Cone <paramref name="i"/> has splatted on tones2 (and is in his hand, squashed).</summary>
        public bool Hit(int i) => Cone(i) >= 1f;

        /// <summary>Seconds since the first cone splatted on him (for the splat's burst and drip), or -1.</summary>
        public float SplatSince => T < FirstAt ? -1f : T - FirstAt;

        /// <summary>Where cone <paramref name="i"/> is in the air (the bay's pixels): an arc from the nozzle's mouth to its hand.</summary>
        public (float x, float y) ConeAt(int i)
        {
            float k = Math.Max(0f, Cone(i));
            (float x0, float y0) = Tip;
            (float x1, float y1) = i == 0 ? HandFront : HandBack;
            return (Lerp(x0, x1, k), Lerp(y0, y1, k) + Arc * 4f * k * (1f - k));
        }

        // ------------------------------------------------------------------ kcaj

        /// <summary>kcaj's feet: leaning on the nozzle, then off to the den's door at the end.</summary>
        public float KcajFeet => Lerp(KcajX, KcajOut, Between(ExitFrom, ExitTo));

        // ------------------------------------------------------------------ the drone

        /// <summary>Where the drone's middle is (the bay's pixels): out of shot above its place, leaving it at the start or coming back at the end.</summary>
        public (float x, float y) Drone
        {
            get
            {
                DroneShow.Point home = DroneShow.Home;
                float k = T >= DroneBackFrom ? 1f - Eased(DroneBackFrom, DroneBackTo) : AfterHomecoming ? 1f : Eased(0.2f, DroneOutTo);
                return (home.X, Lerp(home.Y, KcajHomecoming.OutY, k));
            }
        }

        /// <summary>The drone's on the move (its whine plays).</summary>
        public bool DroneFlying => !AfterHomecoming && T < DroneOutTo || T >= DroneBackFrom && T < DroneBackTo;

        /// <summary>tones2 licking the cone in his front hand (it's at his mouth).</summary>
        public bool Licking => T >= LickFrom && T < LickTo;

        static float Sq(float k) => k * k;
        static float Lerp(float a, float b, float k) => a + (b - a) * k;
    }
}
