using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>Where a skit plays: in the den, as its beats do, or in the tabk bay, as kie's homecoming does.</summary>
    public enum SkitPlace { Den, Bay }

    /// <summary>One line of a skit: who says it (in a speech bubble), and when it's up.</summary>
    public readonly struct SkitLine
    {
        public readonly SoldierDef Who;
        public readonly string Text;
        public readonly float From, To;

        public SkitLine(SoldierDef who, string text, float from, float to)
        {
            Who = who;
            Text = text;
            From = from;
            To = to;
        }
    }

    /// <summary>
    /// A skit (<c>docs/design/cutscenes/skits.md</c>): a short comic crew moment played once ever in the room itself, the
    /// den or the bay, with speech bubbles only and the room's own music. Each kind is a subclass with its script's
    /// timings; this is what they share: the clock, its shots, the lines, tap to skip, and its once-only key
    /// (<see cref="Skits"/>). The screen's walking waits while it plays, and afterwards everyone's back where they were.
    /// </summary>
    public abstract class Skit
    {
        /// <summary>The key kept once it's played or skipped (in <see cref="OnceOnly.All"/>). It never changes.</summary>
        public abstract string Key { get; }
        public abstract SkitPlace Place { get; }
        /// <summary>Its seconds, end to end.</summary>
        public abstract float Length { get; }
        /// <summary>When each shot after the first starts (the den and the bay have no cuts: a shot is a beat of the action).</summary>
        public abstract float[] Shots { get; }
        /// <summary>Its lines, in order; a line's up from its <see cref="SkitLine.From"/> till its <see cref="SkitLine.To"/>.</summary>
        public abstract IReadOnlyList<SkitLine> Lines { get; }
        /// <summary>Who's in it, drawn by the skit (the room's own figures of them stand aside till it's done).</summary>
        public abstract IReadOnlyList<SoldierDef> Cast { get; }

        public float T { get; private set; }
        public bool Done => T >= Length - 1e-4f;

        /// <summary>Moves it on.</summary>
        public void Tick(float dt) => T += dt;

        /// <summary>Tap to skip: straight to the end (what the end leaves behind, such as the den's button, still happens).</summary>
        public void Skip() => T = Length;

        /// <summary>Did <paramref name="at"/> pass in the tick just made (<paramref name="dt"/> long)? For its sounds.</summary>
        public bool Passed(float at, float dt) => T >= at && T - dt < at;

        /// <summary>The shot playing, from 1.</summary>
        public int Shot
        {
            get
            {
                int shot = 1;
                foreach (float s in Shots)
                    if (T >= s) shot++;
                return shot;
            }
        }

        /// <summary>The line up now, or null.</summary>
        public SkitLine? Line
        {
            get
            {
                foreach (SkitLine l in Lines)
                    if (T >= l.From && T < l.To) return l;
                return null;
            }
        }

        /// <summary>How far through <paramref name="from"/> to <paramref name="to"/> it is: 0 before, 1 after.</summary>
        public float Between(float from, float to) => Math.Max(0f, Math.Min(1f, (T - from) / (to - from)));

        /// <summary>The same, eased in and out.</summary>
        public float Eased(float from, float to)
        {
            float k = Between(from, to);
            return k * k * (3f - 2f * k);
        }

        public override string ToString() => Key;
    }

    /// <summary>
    /// Two of us now (14 s, the den): straight after the first visit, tones2 flops onto the couch with a controller, kie
    /// picks the second one up off the empty seat, holds it out to us (BRING A MATE), and tosses it at the camera; where it
    /// lay, the co-op button pops up for the first time and pulses once. A skip still puts the button up.
    /// </summary>
    public sealed class TwoOfUsSkit : Skit
    {
        public const string Id = "skit-two-of-us";
        public const float Shot2 = 4f, Shot3 = 9f, Seconds = 14f;
        /// <summary>
        /// Shot 1: from where the first visit leaves them (tones2 by the TV's plug, kie by the couch's left arm; on a later
        /// visit, near their stations, the same sides), tones2 walks to the left seat and flops into it, and kie walks over
        /// to the right one and picks the controller up off it.
        /// </summary>
        public const float FlopFrom = 0.1f, FlopAt = 1f, KieThere = 1.4f, PickAt = 1.5f;
        /// <summary>Shot 2: kie turns to us, the controller out; tones2's hand up.</summary>
        public const float TurnAt = Shot2, HandFrom = 6.6f, HandTo = 8.4f;
        /// <summary>Shot 3: the toss (the controller flies at the camera), the button popping up where it lay, its pulse, kie dropping onto the couch's arm.</summary>
        public const float TossAt = 11.2f, TossTo = 11.6f, ButtonAt = 11.4f, PulseTo = 12.4f, PerchAt = 12.3f;

        static readonly SkitLine[] s_Lines =
        {
            new SkitLine(Content.Kie, "TWO OF US NOW.", 1.8f, Shot2),
            new SkitLine(Content.Kie, "BRING A MATE.", 4.3f, 6.5f),
            new SkitLine(Content.Tones2, "I'M RIGHT HERE.", 6.6f, Shot3),
            new SkitLine(Content.Kie, "ANOTHER MATE, MATE.", 9.3f, TossAt),
        };
        static readonly SoldierDef[] s_Cast = { Content.Tones2, Content.Kie };

        public override string Key => Id;
        public override SkitPlace Place => SkitPlace.Den;
        public override float Length => Seconds;
        public override float[] Shots => new[] { Shot2, Shot3 };
        public override IReadOnlyList<SkitLine> Lines => s_Lines;
        public override IReadOnlyList<SoldierDef> Cast => s_Cast;

        /// <summary>tones2 to his seat: 0 where he was, 1 sat.</summary>
        public float Flop => Between(FlopFrom, FlopAt);
        /// <summary>kie over to the right seat: 0 where he was, 1 there.</summary>
        public float KieWalk => Between(FlopFrom, KieThere);
        /// <summary>kie has the second controller in his hand (it's off the seat).</summary>
        public bool Holding => T >= PickAt && T < TossAt;
        /// <summary>kie facing us, the controller held out, from shot 2 till the toss.</summary>
        public bool HoldingOut => T >= TurnAt && T < TossAt;
        /// <summary>tones2's hand up.</summary>
        public bool HandUp => T >= HandFrom && T < HandTo;
        /// <summary>The tossed controller flying at the camera: 0 leaving his hand, 1 gone past us (-1 before the toss).</summary>
        public float Toss => T < TossAt ? -1f : Between(TossAt, TossTo);
        /// <summary>The button's up (from the moment it pops, and for good).</summary>
        public bool ButtonUp => T >= ButtonAt;
        /// <summary>
        /// The button's size as it pops up and pulses once: 0 to full in a blink, then one soft swell and back. 1 after.
        /// Never a flash: only its size moves.
        /// </summary>
        public float ButtonScale
        {
            get
            {
                if (T < ButtonAt) return 0f;
                float pop = Between(ButtonAt, ButtonAt + 0.15f);
                float pulse = Between(ButtonAt + 0.2f, PulseTo);
                return pop * (1f + 0.12f * (float)Math.Sin(pulse * Math.PI));
            }
        }
        /// <summary>The pulse's glow, 0 to 1 and back once (for the button's co-op blue).</summary>
        public float Glow => T < ButtonAt + 0.2f ? 0f : (float)Math.Sin(Between(ButtonAt + 0.2f, PulseTo) * Math.PI);
        /// <summary>kie's dropped onto the couch's arm, pleased with himself.</summary>
        public bool Perched => T >= PerchAt;
    }

    /// <summary>
    /// Special delivery (14 s, the den): the first time the den opens after a replay of Mission one. tones2 waits on the
    /// couch with the drone's remote and the TV off; the roller door rattles up, the cargo drone flies kie in, face down
    /// and asleep on its cable, lowers him onto the couch and goes; kie rolls over hugging the snack bag, and tones2 puts
    /// the remote down and turns the TV back on.
    /// </summary>
    public sealed class SpecialDeliverySkit : Skit
    {
        public const string Id = "skit-special-delivery";
        public const float Shot2 = 4f, Shot3 = 10f, Seconds = 14f;
        /// <summary>Shot 1: the roller door rattles up.</summary>
        public const float DoorFrom = 2.6f, DoorUp = 3.4f;
        /// <summary>Shot 2: the drone flies kie in, lowers him, lets go and climbs away out under the door, which rolls down after it.</summary>
        public const float FlyFrom = Shot2, FlyTo = 6.8f, LowerTo = 7.6f, LetGo = 7.8f, LeaveFrom = 8f, LeaveTo = 9.4f, DoorDownFrom = 9.3f, DoorDownTo = 10.1f;
        /// <summary>Shot 3: kie rolls over; tones2 puts the remote down and turns the TV on.</summary>
        public const float RollAt = 10.4f, RemoteDownAt = 12.6f, TvOnAt = 13.1f;

        /// <summary>
        /// Where the drone flies (its middle, in the den's art pixels): in under the roller door, along over the couch to
        /// above its right seat, down to put kie on it, then up and back out under the door.
        /// </summary>
        public const float InX = 276f, OverX = 140f, HighY = 70f, LowY = 58f;

        static readonly SkitLine[] s_Lines =
        {
            new SkitLine(Content.Tones2, "ANY SECOND NOW.", 0.4f, 2.6f),
            new SkitLine(Content.Tones2, "SAME AS LAST TIME.", 8f, Shot3),
            new SkitLine(Content.Kie, "...LEGEND.", 10.9f, 13f),
        };
        static readonly SoldierDef[] s_Cast = { Content.Tones2, Content.Kie };

        public override string Key => Id;
        public override SkitPlace Place => SkitPlace.Den;
        public override float Length => Seconds;
        public override float[] Shots => new[] { Shot2, Shot3 };
        public override IReadOnlyList<SkitLine> Lines => s_Lines;
        public override IReadOnlyList<SoldierDef> Cast => s_Cast;

        /// <summary>The roller door: up for the drone, held while it's in, and down after it.</summary>
        public float DoorLift => T < DoorUp ? Between(DoorFrom, DoorUp) : 1f - Between(DoorDownFrom, DoorDownTo);

        /// <summary>The drone's in the den (it's behind the door before and after).</summary>
        public bool DroneIn => T >= FlyFrom && T < LeaveTo;

        /// <summary>Where the drone's middle is now (den art pixels).</summary>
        public (float x, float y) Drone
        {
            get
            {
                if (T < LowerTo) return (Lerp(InX, OverX, Eased(FlyFrom, FlyTo)), Lerp(HighY, LowY, Eased(FlyTo, LowerTo)));
                if (T < LeaveFrom) return (OverX, LowY);
                float k = Eased(LeaveFrom, LeaveTo);
                return (Lerp(OverX, InX, k), Lerp(LowY, HighY, Between(LeaveFrom, LeaveFrom + 0.5f)));
            }
        }

        /// <summary>kie's on the cable (till the hook lets go, then on the couch).</summary>
        public bool Hanging => T < LetGo;
        /// <summary>kie's in the den at all: carried in from the start of shot 2.</summary>
        public bool KieIn => T >= FlyFrom;
        /// <summary>kie's rolled over onto his back, hugging the bag.</summary>
        public bool RolledOver => T >= RollAt;
        /// <summary>tones2 still has the remote in his hand.</summary>
        public bool Remote => T < RemoteDownAt;
        /// <summary>The TV's off (it's back on at the end).</summary>
        public bool TvOff => T < TvOnAt;

        static float Lerp(float a, float b, float k) => a + (b - a) * k;
    }

    /// <summary>
    /// A Trollogram of anything (15 s, the den): straight after kcaj's move-in. kcaj, up in his nook, asks kie at his bench
    /// below; kie answers without looking up; a silence held by looks alone (kcaj to the projector, to his string board, back
    /// to kie, and tones2 frozen mid-slurp, the music ducked); then kie puts the screwdriver down, and kcaj pockets a sticky
    /// note for later.
    /// </summary>
    public sealed class TrollogramSkit : Skit
    {
        public const string Id = "skit-trollogram-of-anything";
        public const float Shot2 = 4f, Shot3 = 7f, Shot4 = 12f, Seconds = 15f;
        /// <summary>Shot 3, the silence: kcaj's looks (the projector, his board, back to kie), and tones2 frozen mid-slurp from the line before.</summary>
        public const float AtProjector = Shot3, AtBoard = 8.6f, AtKie = 10.2f, SlurpFrom = 6f;
        /// <summary>Shot 4: the screwdriver down, kcaj's note off the board, folded, and into his pocket; tones2 finishing the slurp.</summary>
        public const float ToolDownAt = Shot4, NoteFrom = 12.6f, NoteInHand = 13.1f, FoldAt = 13.6f, PocketAt = 14.2f, SlurpDoneAt = 13.4f;

        static readonly SkitLine[] s_Lines =
        {
            new SkitLine(Content.Kcaj, "CAN YOU MAKE A TROLLOGRAM OF ANYTHING?", 0.3f, Shot2),
            new SkitLine(Content.Kie, "YEAH. WHY DO YOU ASK?", 4.2f, Shot3),
            new SkitLine(Content.Kie, "ABSOLUTELY NOT.", 12.3f, Seconds),
        };
        static readonly SoldierDef[] s_Cast = { Content.Tones2, Content.Kie, Content.Kcaj };

        public override string Key => Id;
        public override SkitPlace Place => SkitPlace.Den;
        public override float Length => Seconds;
        public override float[] Shots => new[] { Shot2, Shot3, Shot4 };
        public override IReadOnlyList<SkitLine> Lines => s_Lines;
        public override IReadOnlyList<SoldierDef> Cast => s_Cast;

        /// <summary>The silence: no bubbles, and the music ducked.</summary>
        public bool Silence => T >= Shot3 && T < Shot4;

        /// <summary>What kcaj is looking at: kie below, the projector on kie's bench, or his string board (in the silence, and to peel the note off it).</summary>
        public enum Look { Kie, Projector, Board }

        public Look Kcaj => T >= AtProjector && T < AtBoard ? Look.Projector
            : T >= AtBoard && T < AtKie || T >= NoteFrom && T < NoteInHand ? Look.Board : Look.Kie;

        /// <summary>kie's working at the bench, screwdriver in hand (till he puts it down).</summary>
        public bool Working => T < ToolDownAt;

        /// <summary>tones2's straw in his mouth, mid-slurp, frozen through the silence till he finishes it.</summary>
        public bool Slurping => T >= SlurpFrom && T < SlurpDoneAt;

        /// <summary>tones2 looking from one to the other in the silence: up at kcaj (true) or across at kie.</summary>
        public bool Tones2LooksUp => Silence && (int)((T - Shot3) / 1.25f) % 2 == 1;

        /// <summary>The sticky note: 0 none, 1 on the board, 2 in his hand, 3 folded in two, and gone into his pocket after.</summary>
        public int Note => T < NoteFrom || T >= PocketAt ? 0 : T < NoteInHand ? 1 : T < FoldAt ? 2 : 3;
    }

    /// <summary>
    /// Five-second rule (11 s, the den): the den's second opening, the visit after the first visit. kie sprawls on the
    /// couch with the snack bag from the motor pool, eating, and stops with a crisp halfway to his mouth to ask where they
    /// came from; tones2, at his slushie machine, nods at the floor by the side door, where the bag burst on raid night,
    /// dead straight; kie turns the bag round, and its label close up says FAMBLY MART, new (<see cref="SnackShow"/>); he
    /// looks at tones2, unconvinced, tones2 slurps loudly, and kie eats the crisp anyway.
    /// </summary>
    public sealed class FiveSecondRuleSkit : Skit
    {
        public const string Id = "skit-five-second-rule";
        public const float Shot2 = 4f, Shot3 = 7.5f, Seconds = 11f;
        /// <summary>Shot 1: a crisp from the bag to his mouth and back, every <see cref="Munch"/> seconds, till he stops; then halfway up, and held there.</summary>
        public const float Munch = 0.7f, StopAt = 1.4f, HalfwayAt = 1.7f;
        /// <summary>Shot 2: tones2's nod at the floor by the side door.</summary>
        public const float NodFrom = 4.2f, NodTo = 4.7f;
        /// <summary>
        /// Shot 3: the bag turned round, its label close up, then kie's look at tones2 (unconvinced), tones2's slurp, and the
        /// crisp eaten.
        /// </summary>
        public const float TurnFrom = 7.6f, TurnTo = 8.2f, LabelFrom = 8.2f, LabelTo = 9.4f, DoubtAt = 9.4f,
            SlurpFrom = 9.4f, SlurpTo = 10.1f, EatFrom = 10.4f, EatTo = 10.7f;

        static readonly SkitLine[] s_Lines =
        {
            new SkitLine(Content.Kie, "WHERE'D THESE COME FROM?", 1.8f, Shot2),
            new SkitLine(Content.Tones2, "FIVE-SECOND RULE.", 4.6f, Shot3),
            new SkitLine(Content.Kie, "...LEGEND.", 9.8f, Seconds),
        };
        static readonly SoldierDef[] s_Cast = { Content.Tones2, Content.Kie };

        public override string Key => Id;
        public override SkitPlace Place => SkitPlace.Den;
        public override float Length => Seconds;
        public override float[] Shots => new[] { Shot2, Shot3 };
        public override IReadOnlyList<SkitLine> Lines => s_Lines;
        public override IReadOnlyList<SoldierDef> Cast => s_Cast;

        /// <summary>
        /// The crisp: 0 in the bag, 1 at his mouth; -1 once it's eaten. Up and down while he eats, then stopped halfway
        /// (from his question till the end), then into his mouth.
        /// </summary>
        public float Crisp
        {
            get
            {
                if (T >= EatTo) return -1f;
                if (T >= EatFrom) return 0.5f + 0.5f * Eased(EatFrom, EatTo);
                if (T >= StopAt) return 0.5f * Eased(StopAt, HalfwayAt);
                float k = (T % Munch) / Munch;
                return (float)Math.Sin(k * Math.PI);
            }
        }

        /// <summary>tones2's head dipped at the floor, in his nod.</summary>
        public bool Nodding => T >= NodFrom && T < NodTo;

        /// <summary>The bag turning round: -1 before, 0 to 1 as it turns (<see cref="SnackShow.BagWidth"/>), 1 after (its label out, for good).</summary>
        public float Turn => T < TurnFrom ? -1f : Between(TurnFrom, TurnTo);

        /// <summary>The label's close-up is up.</summary>
        public bool LabelShown => T >= LabelFrom && T < LabelTo;

        /// <summary>Seconds since the close-up came up (for <see cref="SnackShow.LabelUp"/>).</summary>
        public float LabelSince => T - LabelFrom;

        /// <summary>kie's looked from the label to tones2: the grin gone, unconvinced.</summary>
        public bool Doubting => T >= DoubtAt;

        /// <summary>tones2's cup at his mouth, slurping (loudly).</summary>
        public bool Slurping => T >= SlurpFrom && T < SlurpTo;
    }

    /// <summary>What's known as a room opens, for which skit is due (<see cref="Skits.Due"/>).</summary>
    public struct SkitVisit
    {
        public SkitPlace Place;
        public DenMode Mode;
        /// <summary>The crew freed.</summary>
        public IReadOnlyList<SoldierDef> Crew;
        /// <summary>The den's beats playing first on this visit (<see cref="DenBeats.Due"/>).</summary>
        public IReadOnlyList<DenBeat> Beats;
        /// <summary>Co-op's open on this phone (Mission one won solo, and the network's there).</summary>
        public bool CoopOpen;
        /// <summary><c>?unlock=coop</c>: the co-op button shows at once, with no skit.</summary>
        public bool CoopUnlocked;
        /// <summary>A <c>?test=</c> jump, checking something else: no skits in its way.</summary>
        public bool Testing;
        /// <summary>The den opened from the debrief of a replay of this mission (won again, solo), or null.</summary>
        public MissionDef Replayed;
        /// <summary><c>?skit=</c>, the first time the den opens on this visit: that skit plays (solo), seen or not, or null.</summary>
        public string Asked;
    }

    /// <summary>
    /// Which skit plays as a room opens (<c>docs/design/cutscenes/skits.md</c>, How a skit plays): once ever each, under its
    /// own key kept with the player's progress; solo only (never in a co-op room: it waits for the next visit on your
    /// own); one a visit, in the order Two of us now, A Trollogram of anything, Five-second rule, Special delivery; never on
    /// the same visit as a beat, except Two of us now (straight after the first visit) and A
    /// Trollogram of anything (straight after kcaj's move-in). And whether the den's co-op button shows: from the moment Two
    /// of us now puts it up.
    /// </summary>
    public static class Skits
    {
        /// <summary>Every skit's key, for <see cref="OnceOnly"/>, and what it is.</summary>
        public static readonly IReadOnlyList<(string key, string what)> All = new[]
        {
            (TwoOfUsSkit.Id, "the skit Two of us now has played: the co-op button's up"),
            (SpecialDeliverySkit.Id, "the skit Special delivery has played"),
            (TrollogramSkit.Id, "the skit A Trollogram of anything has played"),
            (FiveSecondRuleSkit.Id, "the skit Five-second rule has played"),
        };

        /// <summary>The skit due as a room opens on <paramref name="visit"/>, or null. At most one.</summary>
        public static Skit Due(Profile profile, SkitVisit visit)
        {
            if (visit.Place != SkitPlace.Den || visit.Mode != DenMode.Solo) return null;
            if (visit.Asked != null) return Named(visit.Asked);
            if (visit.Testing) return null;
            IReadOnlyList<DenBeat> beats = visit.Beats ?? new DenBeat[0];
            // a phone that's past it (co-op opened before the skit was built) gets it on its next solo visit, unless it's
            // already pressed the button: then it's found co-op, and the button just shows
            if (visit.CoopOpen && !CoopButtonUp(profile, visit.CoopUnlocked)) return new TwoOfUsSkit();
            if (TrollogramDue(profile, visit.Crew, beats)) return new TrollogramSkit();
            // the den's second opening: the first visit behind it, no beat on this one (and it waits, if it has to, for the
            // next solo visit). It goes before Special delivery, whose trigger comes round again on the next replay
            if (beats.Count == 0 && profile.HasSeen(DenBeats.FirstVisit) && !profile.HasSeen(FiveSecondRuleSkit.Id)) return new FiveSecondRuleSkit();
            if (beats.Count == 0 && visit.Replayed == Missions.One && !profile.HasSeen(SpecialDeliverySkit.Id)) return new SpecialDeliverySkit();
            return null;
        }

        /// <summary>
        /// The skit called <paramref name="name"/> (its key without <c>skit-</c>: <c>two-of-us</c>, <c>special-delivery</c>,
        /// <c>trollogram-of-anything</c>, <c>five-second-rule</c>), new, or null. For <c>?skit=</c>, to see one whenever.
        /// </summary>
        public static Skit Named(string name)
        {
            switch ("skit-" + name)
            {
                case TwoOfUsSkit.Id: return new TwoOfUsSkit();
                case SpecialDeliverySkit.Id: return new SpecialDeliverySkit();
                case TrollogramSkit.Id: return new TrollogramSkit();
                case FiveSecondRuleSkit.Id: return new FiveSecondRuleSkit();
                default: return null;
            }
        }

        /// <summary>A Trollogram of anything: kcaj's home (moving in now, or before), and no beat but his move-in on this visit.</summary>
        static bool TrollogramDue(Profile profile, IReadOnlyList<SoldierDef> crew, IReadOnlyList<DenBeat> beats)
        {
            if (profile.HasSeen(TrollogramSkit.Id) || crew == null || !RoomHas(crew, Content.Kcaj)) return false;
            bool movingIn = false;
            foreach (DenBeat b in beats)
            {
                if (b.Kind != DenBeatKind.MoveIn || b.Newcomer != Content.Kcaj) return false;
                movingIn = true;
            }
            return movingIn || profile.HasSeen(DenBeats.MovedIn(Content.Kcaj));
        }

        static bool RoomHas(IReadOnlyList<SoldierDef> crew, SoldierDef s)
        {
            foreach (SoldierDef x in crew)
                if (x == s) return true;
            return false;
        }

        /// <summary>
        /// The den's co-op button is up (when co-op's open): Two of us now has put it up, or the player has pressed it
        /// before (<see cref="Reveals.Coop"/>), or <c>?unlock=coop</c> or a <c>?test=</c> start shows it at once.
        /// </summary>
        public static bool CoopButtonUp(Profile profile, bool unlocked) =>
            unlocked || profile.HasSeen(TwoOfUsSkit.Id) || profile.HasSeen(Reveals.Coop);

        /// <summary>Marks <paramref name="skit"/> played (skipped counts). True if that changed the profile.</summary>
        public static bool Played(Profile profile, Skit skit) => profile.MarkSeen(skit.Key);
    }
}
