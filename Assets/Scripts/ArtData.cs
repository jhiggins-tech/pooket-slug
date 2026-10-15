namespace PooketSlug
{
    /// <summary>
    /// ASCII pixel art. Rows are top-down, '.' is transparent, every other char is a palette key
    /// (see <see cref="Art"/>). A 1px dark outline is added automatically around opaque pixels.
    /// Character parts live on a shared 24x32 canvas so torso and legs line up when stacked.
    /// </summary>
    public static class ArtData
    {
        // ---- Character torsos (24x32 canvas, rows start at the given top row) ----

        public const int TorsoForwardTop = 8;
        public static readonly string[] TorsoForward =
        {
            "........................",
            ".........rrrrr..........",
            "........rrrrrrr.........",
            "......RRrrrrrrr.........",
            ".....R..hhsssss.........",
            "........hsssses.........",
            "........hssssss.........",
            ".........SsssS..........",
            "........cgggggc.........",
            ".......ccgggggcsmmmmmmm.",
            ".......cCgGgggCsMMMmMM..",
            ".......cgGgggg..MM......",
            "........gGgggg..........",
            "........bbbybb..........",
        };

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
            ".........rrrrr.mM.......",
            "........rrrrrrrmM.......",
            "......RRrrrrrrrsM.......",
            ".....R..hhssssssM.......",
            "........hsssses.M.......",
            "........hssssssC........",
            ".........SsssScC........",
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
            ".........rrrrr..........",
            "........rrrrrrr.........",
            "......RRrrrrrrr.........",
            ".....R..hhsssss.........",
            "........hsssses.........",
            "........hssssss.........",
            ".........SsssS..........",
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
            ".........rrrrr.......mm.",
            "........rrrrrrr.....mM..",
            "......RRrrrrrrr....mM...",
            ".....R..hhsssss...mM....",
            "........hsssses..mM.....",
            "........hssssss.mM......",
            ".........SsssS.mMM......",
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
            ".........rrrrr..........",
            "........rrrrrrr.........",
            "......RRrrrrrrr.........",
            ".....R..hhsssss.........",
            "........hsssses.........",
            "........hssssss.........",
            ".........SsssS..........",
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
