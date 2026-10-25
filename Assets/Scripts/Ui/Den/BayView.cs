using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The tabk bay's screen (<c>docs/design/lobby.md</c>, The tabk bay): the garage through the den's roller door, painted
    /// 993 pixels wide on the den's stage, the camera following you along it. The crew's tabks stand in it 1.5 times the
    /// game's size: the loaded one on the repair stands, ticking over with its lights on while a fuel hose fills it and a
    /// gauge climbs, the rest in their spaces. The cargo drone waits over the stands and carries them about
    /// (<see cref="DroneShow"/>); the lift's platform carries you to the top deck. A tabk you walk up to shows its card:
    /// its name in its crewmate's colour, its guns and their rounds, and TAKE IT (or LOADED). In co-op the tabk your
    /// partner has loaded is taken: dimmed in its space under their P1 or P2 tag, its card saying TAKEN (<see cref="CoopTabks"/>).
    /// The first visit with kie's tabk won, it comes home (<see cref="BayHomecomingView"/>).
    /// <para>
    /// The rules are Core's (<see cref="BayMap"/>, <see cref="BayWalker"/>, <see cref="Garage"/>); <see cref="BayFlow"/>
    /// runs them, and this draws them and takes the taps.
    /// </para>
    /// </summary>
    public sealed class BayView : StageScreen
    {
        /// <summary>Tabks here are drawn 1.5 times the game's sprite (Design's scale rule for the bay).</summary>
        const float Scale = BayMap.Scale;
        /// <summary>The partner's tabk, taken: dimmed in its space.</summary>
        static readonly Color TakenDim = new Color(0.42f, 0.42f, 0.48f, 0.8f);
        /// <summary>The fuel hose: where it comes from (the gas bottles by the bench), where it hangs when it's out, and its filler on the tabk on the stands.</summary>
        static readonly Vector2 HoseFrom = new Vector2(168f, 42f), HoseHang = new Vector2(160f, 18f), Filler = new Vector2(BayMap.StandX + 26f, BayMap.StandY + 20f);
        static readonly Vector2 Gauge = new Vector2(150f, 50f);
        const int HoseDots = 16;

        readonly RectTransform m_Tabks, m_Card;
        readonly Image m_Lift, m_LiftEdge, m_GaugeFill, m_Spark, m_Drone, m_Cable, m_Hook, m_Nozzle;
        readonly Image[] m_Hose = new Image[HoseDots], m_Lights = new Image[2];
        readonly StageFigure m_Soldier;
        readonly Text m_Hint, m_Note, m_CardName, m_CardGuns, m_CardRounds, m_TakeLabel, m_Loaded, m_TakenTag;
        readonly Button m_Take;
        readonly Dictionary<TabkCard, Image> m_Bodies = new Dictionary<TabkCard, Image>();
        readonly System.Random m_Rng = new System.Random();
        readonly BayHomecomingView m_Home;

        float m_Clock, m_CamX, m_NoteUntil, m_SparkAt, m_SparkUntil, m_Fill;
        SoldierDef m_SoldierDef;
        TabkCard m_Taken;
        int m_TakenSeat;

        readonly Latch<int> m_TapSpot = new Latch<int>();
        readonly Latch<BayPlace> m_TapPlace = new Latch<BayPlace>();
        readonly Latch<(BayDeck deck, float x)> m_TapWalk = new Latch<(BayDeck deck, float x)>();
        readonly Latch m_Back = new Latch();
        readonly Latch m_TakeIt = new Latch();
        readonly Latch m_Close = new Latch();

        public BayView(Transform parent) : base(parent, "Tabk bay", 9) // over the den (8), under the menus (10)
        {
            m_Letterbox.raycastTarget = true; // taps stop here, not on the den under it

            // back to front: the painting, the lift's platform, the gauge, the tabks, the hose, the spark, you, the drone
            Sprite bay = DenRoom.Painting("bay");
            if (bay != null)
                Ui.Picture(m_World, "Bay", bay, Vector2.zero, Vector2.zero, Vector2.zero, bay.rect.size * Px, Color.white);
            m_Lift = Picture(m_World, "Lift platform", null, BayMap.LiftX, 0f, 62f, 3f, new Color(0.23f, 0.26f, 0.31f));
            m_LiftEdge = Picture(m_World, "Lift edge", null, BayMap.LiftX, 0f, 62f, 1f, Gold);
            Picture(m_World, "Gauge", null, Gauge.x, Gauge.y, 6f, 15f, new Color(0.08f, 0.08f, 0.1f));
            m_GaugeFill = Picture(m_World, "Gauge fill", null, Gauge.x, Gauge.y + 1f, 4f, 0f, new Color(0.3f, 1f, 0.45f));
            m_Tabks = Ui.Rect(m_World, "Tabks", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            // the hose over the tabk on the stands, into its filler
            for (int i = 0; i < HoseDots; i++) m_Hose[i] = Picture(m_World, "Hose", null, 0f, 0f, 2f, 2f, new Color(0.62f, 0.16f, 0.12f));
            m_Nozzle = Picture(m_World, "Nozzle", null, 0f, 0f, 3f, 3f, Gold);
            for (int i = 0; i < 2; i++) m_Lights[i] = Picture(m_World, "Light", null, 0f, 0f, 4f, 3f, new Color(1f, 0.9f, 0.5f, 0.65f));
            m_Spark = Picture(m_World, "Spark", Art.Spark[0], 0f, 0f, 10f, 10f, Color.white);
            m_TakenTag = Label(m_World, "Taken tag", 0f, 0f, 9, TextAnchor.LowerCenter, Palette.Players[1]);
            m_Home = new BayHomecomingView(m_World, m_Stage);
            m_Soldier = new StageFigure(m_World, "You");
            m_Cable = Picture(m_World, "Cable", null, 0f, 0f, 1f, 1f, new Color(0.1f, 0.1f, 0.12f));
            m_Hook = Picture(m_World, "Hook", null, 0f, 0f, 4f, 2f, new Color(0.55f, 0.58f, 0.62f));
            Sprite drone = Tones2Art.TabkDrone[0];
            m_Drone = Ui.Picture(m_World, "Drone", drone, Vector2.zero, Ui.Middle, Vector2.zero, drone.rect.size * Scale * Px, Color.white);

            // the whole bay takes taps: a tabk to look over, a place to use, anywhere else to walk to
            RectTransform tap = Ui.Rect(m_World, "Tap bay", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(BayMap.Width, StageH) * Px);
            Ui.Catcher(tap);
            OnPointer(tap, EventTriggerType.PointerClick, Tapped);

            // over it: BACK, the hint and notes, and a tabk's card
            Vector2 half = new Vector2(StageW, StageH) * Px / 2f;
            Ui.Button(m_Stage, "BACK", new Vector2(-half.x + 40f, half.y - 18f), new Vector2(64f, 22f), m_Back.Set, out Text backKey);
            backKey.text = "ESC";
            m_Hint = Ui.Label(m_Stage, "Hint", Ui.TopCenter, new Vector2(0f, -8f), 10, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.75f));
            m_Note = Ui.Label(m_Stage, "Note", Ui.TopCenter, new Vector2(0f, -24f), 11, TextAnchor.UpperCenter, new Color(1f, 0.55f, 0.45f));

            m_Card = Ui.Rect(m_Stage, "Tabk card", Ui.Middle, Ui.Middle, new Vector2(half.x - 116f, 14f), new Vector2(206f, 132f));
            var back = m_Card.gameObject.AddComponent<Image>();
            back.color = Band;
            back.raycastTarget = true;
            m_Card.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.25f);
            m_CardName = Ui.Label(m_Card, "Name", Ui.TopCenter, new Vector2(0f, -10f), 14, TextAnchor.UpperCenter, Gold);
            m_CardGuns = Ui.Label(m_Card, "Guns", Ui.TopCenter, new Vector2(-22f, -34f), 11, TextAnchor.UpperLeft, Color.white);
            m_CardGuns.rectTransform.sizeDelta = new Vector2(124f, 52f);
            m_CardRounds = Ui.Label(m_Card, "Rounds", Ui.TopCenter, new Vector2(70f, -34f), 11, TextAnchor.UpperRight, Gold);
            m_CardRounds.rectTransform.sizeDelta = new Vector2(30f, 52f);
            m_Take = Ui.Button(m_Card, "TAKE IT", new Vector2(0f, -48f), new Vector2(110f, 26f), m_TakeIt.Set, out Text takeKey);
            takeKey.text = "ENTER";
            m_TakeLabel = m_Take.GetComponentInChildren<Text>();
            m_Loaded = Ui.Label(m_Card, "Loaded", Ui.Middle, new Vector2(0f, -48f), 14, TextAnchor.MiddleCenter, new Color(0.3f, 1f, 0.45f));
            m_Loaded.text = "LOADED";
            Button close = Ui.Button(m_Card, "X", new Vector2(93f, 56f), new Vector2(22f, 22f), m_Close.Set, out Text closeKey);
            close.GetComponent<Image>().sprite = Ui.Disc;
            closeKey.text = "";

            Ui.Show(m_Root, false);
        }

        void Tapped(PointerEventData e)
        {
            if (!ToArt(e, out Vector2 p)) return;
            int? spot = BayMap.SpotAt(p.x, p.y);
            if (spot != null && m_Garage != null && m_Garage.At(spot.Value) != null)
            {
                m_TapSpot.Set(spot.Value);
                return;
            }
            BayPlace place = BayMap.PlaceAt(p.x, p.y);
            if (place != BayPlace.None)
            {
                m_TapPlace.Set(place);
                return;
            }
            m_TapWalk.Set((BayMap.DeckAt(p.x, p.y), p.x));
        }

        Garage m_Garage;

        /// <summary>Opens the bay on <paramref name="garage"/>'s tabks, with <paramref name="soldier"/> walking it.</summary>
        public void Begin(Garage garage, SoldierDef soldier)
        {
            m_Garage = garage;
            m_SoldierDef = soldier;
            foreach (Image body in m_Bodies.Values) Object.Destroy(body.gameObject);
            m_Bodies.Clear();
            foreach (TabkCard c in garage.Won)
            {
                Sprite s = TabkKit.Of(c).Parked;
                m_Bodies[c] = Ui.Picture(m_Tabks, c.Name, s, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, s.rect.size * Scale * Px, Color.white);
            }
            m_Clock = 0f;
            m_CamX = 0f;
            m_NoteUntil = 0f;
            m_SparkAt = 2f;
            m_Fill = 0.3f;
            m_TapSpot.Clear();
            m_TapPlace.Clear();
            m_TapWalk.Clear();
            m_Back.Clear();
            m_TakeIt.Clear();
            m_Close.Clear();
            m_World.anchoredPosition = Vector2.zero;
            Ui.Show(m_Card, false);
            Ui.Show(m_Root, true);
        }

        public void Hide()
        {
            EndHomecoming();
            Ui.Show(m_Root, false);
        }

        /// <summary>kie's tabk coming home, playing over the bay (<see cref="BayFlow"/> sets it), or null.</summary>
        public KieHomecoming Homecoming { get; private set; }

        public void PlayHomecoming(KieHomecoming beat) => Homecoming = beat;

        /// <summary>The homecoming's over (or skipped): kie's tabk whole, the bay as it is.</summary>
        public void EndHomecoming()
        {
            if (Homecoming == null) return;
            Homecoming = null;
            m_Home.Hide();
            if (m_Bodies.TryGetValue(TabkCards.Kie, out Image body))
            {
                body.sprite = TabkKit.Of(TabkCards.Kie).Parked;
                body.color = Color.white;
            }
        }

        public int? TakeSpot() => m_TapSpot.Take(out int s) ? s : (int?)null;

        public BayPlace TakePlace() => m_TapPlace.Take(out BayPlace p) ? p : BayPlace.None;

        public bool TakeWalk(out BayDeck deck, out float x)
        {
            bool tapped = m_TapWalk.Take(out var to);
            (deck, x) = to;
            return tapped;
        }

        public bool TakeBack() => m_Back.Take();
        public bool TakeTakeIt() => m_TakeIt.Take();
        public bool TakeClose() => m_Close.Take();

        /// <summary>Says something across the top for a few seconds.</summary>
        public void Note(string text, float seconds = 3f)
        {
            m_Note.text = text;
            m_NoteUntil = m_Clock + seconds;
        }

        // ------------------------------------------------------------------ each frame

        /// <summary>
        /// Draws the bay: <paramref name="walker"/> you, <paramref name="show"/> the drone's moves (or null, waiting at
        /// home), <paramref name="card"/> the tabk whose card is open (or null), and whether it's being loaded now;
        /// <paramref name="taken"/> the partner's tabk in co-op (or null), and their seat.
        /// </summary>
        public void Refresh(float dt, BayWalker walker, DroneShow show, TabkCard card, bool loading, bool touch, TabkCard taken, int takenSeat)
        {
            m_Clock += dt;
            m_Taken = taken;
            m_TakenSeat = takenSeat;
            float hose = show?.Hose ?? 1f;
            Tabks(show, hose);
            Hose(hose, dt);
            Drone(show);
            You(walker);
            m_Lift.rectTransform.anchoredPosition = new Vector2(BayMap.LiftX, walker.LiftY - 3f) * Px;
            m_LiftEdge.rectTransform.anchoredPosition = new Vector2(BayMap.LiftX, walker.LiftY - 1f) * Px;
            Card(Homecoming != null ? null : card, loading);
            if (Homecoming != null && m_Bodies.TryGetValue(TabkCards.Kie, out Image kie))
                m_Home.Draw(Homecoming, kie, m_Soldier.Head, m_Clock);
            Ui.Show(m_Hint, Homecoming == null);
            m_Hint.text = touch ? "TAP A TABK TO LOOK IT OVER · THE LIFT GOES UP · THE DOOR'S BACK TO THE DEN"
                : "LEFT / RIGHT WALK · JUMP LOOKS A TABK OVER · UP / DOWN AT THE LIFT";
            Ui.Show(m_Note, m_Clock < m_NoteUntil);
            Camera(dt, walker);
        }

        /// <summary>The won tabks where they are; the loaded one, settled on the stands, ticks over with its lights on and the odd spark.</summary>
        void Tabks(DroneShow show, float hose)
        {
            TabkCard idling = null;
            Image carried = null;
            foreach (var kv in m_Bodies)
            {
                TabkCard c = kv.Key;
                Vector2 feet;
                if (show != null && show.Where(c, out DroneShow.Point at))
                {
                    feet = new Vector2(at.X, at.Y);
                    if (show.Carried == c) carried = kv.Value;
                }
                else
                {
                    int spot = m_Garage.SpotOf(c);
                    feet = new Vector2(BayMap.SpotX(spot), BayMap.SpotY(spot));
                }
                bool onStands = Mathf.Abs(feet.x - BayMap.StandX) < 0.5f && Mathf.Abs(feet.y - BayMap.StandY) < 0.5f;
                if (onStands && hose >= 1f) idling = c;
                float tick = onStands && hose >= 1f && Fx.Blink(m_Clock, 8f) ? 0.5f : 0f; // ticking over
                kv.Value.rectTransform.anchoredPosition = new Vector2(feet.x, feet.y + tick) * Px;
                if (Homecoming == null) kv.Value.color = c == m_Taken ? TakenDim : Color.white; // the homecoming paints kie's itself
                if (c == m_Taken) m_TakenTag.rectTransform.anchoredPosition = new Vector2(feet.x, feet.y + BayMap.TabkH + 3f) * Px;
            }
            bool tagged = m_Taken != null && m_Bodies.ContainsKey(m_Taken);
            Ui.Show(m_TakenTag, tagged);
            if (tagged)
            {
                m_TakenTag.text = "P" + (m_TakenSeat + 1);
                m_TakenTag.color = Palette.Players[Mathf.Clamp(m_TakenSeat, 0, 1)];
            }
            if (carried != null) carried.transform.SetAsLastSibling();
            for (int i = 0; i < 2; i++)
            {
                Ui.Show(m_Lights[i], idling != null);
                // its headlamps, where the sprite's are (1.5 times)
                if (idling != null) m_Lights[i].rectTransform.anchoredPosition = new Vector2(BayMap.StandX + (i == 0 ? -9f : 16.5f) * Scale, BayMap.StandY + 10f * Scale) * Px;
                if (idling != null) m_Lights[i].color = new Color(1f, 0.9f, 0.5f, 0.5f + 0.15f * Mathf.Sin(m_Clock * 3f));
            }
            // now and then a spark at its engine, under the tuning
            if (idling != null && m_Clock >= m_SparkAt)
            {
                m_SparkUntil = m_Clock + 0.18f;
                m_SparkAt = m_Clock + 2f + (float)m_Rng.NextDouble() * 3.5f;
                if (m_CamX < 120f) Sfx.Play(Sfx.Servo, 0.5f);
            }
            bool spark = idling != null && m_Clock < m_SparkUntil;
            Ui.Show(m_Spark, spark);
            if (spark)
            {
                m_Spark.sprite = Art.Spark[Fx.Spark.Step(m_Clock)];
                m_Spark.rectTransform.anchoredPosition = new Vector2(BayMap.StandX + 22f, BayMap.StandY + 20f) * Px;
            }
        }

        /// <summary>The fuel hose from the bottles to the filler (or hanging off them while it's out), and the gauge filling while it's in.</summary>
        void Hose(float hose, float dt)
        {
            Vector2 end = Vector2.Lerp(HoseHang, Filler, hose);
            Vector2 sag = new Vector2((HoseFrom.x + end.x) / 2f, Mathf.Min(HoseFrom.y, end.y) - 14f);
            for (int i = 0; i < HoseDots; i++)
            {
                float k = i / (HoseDots - 1f);
                Vector2 p = (1 - k) * (1 - k) * HoseFrom + 2 * (1 - k) * k * sag + k * k * end;
                m_Hose[i].rectTransform.anchoredPosition = p * Px;
            }
            m_Nozzle.rectTransform.anchoredPosition = (end - new Vector2(0f, 1.5f)) * Px;
            // the gauge climbs while the hose is in, holds full a moment, and starts again
            if (hose >= 1f) m_Fill += dt / 7f;
            if (m_Fill > 1.25f) m_Fill = 0.25f;
            m_GaugeFill.rectTransform.sizeDelta = new Vector2(4f, 13f * Mathf.Min(1f, m_Fill)) * Px;
            m_GaugeFill.color = m_Fill >= 1f && Fx.Warning.On(m_Clock) ? Gold : new Color(0.3f, 1f, 0.45f);
        }

        /// <summary>The drone, at home over the stands or on the move, with its cable (and a tabk on it).</summary>
        void Drone(DroneShow show)
        {
            DroneShow.Point d = show != null ? show.Drone : DroneShow.Home;
            float bob = show == null || show.Done ? Mathf.Round(Mathf.Sin(m_Clock * 2.4f)) : 0f;
            Vector2 at = new Vector2(d.X, d.Y + bob);
            m_Drone.sprite = Tones2Art.TabkDrone[Fx.Rotor.Step(m_Clock)];
            m_Drone.rectTransform.anchoredPosition = at * Px;
            bool loaded = show?.Carried != null;
            float cable = loaded ? DroneShow.Hang - 8f : 3f; // from under its body to the tabk's roof, or a short hook
            float top = at.y - 8f;
            m_Cable.rectTransform.anchoredPosition = new Vector2(at.x, top - cable) * Px;
            m_Cable.rectTransform.sizeDelta = new Vector2(1f, cable) * Px;
            m_Hook.rectTransform.anchoredPosition = new Vector2(at.x, top - cable - 1f) * Px;
        }

        void You(BayWalker w)
        {
            PlayerSkin skin = Art.SkinFor(m_SoldierDef);
            bool walking = !w.Standing && !w.Riding;
            m_Soldier.Pose(skin, StagePose.Walking(walking, m_Clock));
            m_Soldier.At(w.X, w.Y);
            m_Soldier.Rt.localScale = new Vector3(w.FaceLeft ? -1f : 1f, 1f, 1f);
        }

        void Card(TabkCard card, bool loading)
        {
            Ui.Show(m_Card, card != null);
            if (card == null) return;
            m_CardName.text = card.Name;
            CrewMember crew = Crew.Of(card.Owner);
            Color colour = crew != null ? crew.Card : Gold;
            colour.a = 1f;
            m_CardName.color = colour;
            string guns = "", rounds = "";
            foreach (var g in card.Guns)
            {
                guns += g.name + "\n";
                rounds += g.rounds + "\n";
            }
            m_CardGuns.text = guns;
            m_CardRounds.text = rounds;
            bool isLoaded = m_Garage.Loaded == card && !loading, taken = card == m_Taken && !isLoaded;
            Ui.Show(m_Take, !isLoaded && !taken);
            Ui.Show(m_Loaded, isLoaded || taken);
            m_Loaded.text = taken ? "TAKEN" : "LOADED";
            m_Loaded.color = taken ? Palette.Players[Mathf.Clamp(m_TakenSeat, 0, 1)] : new Color(0.3f, 1f, 0.45f);
            m_TakeLabel.text = loading ? "SKIP" : "TAKE IT";
        }

        /// <summary>The camera follows you along the bay (it's three stages wide), easing.</summary>
        void Camera(float dt, BayWalker w)
        {
            float to = Mathf.Clamp(w.X - StageW / 2f, 0f, BayMap.Width - StageW);
            m_CamX = Mathf.Lerp(m_CamX, to, 1f - Mathf.Exp(-6f * dt));
            if (Mathf.Abs(m_CamX - to) < 0.05f) m_CamX = to;
            m_World.anchoredPosition = new Vector2(-Mathf.Round(m_CamX), 0f) * Px;
        }

        /// <summary>Whether the stands are on screen (their sounds play only then).</summary>
        public bool StandsInView => m_CamX < BayMap.StandX + 40f;
    }
}
