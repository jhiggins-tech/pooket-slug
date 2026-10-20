# kie's tabk

THE PROJECTIONIST, kie's own tabk: its theme, paint, guns, name and sprite.

**Read this when** you're changing kie's tabk.

Up: [Tabks](../tanks.md)

THE PROJECTIONIST: the second tabk you win, kie's own, back after Mission one (owner, 2026-10-20; it was after Mission two; theme owner, 2026-10-19, kie-tabk-design; page https://claude.ai/artifact/Hp8MXScfoogFsPouTpdexn).

- **The theme** (owner picked the projector, 2026-10-19): the boss tabk was wrecked in Mission one, and kie rebuilds it around the Trollogram projector that made Trollogram kie, pulled from the wreck. Trollograms run hot, so he wraps it in copper freezer pipes and it comes back frosted over, patched with welded plates and a few rust spots. The projector is the turret, its short fat lens the gun, and two film reels on top spin while a shot charges. Its return is a den beat ([kie's tabk comes home](../cutscenes.md#kies-tabk-comes-home)).
- **Paint** (owner, 2026-10-19): kie's blue (#4ea8ff) with a green (#46d27a) stripe on the hull and green on the reels' spokes, from his Pooket Tabks colours. The lens glows warm gold, the friendly colour, never cyan (cyan is the villains'). The reference painting is blue take 2 (`/mnt/project-files/concepts/kie-tabk/proj-blue-2.png`).
- **Its own guns:** kie's kit, tabk-sized, like tones2's; rounds 5, 3 and 1, never refilled, and the same controls ([driving a tabk](driving.md#driving-a-tabk)).
  - **Weasel Pop** (FIRE, 5 rounds): weasels tumble out of the lens, lobbed, and scurry under goons to pop, as on foot.
  - **Trollogram** (SP, 3 rounds): the lens beams a Trollogram of the tabk, a decoy goons shoot at.
  - **Steal** (SU, 1 round): the roulette spins on the projector and lifts a power-up off the toughest enemy on screen, as on foot.
  - How each is tuned is for its build card; these follow kie's kit on foot ([kits](../kits.md#kie)).
- **Its name** on screen while you drive it: KIE'S TABK, over its rounds (WEASEL 5, TROLLOGRAM 3, STEAL 1).
- **The sprite** is 52 × 28, six rows taller than the tabk's 52 × 22 for the reels: the tabk's own hull and tracks, the projector body over them with frost on top and a copper coil and pipe behind, the lens in front and the two reels above. Its source (the rows and a renderer) is in `/mnt/project-files/concepts/kie-tabk/sprite/`, for the build card to put in `ArtData`.
- **Mission one's boss stays the plain olive tabk** with Trollogram kie at the controls ([tabk bosses](bosses.md#tabk-bosses)); this one is clearly kie's own.
- **In the bay** it parks downstairs once it's back, after Mission one, and the drone swaps it onto the repair stands when you pick it ([the tabk bay](../lobby.md#the-tabk-bay)).
- **Built so far** (kcaj-tabk-build, 2026-10-20): drivable when it's loaded in the bay, solo and co-op. The sprite is Design's two frames in `Crew/Kcaj/KcajTabkArt.cs` (the second while a volley or the beam is going), the nozzle cut out of them to turn at its pivot. Guns: `Crew/Kcaj/KcajTabk.cs`, numbers and rules in `Core/Crew/KcajTabkSpec.cs` (tested). FIRE wobbles the swirl, then pipes three pairs of his cones along the aim, faster and further than his. SP is his beam from the nozzle, longer and ticking hotter; a beam that touches nothing keeps its round, and the tabk stands still while it holds. SU rains his pills, the same count as his, harmless to the crew; the rain is the world's, so it keeps falling if the tabk's ejected or blown. A partner's screen draws it in its own look and plays its beam and pills from it (`KcajCopy`, in its tabk mode). `?test=softserve` parks one at the start.
- **Built so far** (kie-tabk-build): drivable when it's loaded in the bay, solo and co-op (a partner's screen draws it in its own look, from the tabk's pose). The sprite is Design's rows in `Crew/Kie/KieTabkArt.cs`, the lens turning at the projector's front, the reels a second frame. Guns: `Crew/Kie/KieTabk.cs`, numbers and rules in `Core/Crew/KieTabkSpec.cs` (tested). FIRE spins the reels for a moment, then lobs five weasels (his are three), bigger pops; with no room for a volley the round isn't spent. SP spins the reels and beams a Trollogram of the whole tabk 3.6 ahead, on screen, that goons shoot instead and that blows up bigger than his own (it can't be swapped with). SU is his Steal; the loot is his, and while he drives FIRE shoots it from the lens until it's spent (the HUD shows it in the first gun's place). `?test=kietabk` parks one at the start.
