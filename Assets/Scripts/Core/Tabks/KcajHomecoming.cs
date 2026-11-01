using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// kcaj's tabk comes home (<c>docs/design/cutscenes/den.md</c>, kcaj's tabk comes home): a beat in the tabk bay the first
    /// solo visit once kcaj's tabk is won (Mission two cleared) and he's moved in, once ever, kept in the profile so it
    /// follows a sign-in; a tap skips it. It has a skit's shape (a clock, shots, lines from a cast, a skip and a key), so it's
    /// played as the bay's skits are (<c>SkitRun</c>, on the shared <c>SkitStage</c>), and Does it do cones?
    /// (<see cref="ConesSkit"/>) follows it straight on. Three shots, timed here, in the bay's pixels with the camera at the
    /// den's door (the screen's 320 wide from there):
    /// <list type="number">
    /// <item>The cargo drone comes down the bay from past the screen's right edge, the wreck on its cable (SOFT SERVE's hull
    /// in the plain tabk's olive, scorched and frosted, the cone on its antenna), sets it down on the stands and flies
    /// up out of shot; kcaj wanders in after it, two cones in his hands.</item>
    /// <item>The crane lowers the soft-serve machine (the BRAIN FREEZE sticker on its side) onto the turret ring; kcaj steps
    /// back, looks at it, then at tones2, and they trade three lines.</item>
    /// <item>kcaj slaps a sticky note (KCAJ) over the sticker, pulls the machine's lever: it hums, the swirl wobbles, and his
    /// purple and gold run down the hull in drips till no olive is left; the name card drops in with MINE NOW., and he
    /// leans back on the tabk polishing its nozzle.</item>
    /// </list>
    /// </summary>
    public sealed class KcajHomecoming : Skit
    {
        /// <summary>The key kept once it's played (or skipped). It never changes.</summary>
        public const string Reveal = "kcaj-tabk-home";

        public const float Shot2 = 3f, Shot3 = 9f, Seconds = 13f;

        /// <summary>Shot 1: the drone across the top of the bay, down onto the stands, letting go and up out of shot.</summary>
        public const float FlyTo = 1.6f, LandAt = 2.3f, HomeFrom = 2.4f, HomeTo = 3.4f;
        /// <summary>
        /// Where the drone waits out of shot (its middle's height, the bay's pixels): above the top of the screen, cable
        /// and all, over its place. Does it do cones? brings it back down at its end (or, with no skit after, this does).
        /// </summary>
        public const float OutY = 205f;
        /// <summary>With no skit after it (<see cref="SkitAfter"/>), the drone dropping back to its place at the end.</summary>
        public const float DroneBackFrom = Seconds - 1.4f, DroneBackTo = Seconds - 0.1f;
        /// <summary>Where the drone flies in from: past the screen's right edge with the camera at the door.</summary>
        public const float InX = StageWidth + 30f;
        /// <summary>The screen's width in the bay's pixels (the stage's).</summary>
        public const float StageWidth = 320f;
        /// <summary>The wreck's height to its roof, where the cable takes it: the hull's 15 rows under the machine, 1.5 times.</summary>
        public const float WreckH = 15f * BayMap.Scale;
        /// <summary>The fuel hose going in once it's down.</summary>
        public const float HoseFrom = LandAt + 0.3f, HoseTo = HoseFrom + 0.8f;

        /// <summary>kcaj wandering in after the drone, from past the right edge to just behind the stands.</summary>
        public const float WalkFrom = 0.6f, WalkTo = 2.9f, KcajFrom = StageWidth + 16f, KcajBehind = 150f;
        /// <summary>His bite of a cone, every so often while he walks and watches (a pulse: <see cref="Fx.Step"/>).</summary>
        public const float BiteHz = 1.2f;

        /// <summary>Shot 2: the crane lowering the machine, kcaj stepping back and looking it over, then turning to tones2.</summary>
        public const float LowerTo = 4f, BackFrom = 4f, BackTo = 4.4f, TurnAt = 4.9f, KcajBack = 166f;
        /// <summary>Where tones2 stands through the beat, on the floor to the right, facing them.</summary>
        public const float Tones2X = 215f;

        /// <summary>
        /// Shot 3: back to the tabk and a hop up onto its hull beside the machine (it stands taller than he does), the note
        /// slapped on, the lever pulled, the hum and the paint running down, the chip clunk and the name card, then a hop
        /// down, over to the nozzle, and polishing it.
        /// </summary>
        public const float ToTabkTo = Shot3 + 0.3f, HopUpTo = Shot3 + 0.5f, NoteAt = Shot3 + 0.7f, LeverAt = Shot3 + 1.2f,
            PaintFrom = Shot3 + 1.3f, PaintTo = Shot3 + 2.7f, ClunkAt = PaintTo, CardFrom = Shot3 + 2.7f,
            HopDownFrom = Shot3 + 2.9f, HopDownTo = Shot3 + 3.1f, ToNozzleTo = Shot3 + 3.8f;
        /// <summary>
        /// Where he goes in shot 3 (the bay's pixels): to the tabk's back, up onto its hull by the machine (his feet on the
        /// deck, his hand at the machine's side), down off its front corner, and to the nozzle, where he leans back on it.
        /// </summary>
        public const float KcajTabk = 128f, KcajMachine = 120f, DeckY = 36f, KcajDown = 112f, KcajNozzle = 56f;
        /// <summary>How high a hop goes over the line between its ends.</summary>
        public const float Hop = 6f;

        /// <summary>The paint runs down in this many drips side by side, each a little behind or ahead of the next.</summary>
        public const int Drips = 6;
        static readonly float[] DripLag = { 0.1f, 0f, 0.22f, 0.06f, 0.16f, 0.03f };

        /// <summary>The name card's two parts.</summary>
        public const string CardName = "KCAJ'S TABK", CardTitle = "SOFT SERVE";

        static readonly SkitLine[] s_Lines =
        {
            new SkitLine(Content.Kcaj, "THE CREAMERY. THE TROLLOGRAMS. THE MOON.", 5f, 7.1f),
            new SkitLine(Content.Tones2, "THE MOON?", 7.1f, 7.9f),
            new SkitLine(Content.Kcaj, "IT'S ALL CONNECTED.", 7.9f, Shot3),
            new SkitLine(Content.Kcaj, "MINE NOW.", CardFrom + 0.1f, Seconds),
        };
        static readonly SoldierDef[] s_Cast = { Content.Tones2, Content.Kcaj };

        public override string Key => Reveal;
        public override SkitPlace Place => SkitPlace.Bay;
        public override float Length => Seconds;
        public override float[] Shots => new[] { Shot2, Shot3 };
        public override IReadOnlyList<SkitLine> Lines => s_Lines;
        public override IReadOnlyList<SoldierDef> Cast => s_Cast;

        /// <summary>Does it do cones? follows straight on (it brings the drone back), so the drone stays out of shot to the end.</summary>
        public bool SkitAfter { get; set; }

        /// <summary>
        /// Whether it plays as the bay opens on a visit in <paramref name="mode"/>: kcaj's tabk is in <paramref name="won"/>,
        /// he's moved into the den (the bay is through the den, so his move-in always comes first; this makes sure of it),
        /// it's a visit on your own (it waits for one), and it hasn't played yet. A <c>?test=</c> jump never plays it.
        /// </summary>
        public static bool Due(Profile profile, IReadOnlyList<TabkCard> won, DenMode mode, bool testing)
        {
            if (testing || mode != DenMode.Solo || profile.HasSeen(Reveal) || !profile.HasSeen(DenBeats.MovedIn(Content.Kcaj))) return false;
            return TabkCards.Has(won, TabkCards.Kcaj);
        }

        // ------------------------------------------------------------------ shot 1

        /// <summary>The wreck's still on the drone's cable (till it's set down on the stands).</summary>
        public bool Carrying => T < LandAt;

        /// <summary>The drone over the stands, dipped to set the wreck down: its middle's height.</summary>
        public static float OverY => BayMap.StandY + WreckH + DroneShow.Hang;

        /// <summary>
        /// Where the drone's middle is (the bay's pixels): along the top of the bay from past the right edge, down onto the
        /// stands, then up over its place and out of shot, where it waits; with no skit after, it drops back to its place
        /// at the end.
        /// </summary>
        public (float x, float y) Drone
        {
            get
            {
                DroneShow.Point home = DroneShow.Home;
                if (T < FlyTo) return (Lerp(InX, BayMap.StandX, Eased(0f, FlyTo)), home.Y);
                if (T < HomeFrom) return (BayMap.StandX, Lerp(home.Y, OverY, Eased(FlyTo, LandAt)));
                if (T < DroneBackFrom || SkitAfter)
                {
                    float k = Eased(HomeFrom, HomeTo);
                    return (Lerp(BayMap.StandX, home.X, k), Lerp(OverY, OutY, k));
                }
                return (home.X, Lerp(OutY, home.Y, Eased(DroneBackFrom, DroneBackTo)));
            }
        }

        /// <summary>The drone's on the move in shot (its whine plays, and tones2 looks up at it).</summary>
        public bool Flying => T < HomeTo || !SkitAfter && T >= DroneBackFrom && T < DroneBackTo;

        /// <summary>Where the wreck's feet are: under the drone while it's carried, then on the stands.</summary>
        public (float x, float y) Wreck
        {
            get
            {
                if (!Carrying) return (BayMap.StandX, BayMap.StandY);
                (float x, float y) = Drone;
                return (x, y - DroneShow.Hang - WreckH);
            }
        }

        /// <summary>The fuel hose: hanging out till the wreck's down, then into it (0 out, 1 in).</summary>
        public float Hose => Between(HoseFrom, HoseTo);

        /// <summary>kcaj's in the bay (he wanders in after the drone).</summary>
        public bool KcajIn => T >= WalkFrom;

        /// <summary>
        /// kcaj's feet (the bay's pixels): walking in, behind the stands, stepping back, back to the tabk and up onto its
        /// hull by the machine, down off it, then over to the nozzle.
        /// </summary>
        public (float x, float y) Kcaj
        {
            get
            {
                float floor = BayMap.FloorY(BayDeck.Floor);
                if (T < WalkTo) return (Lerp(KcajFrom, KcajBehind, Between(WalkFrom, WalkTo)), floor);
                if (T < Shot3) return (Lerp(KcajBehind, KcajBack, Eased(BackFrom, BackTo)), floor);
                if (T < ToTabkTo) return (Lerp(KcajBack, KcajTabk, Between(Shot3, ToTabkTo)), floor);
                if (T < HopDownFrom)
                {
                    float k = Between(ToTabkTo, HopUpTo);
                    return (Lerp(KcajTabk, KcajMachine, k), Lerp(floor, DeckY, k) + Hop * 4f * k * (1f - k));
                }
                if (T < HopDownTo)
                {
                    float k = Between(HopDownFrom, HopDownTo);
                    return (Lerp(KcajMachine, KcajDown, k), Lerp(DeckY, floor, k) + Hop * 4f * k * (1f - k));
                }
                return (Lerp(KcajDown, KcajNozzle, Between(HopDownTo, ToNozzleTo)), floor);
            }
        }

        /// <summary>kcaj's walking (not standing).</summary>
        public bool KcajWalking => T >= WalkFrom && T < WalkTo || T >= Shot3 && T < ToTabkTo || T >= HopDownTo && T < ToNozzleTo;

        /// <summary>In the air, hopping up onto the hull or down off it.</summary>
        public bool KcajHopping => T >= ToTabkTo && T < HopUpTo || T >= HopDownFrom && T < HopDownTo;

        /// <summary>
        /// kcaj facing left (toward the door and the stands): all but when he turns to tones2 in shot 2. He steps back
        /// still facing the tabk, and leans back on it at the end facing out, the nozzle behind him.
        /// </summary>
        public bool KcajFacesLeft => !(T >= TurnAt && T < Shot3);

        /// <summary>His two cones, in his hands till he's at the machine (then they're in his pockets: his hands are busy).</summary>
        public bool Cones => T < Shot3;

        /// <summary>A bite of a cone (the cone at his mouth), now and then while he has them.</summary>
        public bool Biting => Cones && KcajIn && Fx.Step(T, BiteHz, 3) == 0;

        // ------------------------------------------------------------------ shot 2

        /// <summary>The machine's on the tabk (the crane's bringing it, or it's down): from shot 2.</summary>
        public bool Machine => T >= Shot2;

        /// <summary>How far the crane's lowered the machine: 0 high up, 1 on the turret ring (and it stays).</summary>
        public float Lowered => T < Shot2 ? 0f : Between(Shot2, LowerTo);

        // ------------------------------------------------------------------ shot 3

        /// <summary>kcaj up on the hull, his hand at the machine's side: the note, then the lever.</summary>
        public bool AtMachine => T >= HopUpTo && T < HopDownFrom;

        /// <summary>Pulling the lever: his hand down a moment.</summary>
        public bool Pulling => T >= LeverAt && T < LeverAt + 0.25f;

        /// <summary>The note's over the sticker (from his slap till the skit after ends: the bay's skit view keeps it up).</summary>
        public bool Note => T >= NoteAt;

        /// <summary>The machine humming, the swirl on top wobbling: from the lever till the paint's run down.</summary>
        public bool Humming => T >= LeverAt && T < PaintTo;

        /// <summary>
        /// How far down the hull drip <paramref name="drip"/> (0 to <see cref="Drips"/> - 1, left to right) has run: 0 none
        /// of it, 1 all the way to the tracks. They run together, each a little behind or ahead of its neighbours, so the
        /// edge is ragged, and all are down by <see cref="PaintTo"/>.
        /// </summary>
        public float Paint(int drip)
        {
            float k = Between(PaintFrom, PaintTo);
            if (k >= 1f) return 1f;
            float lag = DripLag[Math.Max(0, Math.Min(Drips - 1, drip))];
            return Math.Max(0f, Math.Min(1f, (k - lag) / (1f - 0.25f)));
        }

        /// <summary>The paint's all down: no olive left, SOFT SERVE whole.</summary>
        public bool Painted => T >= PaintTo;

        /// <summary>The name card dropping in: 0 above the screen, 1 in place.</summary>
        public float Card => Between(CardFrom, CardFrom + 0.35f);

        /// <summary>kcaj leaning back on the tabk polishing the nozzle, the rag out (from when he's there).</summary>
        public bool Polishing => T >= ToNozzleTo;

        /// <summary>The note's slap: it's bigger for a moment as it lands, then flat (1).</summary>
        public float NoteSize => 1f + 0.4f * (1f - Between(NoteAt, NoteAt + 0.12f));

        static float Lerp(float a, float b, float k) => a + (b - a) * k;
    }
}
