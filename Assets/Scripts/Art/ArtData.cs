namespace PooketSlug
{
    /// <summary>
    /// ASCII pixel art. Rows are top-down, '.' is transparent, every other char is a palette key
    /// (see <see cref="Art"/>). A 1px dark outline is added automatically around opaque pixels.
    /// Character parts live on a shared 24x32 canvas so torso and legs line up when stacked.
    /// </summary>
    public static class ArtData
    {
        // ---- The crew's figure (24x32 canvas, rows start at the given top row): CrewFigure's layers ----
        // A crewmate is a head (their own: hair, face, hats) under a shared body (the torsos below, arms and gun) on
        // shared legs. The bodies leave the head's rows blank where it sits: CrewArt draws the head at HeadTop, nudged
        // as each body frame says (CrewFigure.HeadOn), and the body over it, so an arm can pass in front of the face.

        /// <summary>The canvas row a head's top sits on, unnudged.</summary>
        public const int HeadTop = 9;

        /// <summary>The bandana head everyone wears until their own is drawn, facing forward.</summary>
        public static readonly string[] Head =
        {
            ".........rrrrr..........",
            "........rrrrrrr.........",
            "......RRrrrrrrr.........",
            ".....R..hhsssss.........",
            "........hsssses.........",
            "........hssssss.........",
            ".........SsssS..........",
        };

        /// <summary>Looking down at their work (repairing): eyes lowered. A stand-in until Design draws the crew's.</summary>
        public static readonly string[] HeadDown =
        {
            ".........rrrrr..........",
            "........rrrrrrr.........",
            "......RRrrrrrrr.........",
            ".....R..hhsssss.........",
            "........hssssss.........",
            "........hsssses.........",
            ".........SsssS..........",
        };

        // ---- The shared bodies ----

        public const int TorsoForwardTop = 8;
        public static readonly string[] TorsoForward =
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........cgggggc.........",
            ".......ccgggggcsmmmmmmm.",
            ".......cCgGgggCsMMMmMM..",
            ".......cgGgggg..MM......",
            "........gGgggg..........",
            "........bbbybb..........",
        };

        // ---- Off duty: the den's poses, no gun (the same canvas and rows as TorsoForward) ----

        /// <summary>Standing or walking round the den, arms down.</summary>
        public static readonly string[] TorsoUnarmed =
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........cgggggc.........",
            ".......ccgggggcc........",
            ".......cCgGgggCc........",
            ".......cgGggggCc........",
            ".......sgGgggg.s........",
            "........bbbybb..........",
        };

        /// <summary>Gaming: both hands on a controller (v, V).</summary>
        public static readonly string[] TorsoPad =
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........cgggggc.........",
            ".......ccgggggccc.......",
            ".......cCgGgggCCcss.....",
            ".......cgGgggg..vVvv....",
            "........gGgggg..........",
            "........bbbybb..........",
        };

        /// <summary>Lounging at their station, a hand behind the head (and reaching for the next rung, climbing).</summary>
        public static readonly string[] TorsoLounge =
        {
            "........................",
            "........................",
            "........................",
            "........................",
            ".....css................",
            ".....cc.................",
            ".....cc.................",
            "......cc................",
            ".......ccgggggc.........",
            "........cgggggc.........",
            "........gGgggg..........",
            "........gGgggg..........",
            "........gGgggg..........",
            "........bbbybb..........",
        };

        // ---- Shared moves (the same canvas and rows as TorsoForward): stand-ins until Design draws them ----

        /// <summary>Repairing a tabk, crouched (<see cref="CrewFigure.Repair"/>): the wrench up.</summary>
        public static readonly string[] Repair1 =
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........cgggggc...mm....",
            ".......ccgggggccc.mM....",
            ".......cCgGgggCCcsM.....",
            ".......cgGgggg...M......",
            ".......sgGgggg..........",
            "........bbbybb..........",
        };

        /// <summary>Repairing: the wrench coming down.</summary>
        public static readonly string[] Repair2 =
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........cgggggc.........",
            ".......ccgggggccc.......",
            ".......cCgGgggCCcsmm....",
            ".......cgGgggg....MM....",
            ".......sgGgggg..........",
            "........bbbybb..........",
        };

        /// <summary>Repairing: the wrench on the bolt.</summary>
        public static readonly string[] Repair3 =
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........cgggggc.........",
            ".......ccgggggcc........",
            ".......cCgGgggCcc.......",
            ".......cgGggggCCcs......",
            ".......sgGgggg...mMMm...",
            "........bbbybb..........",
        };

        /// <summary>Repairing: the wrench going back up.</summary>
        public static readonly string[] Repair4 = Repair2;

        public const int TorsoUpTop = 0;
        public static readonly string[] TorsoUp =
        {
            "........................",
            "...............m........",
            "...............mm.......",
            "...............mM.......",
            "...............mM.......",
            "...............mM.......",
            "...............mM.......",
            "...............mMM......",
            "...............mM.......",
            "...............mM.......",
            "...............mM.......",
            "...............sM.......",
            "...............sM.......",
            "................M.......",
            "...............C........",
            "..............cC........",
            "........cgggggc.........",
            ".......ccgggggc.........",
            ".......cCgGgggC.........",
            ".......cgGgggg..........",
            "........gGgggg..........",
            "........bbbybb..........",
        };

        public const int TorsoDownTop = 8;
        public static readonly string[] TorsoDown =
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........cgggggc.........",
            ".......ccgggggcc........",
            ".......cCgGgggCc........",
            ".......cgGggggCs........",
            "........gGgggsMm........",
            "........bbbybMm.........",
            ".............Mm.........",
            ".............Mm.........",
            ".............Mm.........",
            ".............Mm.........",
            ".............Mm.........",
            ".............Mm.........",
            "..............m.........",
        };

        /// <summary>Aiming up and forward, at 45°.</summary>
        public const int TorsoUpDiagTop = 8;
        public static readonly string[] TorsoUpDiag =
        {
            "........................",
            ".....................mm.",
            "....................mM..",
            "...................mM...",
            "..................mM....",
            ".................mM.....",
            "................mM......",
            "...............mMM......",
            "........cgggggsM........",
            ".......ccgggggcs........",
            ".......cCgGgggC.........",
            ".......cgGgggg..........",
            "........gGgggg..........",
            "........bbbybb..........",
        };

        /// <summary>Aiming down and forward in the air, at 45°.</summary>
        public const int TorsoDownDiagTop = 8;
        public static readonly string[] TorsoDownDiag =
        {
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........cgggggc.........",
            ".......ccgggggcc........",
            ".......cCgGgggCsM.......",
            ".......cgGgggg.MmM......",
            "........gGgggg..MmM.....",
            "........bbbybb...MmM....",
            "..................MmM...",
            "...................Mm...",
            "....................m...",
        };

        // ---- Legs (24x32 canvas) ----

        public const int LegsTop = 22;
        public static readonly string[] LegsIdle =
        {
            "........pppppp..........",
            "........ppPppp..........",
            "........ppp.ppp.........",
            "........ppp.ppp.........",
            "........PPp.PPp.........",
            "........PPp.PPp.........",
            "........PPp.PPp.........",
            ".......wwww.wwww........",
            ".......wwwww.wwwww......",
        };

        public static readonly string[] LegsRun1 =
        {
            "........pppppp..........",
            ".......pppPpppp.........",
            "......pppp..ppp.........",
            ".....PPp.....ppp........",
            "....PPp.......ppp.......",
            "...www........PPp.......",
            "..wwww........PPw.......",
            "..ww..........wwwww.....",
            "..............wwwww.....",
        };

        public static readonly string[] LegsRun2 =
        {
            "........pppppp..........",
            "........pppPppp.........",
            "........PPp.ppp.........",
            ".......PPp..ppp.........",
            ".......wwww.ppp.........",
            ".......www..PPp.........",
            ".............PPp........",
            "............wwww........",
            "............wwwww.......",
        };

        public static readonly string[] LegsRun3 =
        {
            "........pppppp..........",
            ".......ppppPppp.........",
            "......ppp...PPp.........",
            ".....ppp.....PPp........",
            "....ppp.......PPp.......",
            "....PPp........www......",
            "....PPw........wwww.....",
            "...wwwww........ww......",
            "...wwwww................",
        };

        public static readonly string[] LegsRun4 =
        {
            "........pppppp..........",
            "........ppPpppp.........",
            "........ppp.PPp.........",
            "........ppp..PPp........",
            "........ppp.wwww........",
            "........PPp..www........",
            ".......PPp..............",
            ".......wwww.............",
            ".......wwwww............",
        };

        public static readonly string[] LegsJump =
        {
            "........pppppp..........",
            "........ppPppppp........",
            ".........pppp.ppp.......",
            "........ppp....PPp......",
            ".......PPp.....PPp......",
            ".......PPw....wwww......",
            "......wwww....wwwww.....",
            "......wwwww.............",
            "........................",
        };

        public const int LegsCrouchTop = 26;
        public const int CrouchDrop = 6;
        public static readonly string[] LegsCrouch =
        {
            ".......ppppppppp........",
            "......pppPPpppppp.......",
            "......PPp.....pppp......",
            ".....wwwP.....wwww......",
            ".....wwww.....wwwww.....",
        };

        /// <summary>Sitting on the couch, side on: thighs forward, shins down (with the torso dropped by <see cref="CrouchDrop"/>).</summary>
        public const int LegsSitTop = 27;
        public static readonly string[] LegsSit =
        {
            "........pppppppppp......",
            "........ppPPpppppPp.....",
            "................PPp.....",
            "...............wwww.....",
            "...............wwwww....",
        };

        // ---- Enemies ----

        public static readonly string[] Drone1 =
        {
            "..................",
            "..zzzzzz..zzzzzz..",
            "........MM........",
            "......dddddd......",
            "....dddddddddd....",
            "...dDDDDDDDDDDd...",
            "...dDDrrDDDDDDd...",
            "...dDDDDDDDDDDd...",
            "....dDDDDDDDDd....",
            ".....MM....MM.....",
            ".....M......M.....",
            "..................",
        };

        public static readonly string[] Drone2 =
        {
            "..................",
            "....zzzz..zzzz....",
            "........MM........",
            "......dddddd......",
            "....dddddddddd....",
            "...dDDDDDDDDDDd...",
            "...dDDrrDDDDDDd...",
            "...dDDDDDDDDDDd...",
            "....dDDDDDDDDd....",
            ".....MM....MM.....",
            ".....M......M.....",
            "..................",
        };

        public static readonly string[] Tank =
        {
            "....................................................",
            "..........................ttttttttt.................",
            "........................ttttttttttttt...............",
            ".......................tttTTTTTTTTtttt..............",
            "..mmmmmmmmmmmmmmmmmmmmtttTTxxTTTTTTttt..............",
            "..MMMMMMMMMMMMMMMMMMMMtttTTxxTTTTTTttt..............",
            "......................ttTTTTTTTTTTTTtt..............",
            "................tttttttttttttttttttttttttt..........",
            "..............ttttttttttttttttttttttttttttttt.......",
            ".............tTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTtt......",
            "............tTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTt.....",
            "...........tTTTTyyTTTTTTTTTTTTTTTTTTTTTTTTyyTTTt....",
            "...........tTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTt....",
            "..........uuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuu...",
            ".........uDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDu..",
            "........uDDdddDDDdddDDDdddDDDdddDDDdddDDDdddDDDDDDu.",
            "........uDdmmmdDdmmmdDdmmmdDdmmmdDdmmmdDdmmmdDDDDDu.",
            "........uDdmMmdDdmMmdDdmMmdDdmMmdDdmMmdDdmMmdDDDDDu.",
            "........uDdmmmdDdmmmdDdmmmdDdmmmdDdmmmdDdmmmdDDDDDu.",
            ".........uDdddDDDdddDDDdddDDDdddDDDdddDDDdddDDDDDu..",
            "..........uuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuu...",
            "....................................................",
        };

        // ---- Projectiles, pickups and small effects ----

        public static readonly string[] Bullet = { "..xxxxW", ".oxxxxx", "..xxxxW" };
        public static readonly string[] HeavyBullet = { "oxxxxxxxW", "oxxxxxxxW" };
        public static readonly string[] EnemyBullet = { ".oo.", "oxxo", "oxxo", ".oo." };
        public static readonly string[] Shell = { ".MMMMm", "MMMMMmz", ".MMMMm" };
        public static readonly string[] Grenade = { "..m..", ".tTt.", "tTTTt", "tTTTt", ".tTt." };
        public static readonly string[] Bomb = { ".mm.", "DDDD", "DDDD", "DDDD", ".DD.", "..r." };

        public static readonly string[] MuzzleFlash =
        {
            "..W....",
            ".xWx.W.",
            "xxWWWxx",
            "oxxWWWx",
            "xxWWWxx",
            ".xWx.W.",
            "..W....",
        };

        public static readonly string[] CrateHeavy =
        {
            "pppppppppppppp",
            "pPPPPPPPPPPPPp",
            "pPrrPPPPPPrrPp",
            "pPrrPPPPPPrrPp",
            "pPrrPPPPPPrrPp",
            "pPrrrrrrrrrrPp",
            "pPrrrrrrrrrrPp",
            "pPrrPPPPPPrrPp",
            "pPrrPPPPPPrrPp",
            "pPrrPPPPPPrrPp",
            "pPPPPPPPPPPPPp",
            "pppppppppppppp",
        };

        public static readonly string[] CrateBombs =
        {
            "pppppppppppppp",
            "pPPPPPPPPPPPPp",
            "pPGGGGGGGGPPPp",
            "pPGGPPPPPGGPPp",
            "pPGGPPPPPGGPPp",
            "pPGGGGGGGGPPPp",
            "pPGGPPPPPGGPPp",
            "pPGGPPPPPGGPPp",
            "pPGGGGGGGGPPPp",
            "pPPPPPPPPPPPPp",
            "pPPPPPPPPPPPPp",
            "pppppppppppppp",
        };

        public static readonly string[] Medkit =
        {
            "WWWWWWWWWWWW",
            "WWWWWrrWWWWW",
            "WWWWWrrWWWWW",
            "WWWrrrrrrWWW",
            "WWWrrrrrrWWW",
            "WWWWWrrWWWWW",
            "WWWWWrrWWWWW",
            "WWWWWWWWWWWW",
            "zzzzzzzzzzzz",
        };

        public static readonly string[] OneUp =
        {
            "tttttttttttttt",
            "tTTTTTTTTTTTTt",
            "tTTWTWTWTWWTTt",
            "tTWWTWTWTWTWTt",
            "tTTWTWTWTWWTTt",
            "tTTWTWTWTWTTTt",
            "tTTWTWWWTWTTTt",
            "tTTTTTTTTTTTTt",
            "tttttttttttttt",
        };

        public static readonly string[] Heart =
        {
            ".rr.rr.",
            "rWrrrrr",
            "rrrrrrr",
            "rrrrrrr",
            ".rrrrr.",
            "..rrr..",
            "...r...",
        };

        public static readonly string[] Flag =
        {
            "mrrrrrrr",
            "mrrWWrrr",
            "mrWrrWrr",
            "mrrWWrrr",
            "mrrrrrrr",
            "m.......",
            "m.......",
            "m.......",
            "m.......",
            "m.......",
            "m.......",
            "m.......",
            "m.......",
            "m.......",
            "M.......",
            "M.......",
        };
    }
}
