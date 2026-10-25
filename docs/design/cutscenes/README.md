# Cutscenes

Every cutscene in Pooket Slug, written as a TV-style production script: what happens on screen, who says what, the
narrator, the cards, the sounds and how long each shot runs. It's the one place cutscenes are designed. Other
design docs link here and don't repeat them.

**Status:** the scenes keep the sign-offs they had in story.md, tanks.md and lobby.md (noted on each). Moved into
one script on 2026-10-19 (cutscenes-script).

**Read this when** you're writing or changing a cutscene, a line, a narrator recording, or anything a scene shows.

The scenes are on a page per part: [Mission one](mission-one.md), [the den](den.md), [Mission two](mission-two.md),
[the skits](skits.md), [the retired scene](retired.md) and [the co-op check](coop-check.md). This page has the rules every scene follows.

Up: [Design](../README.md) · The story around the scenes: [story](../story.md) · How the game plays a scene:
[writing the cutscenes](../../scenes.md)

## How to read the script

**This script leads.** Each scene's words, shots and timings are decided here. The game plays a scene from its text
file in `scenes/` ([writing the cutscenes](../../scenes.md)), which adds the staging an engine needs (positions, eases,
props). When a scene here changes, its scene file is changed to match, and that goes live without a build.

Each scene opens with a short note: when it plays, how long it runs, whether it can be skipped, whose
[character intro](#character-intros) it carries, and its scene file. Then comes the script, laid out like a
production script:

| In the script | What it is |
|---|---|
| `1. INT. THE HANGOUT - NIGHT` | A shot: its number, inside or outside, the place, the time of day. Its length is on the right. |
| Plain paragraphs | The action: what we see and hear. A character's name is in capitals the first time they're on screen in a scene. |
| `TONES2`, then the line | A speech bubble. Bubbles are in the game's capitals, a few words each. |
| `NARRATOR (V.O.)` | The narrator: subtitles along the bottom and read out loud. The recording's id is in square brackets. |
| `ANNOUNCER (V.O.)` | The arcade announcer, from the vendor repository's `voice/lines.json`. |
| `(GLITCHY)` | How a line is said or shown: a Trollogram's bubble flickers. |
| `INTRO:` | A [character intro](#character-intros) and the second it lands. |
| `SUPER:` | Text on screen: the caption card, a banner or a name card. |
| `SFX:` / `MUSIC:` | A sound cue, and what the music does. |
| `CUT TO:` / `INTO:` | How a shot ends: a cut to the next, or straight into play. |

The narrator says "tanks" where the subtitles say "tabks", and says kie as "Kye" and kcaj as "Kadge", so the
recordings' own text in `voice/narration.json` is spelled that way. All the lines are placeholders for the owner to
rewrite in the crew's own voice.

### Every scene at a glance

| Scene | Plays | About | Scene file | State |
|---|---|---|---|---|
| [The opening](mission-one.md#the-opening) | Mission one starts | 49 s | `opening.txt` | Built, eight shots (2026-10-19) |
| [The motor pool reveal](mission-one.md#the-motor-pool-reveal) | The first time tones2 reaches the motor pool | 24 s | `motor-pool.txt` | Built |
| [The boss intro](mission-one.md#the-boss-intro) | Every boss tabk's arrival | 3 s | none: in the game itself (`BossIntroShow`) | Built |
| [Freeing kie](mission-one.md#freeing-kie) | Trollogram kie beaten on foot | 25.5 s, after the glitch-out in play | `freeing-kie.txt` (shots 2 to 5) | Built |
| [The first debrief](mission-one.md#the-first-debrief) | After every win, solo or co-op | 12 s | none: a screen (`DebriefView`) | Built |
| [The first visit](den.md#the-first-visit) | The den's first opening, once ever | 4 s | none: in the den itself (`DenBeats`) | Built (2026-10-19) |
| [A crewmate moves in](den.md#a-crewmate-moves-in) | The den, after each freeing | 3 s | none: in the den itself (`DenBeats`) | Built (2026-10-19) |
| [kie's tabk comes home](den.md#kies-tabk-comes-home) | The tabk bay, the first visit after Mission one | about 12 s | none: in the bay itself (`KieHomecoming`) | Built (kie-tabk-home-m1, 2026-10-20) |
| [The Mission two briefing](mission-two.md#the-mission-two-briefing) | Mission two starts | 26 s | `m2-briefing.txt` | Built, narrator voiced |
| [kcaj's tabk arrives](mission-two.md#kcajs-tabk-arrives) | The first time the players reach the cold store | 12 s | `scenes/kcaj-tabk.txt`, then the boss intro | Built (m2-finish): solo the first time; in co-op, together at each run's first boss wake (the phones' saves can differ); where it doesn't play (a test start, or a solo fight once it's been seen, until Mission two is cleared) the boss intro ends with Trollogram kcaj's TROLLO-KCAJ band at the hatch |
| [Freeing kcaj](mission-two.md#freeing-kcaj) | Trollogram kcaj beaten on foot | 23.5 s, after the glitch-out in play | `scenes/freeing-kcaj.txt` (shots 2 to 5) | Built (m2-finish), on both phones in co-op, on Design's loading-yard plate |
| [Skits](skits.md) | Once each: in the den, the tabk bay, Mission one's coda and the debrief, after Mission one and after Mission two | 3 to 18 s | none: in the den, the bay, the debrief or play itself | Seven scripts (skit-scripts, 2026-10-25), all picked or workshopped by the owner. Two of us now is where the den's co-op button first appears; its new label, MULTI-PLAYER on two lines, comes from the owner (Jack, 25 Oct) |
| [Two of the crew](retired.md#retired-two-of-the-crew) | Retired | 8 s | none | Retired, 2026-10-18 |
| [The co-op check](coop-check.md#the-co-op-check) | `?test=scene` and CI only | 36 s | `coop-test.txt` | A test, not the story |

## How every cutscene plays

Cutscenes are **in-engine scenes**: the game's own art, drawn in code, with speech bubbles. There's no video.

- **Each runs a few seconds a shot.** A tap moves to the next shot, and each moves on by itself too. Once you've seen
  a scene you can skip it, but a reveal always plays in full the first time.
- **The kinds:** a **briefing** before a mission (where you're going, and who's held there); a **boss intro**, the
  tabk's entrance; a **reveal**, the first time something new appears, once ever; a **freeing**, where the fake pilot
  glitches out and the real crewmate appears and joins; and the **debrief**, the mission's tally, XP and rank.
- **Shots hold long enough to read** (about three words a second). Bubbles are a few words each, in the game's
  capitals, so they read at a glance on a phone; the narrator's subtitles are in sentence case.
- **A narrator tells each scene,** in subtitles along the bottom of the screen and out loud (turned well down, to
  sit with the game's sounds), so what's happening is never left to guesswork. The owner asked for this after trying
  the first cut of the opening, 2026-10-15. The voice is a camp, softly spoken, Louis Theroux-style documentary
  narrator (voice F from the narrator voice spike, MiniMax, picked by the owner 2026-10-16), made by the vendor
  repository's Narration workflow from its `voice/narration.json`. A line made once is kept, so a new or changed line
  is the only one made again.
- **A narrator line always gets its whole voice,** plus a breath, before the next line or the end of the shot; if a
  re-recorded line runs longer, the shot waits for it (up to 3 seconds).
- **Bubbles never sit on top of each other:** comic-book style, a later one stacks above the ones already up, or
  below them when there's no room left above.
- **Sound:** each scene uses the music around it ([music](../music.md)); the crew talk in bubbles, and the announcer
  calls the big moments in a generated voice. The music ducks under any recorded line.
- **In co-op,** scenes play for both players, on both phones, and a shot moves on early only once both have tapped
  (the timer still moves it on for both). A line says who's being waited for. A scene seen before skips only once both
  players have pressed SKIP, so a partner who hasn't seen it gets to watch it; a partner who leaves holds nobody up. A
  scene plays in co-op when the players start together from the den; someone joining a game already under way drops
  straight in. Mission one is single player only, so its scenes are always solo.
- **No strobing** ([the design's rule](../README.md)): an alarm is a slow wash, once a second.

### Character intros

**Every character gets an intro the first time they're on screen** (canonical, the owner, 2026-10-16): the camera
crash-zooms onto them, their name slams up big ("TONES2"), time goes slow-mo, and a big musical hit plays. It's part
of that scene, so it plays whenever the scene does, and never anywhere else. One exception is built today:
when [kcaj's tabk arrives](mission-two.md#kcajs-tabk-arrives) doesn't play, the boss intro ends with Trollogram kcaj's
band at the hatch instead.

**How it plays:** it builds up, then it lands, like the run-up to a drop. The owner asked for the build-up after the
first cut came "out of nowhere, like a car crash", 2026-10-16.
- **First we see them.** The character is on screen for a few seconds before anything happens, so the intro has
  someone to land on.
- **The build-up,** about 2.5 seconds: a chip riser climbs (a snare roll that speeds up from quarter notes to a blur,
  over a noise sweep and a rising tone, all swelling), while the camera creeps in on them, the scene darkens a little,
  and it shakes as the riser peaks. A tap before it jumps to the build-up's start, and a tap during it does nothing, so
  it always plays whole.
- **The landing,** about 1.8 seconds, at the riser's peak: a white flash and a crash zoom onto the character, so they
  fill the left of the screen; their name slams in on a slanted band to their right, in the caption card's gold; the
  scene runs at slow-mo underneath (its bubbles, cues and snow all slow down); a chip musical hit rings out (a wide
  chord over a crash and a bass drop). Then it eases back out and the shot carries on at full speed. A tap can't cut it
  short.
- **Levels:** the riser and the hit sit at the level of the game's other sounds, not above them.
- A scene that can be skipped once seen skips its intro with it. The narrator waits for the intro to land before
  speaking, and their subtitles carry on through it.

**What counts:** named characters, the crew and their Trollogram copies. Each Trollogram copy is a character of its
own (it's that mission's boss), so a crewmate gets two intros: their Trollogram where it first appears, then the real
one when they're freed. Nameless enemies (the Trollogram goons) and vehicles don't get one; a tabk gets its name card
in [the boss intro](mission-one.md#the-boss-intro) instead.

| Character | Scene | Shot | Name on screen |
|---|---|---|---|
| tones2 | [The opening](mission-one.md#the-opening) | 1, as he strolls in with the snacks | TONES2 |
| Trollogram kie | [The motor pool reveal](mission-one.md#the-motor-pool-reveal) | 3, as he pops up in the hatch | TROLLO-KIE |
| kie | [Freeing kie](mission-one.md#freeing-kie) | 2, as the real kie drops to the floor | KIE |
| Trollogram kcaj | [kcaj's tabk arrives](mission-two.md#kcajs-tabk-arrives), shot 2 | As he rises out of the hatch | TROLLO-KCAJ |
| kcaj | [Freeing kcaj](mission-two.md#freeing-kcaj) | 2, as he steps out of the frost | KCAJ |
| Each later crewmate's Trollogram | Their mission's reveal, before its [boss intro](mission-one.md#the-boss-intro) | As the pilot shows in the hatch | TROLLO-&lt;NAME&gt; |
| Each later crewmate | Their freeing scene | As the real one appears | &lt;NAME&gt; |

The debrief and the den's beats bring in nobody new, so they have no intro.
