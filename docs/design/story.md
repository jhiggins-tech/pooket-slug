# Story, plot and cutscenes

The story of Pooket Slug, how it unfolds across the missions, and how cutscenes and reveals are shown.

**Status:** signed off by the owner, 2026-10-14.

**Read this when** you're writing a mission's story, a cutscene, or anything that reveals something new to the player.

Up: [Design](README.md)

## The premise

- **The crew are missing, and only tones2 is left.** kie's Trollogram holograms have gone rogue. They raid the crew's hangout while tones2 is out on a snack run, and he comes back to an empty room and glitchy hologram soldiers everywhere.
- **What's really going on comes out as you play.** The holograms have stolen the crew's tanks, and each tank is driven by a hologram copy of its owner. The game keeps this to itself until the first tank appears ([reveals](#reveals-are-story-beats)).
- **Every mission has the same shape.** You fight through to a crewmate's tank, beat it, then beat its fake pilot. The fake glitches out, and the real crewmate is freed to play from then on.

## Tone

- **Arcade action, played straight:** hordes of goons, big explosions, and a boss at the end of every mission.
- **The jokes come from the crew:** their weapons, their catchphrases and in-jokes from Pooket Tabks. kie's weasels pop to Pop Goes the Weasel, and the hologram goons flicker and glitch.
- **Short and punchy.** It's made for phones, so there's never much to read.

## The plot

1. **The opening:** a short scene. tones2 comes back from a snack run to find the crew's hangout raided and empty, and heads in alone. Nothing in it mentions tanks.
2. **Mission one, the military base:** tones2 fights through the base to the motor pool. There comes the reveal: the crew's stolen tanks are parked inside, and kie's tank rolls out to fight, with a hologram kie at the controls. Beat the tank, then hologram kie on foot, and the real kie is freed.
3. **The first drive:** kie's tank is yours now. A short drive out of the base ends the mission and teaches you the tank's controls.
4. **Two of the crew:** with kie back, co-op opens. kie's first line sets it up as the menu changes.
5. **Mission two frees kcaj.** Each mission after that frees one more crewmate from their own tank.
6. **The coming-soon crew** (shotdownboyz, lankcity, odsey and doctorfox) arrive later, each with a mission of their own.
7. **There's no finale for now.** The story stays open-ended and grows one crewmate at a time, like Coming soon in Tabks.

## Cutscenes

Cutscenes are **in-engine scenes**: the game's own art, drawn in code, with speech bubbles. There's no video.
- **Each lasts a few seconds.** Tap to move on. Once you've seen a scene you can skip it, but a reveal always plays the first time.
- **There are five kinds:**
  - **Briefing**, before a mission: where you're going, and who's held there.
  - **Boss intro:** a warning banner, the music changes, and the tank makes its entrance ([tanks](tanks.md#the-boss-intro)).
  - **Reveal:** the first time something new appears. It plays once, ever.
  - **Freeing:** the fake pilot glitches out, and the real crewmate appears and joins.
  - **Debrief:** the mission's tally, XP and rank, then anything newly unlocked.
- **Sound:** there's a music track for each mission, and one for bosses. The crew talk in speech bubbles, and an arcade announcer calls the big moments ("Mission complete!"), in a generated voice ([tanks](tanks.md#the-boss-intro)).
- **In co-op,** scenes play for both players, and a scene ends once both have tapped. A partner who hasn't seen it gets to watch it.

## Reveals are story beats

The game hides what's coming ([progressive disclosure](README.md#progressive-disclosure-the-thread-through-all-four)), so each reveal gets a scene of its own:

| Reveal | When | How |
|---|---|---|
| Tanks exist | The motor pool, at the end of Mission one | kie's tank rolls out, with its boss intro |
| You can drive one | Straight after kie is freed | You climb into his tank and drive it out of the base |
| Co-op exists | Once Mission one has been beaten solo | The menu changes, and kie's first line sets it up |
| Each new crewmate | The end of their mission | The freeing scene |
| XP and rank | The first debrief | The first XP, and the first step up the ladder |

The second time you meet something, it's quick: a tank you've already fought only gets its boss intro.

## Open questions

- **Mission order after kcaj**, which sets the order of the story.
- **Each mission's story:** its briefing, and how its crewmate's freeing plays out.
