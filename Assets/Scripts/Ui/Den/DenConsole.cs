using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.DenRoom;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The den's TV and buttons: in the room, the console's start screen on the TV (the mission, the room code and its
    /// invite, both controllers' lights, the countdown), the mission board's card, the loose controllers, the seats'
    /// P1 / P2 tags and the empty seat's + MATE; over it, BACK, READY, the name card with its ⓘ, the hint along the top
    /// and its notes.
    /// </summary>
    sealed class DenConsole
    {

        readonly RectTransform m_World, m_Stage;
        readonly DenCrew m_Crew;
        readonly Image[] m_Lights = new Image[2], m_LoosePads = new Image[2];
        readonly Text[] m_Tags = new Text[2];
        Text m_TvTitle, m_TvRoom, m_TvLights, m_TvCount, m_Board, m_Hint, m_Note;
        Button m_Ready, m_Back, m_HostPad, m_KitButton;
        Text m_ReadyLabel, m_CardName;
        RectTransform m_Card;
        string m_TvSaid;
        float m_TvSaidUntil, m_NoteUntil, m_Clock;

        public DenConsole(RectTransform world, RectTransform stage, DenCrew crew)
        {
            m_World = world;
            m_Stage = stage;
            m_Crew = crew;
        }

        Image Picture(string name, float x, float y, float w, float h, Color color) => StageKit.Picture(m_World, name, null, x, y, w, h, color);

        Text WorldLabel(string name, float x, float y, int size, TextAnchor align, Color color) => Label(m_World, name, x, y, size, align, color);

        // ------------------------------------------------------------------ made in drawing order (DenView)

        /// <summary>In the room: the TV's words, the board's, the sign, the console's lights, the loose controllers and the tags.</summary>
        public void MakeTv()
        {
            m_TvTitle = WorldLabel("TV title", TvCentre.x - 24f, TvCentre.y + 13f, 10, TextAnchor.UpperLeft, Gold);
            m_TvRoom = WorldLabel("TV room", TvCentre.x - 24f, TvCentre.y + 5f, 7, TextAnchor.UpperLeft, new Color(0.67f, 0.86f, 1f));
            m_TvLights = WorldLabel("TV lights", TvCentre.x - 24f, TvCentre.y - 9f, 8, TextAnchor.LowerLeft, Color.white);
            m_TvCount = WorldLabel("TV countdown", TvCentre.x, TvCentre.y, 30, TextAnchor.MiddleCenter, Gold);
            m_Board = WorldLabel("Mission board", Board.x, Board.y + 1f, 6, TextAnchor.MiddleCenter, Paper); // light ink on the painted brown board
            for (int i = 0; i < 2; i++)
            {
                m_Lights[i] = Picture("Port light", Ports.x + i * 8f, Ports.y, 2f, 2f, Color.black);
                m_LoosePads[i] = Picture("Controller", SeatX[i], SeatY, 6f, 3f, Pad);
                m_Tags[i] = WorldLabel("Tag", SeatX[i], DenMap.FloorY(DenFloor.Ground) + 2f, 9, TextAnchor.LowerCenter, Palette.Players[i]);
                m_Tags[i].text = "P" + (i + 1);
            }
        }

        /// <summary>Over the room: BACK top left, READY bottom right, the name card bottom left, the empty seat's invite, the hint and notes.</summary>
        public void MakeButtons(System.Action back, System.Action ready, System.Action kit, System.Action host)
        {
            Vector2 half = new Vector2(StageW, StageH) * Px / 2f;
            m_Back = Ui.Button(m_Stage, "BACK", new Vector2(-half.x + 40f, half.y - 18f), new Vector2(64f, 22f), back, out Text backKey);
            backKey.text = "ESC";
            m_Ready = Ui.Button(m_Stage, "READY", new Vector2(half.x - 56f, -half.y + 32f), new Vector2(96f, 30f), ready, out Text readyKey);
            readyKey.text = "ENTER";
            m_ReadyLabel = m_Ready.GetComponentInChildren<Text>();
            m_ReadyLabel.fontSize = 16;
            m_Card = Ui.Rect(m_Stage, "Name card", Ui.Middle, Ui.Middle, new Vector2(-half.x + 70f, -half.y + 30f), new Vector2(124f, 28f));
            var card = m_Card.gameObject.AddComponent<Image>();
            card.color = Band;
            card.raycastTarget = false;
            m_CardName = Ui.Label(m_Card, "Name", new Vector2(0f, 0.5f), new Vector2(10f, 0f), 14, TextAnchor.MiddleLeft, Gold);
            m_KitButton = Ui.Button(m_Card, "i", new Vector2(46f, 0f), new Vector2(22f, 22f), kit, out Text kitKey);
            m_KitButton.GetComponent<Image>().sprite = Ui.Disc;
            m_KitButton.GetComponentInChildren<Text>().fontStyle = FontStyle.BoldAndItalic;
            kitKey.text = ScreenWords.KitKey; // the same key the pause menu shows
            Vector2 p2 = new Vector2(SeatX[1] - StageW / 2f, SeatY + 9f - StageH / 2f) * Px;
            m_HostPad = Ui.Button(m_World, "+ MATE", p2, new Vector2(52f, 16f), host, out Text hostKey); // on the couch, so it moves with the camera
            m_HostPad.GetComponentInChildren<Text>().fontSize = 9;
            hostKey.text = "H";
            m_Hint = Ui.Label(m_Stage, "Hint", Ui.TopCenter, new Vector2(0f, -8f), 10, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.75f));
            m_Note = Ui.Label(m_Stage, "Note", Ui.TopCenter, new Vector2(0f, -24f), 11, TextAnchor.UpperCenter, new Color(1f, 0.55f, 0.45f));
        }

        /// <summary>The TV says this under the room code for a few seconds ("LINK COPIED").</summary>
        public void TvSay(string text, float clock)
        {
            m_TvSaid = text;
            m_TvSaidUntil = clock + 2.5f;
        }

        /// <summary>Says something across the top for a few seconds (a pick undone, say).</summary>
        public void Note(string text, float seconds, float clock)
        {
            m_Note.text = text;
            m_NoteUntil = clock + seconds;
        }

        // ------------------------------------------------------------------ each frame

        /// <summary>The TV and buttons as the den stands (<paramref name="clock"/> is the den's).</summary>
        public void Refresh(Den den, DenInfo info, float clock)
        {
            m_Clock = clock;
            Tv(den, info);
            Buttons(den, info);
        }

        void Tv(Den den, DenInfo info)
        {
            bool counting = den.Countdown > 0f;
            m_TvTitle.text = info.Mission ?? "CREW";
            string invite = m_Clock < m_TvSaidUntil ? m_TvSaid : "TAP TO INVITE";
            m_TvRoom.text = info.Room != null ? info.Room + (info.InviteLink != null ? "  " + invite : "")
                : den.Mode == DenMode.Crew ? "PICK WHO YOU'LL PLAY" : "SOLO";
            Ui.Show(m_TvCount, counting);
            if (counting) m_TvCount.text = Mathf.CeilToInt(den.Countdown).ToString();
            m_Board.text = info.Mission ?? "";
            bool partnerSeat = den.Mode == DenMode.Coop && den.Partner.InDenNow && den.Partner.Seated;
            for (int seat = 0; seat < 2; seat++)
            {
                bool mine = seat == den.Seat, filled = mine ? den.Pick != null : partnerSeat || m_Crew.VisitorOn(seat);
                bool ready = mine ? den.Ready : partnerSeat && den.Partner.Ready;
                m_Lights[seat].color = ready ? new Color(0.3f, 1f, 0.45f) : filled ? Palette.Gold : new Color(0.2f, 0.18f, 0.25f);
                m_LoosePads[seat].enabled = !filled;
                Ui.Show(m_Tags[seat], filled && (den.Mode == DenMode.Coop || seat == 0));
                m_Tags[seat].color = Palette.Players[seat]; // the tags say who's who: everyone wears their own colours
            }
            string l1 = den.Ready ? "READY" : den.Pick != null ? "IN" : "--"; // the console font has no dots
            string l2 = partnerSeat ? (den.Partner.Ready ? "READY" : "IN") : "--";
            string mineTag = "P" + (den.Seat + 1), theirTag = "P" + (2 - den.Seat);
            string start = StartLine(den) + (info.Watchers > 0 ? "  " + info.Watchers + " WATCHING" : "");
            m_TvLights.text = den.Mode == DenMode.Crew ? "" : den.Seat == 0 ? $"{mineTag} {l1}   {theirTag} {l2}\n{start}" : $"{theirTag} {l2}   {mineTag} {l1}\n{start}";
        }

        static string StartLine(Den den)
        {
            if (den.Zoom >= 0f) return "GO!";
            if (den.Countdown > 0f) return "GET READY";
            if (den.Ready) return den.Mode == DenMode.Coop && den.Partner.InDenNow ? "WAITING FOR P" + (2 - den.Seat) : "GO!";
            return "PRESS START";
        }

        void Buttons(Den den, DenInfo info)
        {
            bool zooming = den.Zoom >= 0f;
            Ui.Show(m_Ready, den.Mode != DenMode.Crew && !zooming);
            m_ReadyLabel.text = den.Ready ? "UNREADY" : "READY";
            Ui.Show(m_Back, !zooming);
            Ui.Show(m_Card, den.Pick != null && !zooming);
            if (den.Pick != null) m_CardName.text = den.Pick.Name.ToUpperInvariant();
            Ui.Show(m_HostPad, info.CanHost && den.Mode != DenMode.Coop && !zooming);
            Ui.Show(m_Hint, !zooming);
            m_Hint.text = den.Pick == null ? "TAP SOMEONE TO PLAY THEM"
                : den.Ready ? ""
                : den.Mode == DenMode.Crew ? "TAP TO WALK · TAP SOMEONE TO SWAP · THE LADDER GOES UP"
                : "TAP TO WALK · THE COUCH IS READY · THE LADDER GOES UP";
            Ui.Show(m_Note, m_Clock < m_NoteUntil);
        }
    }
}
