# The lobby: the gaming den

Where you pick your soldier and ready up, solo and in co-op: the inside of the crew's hangout, with a station for every freed crewmate, who lives there between missions.

**Status:** approved by the owner, 2026-10-16 (second pass): the owner liked the first ("the bits feel good. I like the writing") and asked for the big screen, a GameCube-style console, and picking by sitting on the couch. First pass from the owner's note on the Roadmap: "The lobby should be the interior of the gaming den base, every playable character has a station and occupies it once they become playable. Small animation cycles of them interacting with their stations or with other players on the screen, sometimes with funny dialogue". All the lines are placeholders for the owner to rewrite in the crew's own voice.

**Read this when** you're building the lobby, the crew screen, or a new crewmate's station.

Up: [Design](README.md)

Concept: `concepts/lobby/den-lobby.png` in the project's files (second pass; the first is `den-lobby-v1.png`).

## The room

- **It's the hangout from [the opening](story.md#the-opening-scene),** the garage with the hand-painted POOKET sign, now cleaned up and lived in. The raid's cyan scorch marks are still on the walls, painted over badly. It's warm where Mission one is cold: lamp light, string lights, screens glowing.
- **One phone screen, side-on, in two floors:** the ground floor and a loft up a ladder, four stations on each. It fits a landscape phone without scrolling, so every soldier is one tap away.
- **The heart of the room is the big screen and the couch.** A big TV on the ground floor, a GameCube-style console under it (a little purple cube with a carry handle, unbranded) and two controllers on long cables, lying on the couch's two seats. The couch is where the players sit: **sitting on it is how you pick your soldier** ([below](#picking-a-soldier-and-getting-ready)).
- **The way out is the roller door** on the right. Crew come and go through it (a freed crewmate moving in, a partner arriving), and it's where BACK walks you out.
- **The mission board** hangs beside the door: a corkboard with the chosen mission's card pinned to it. Tapping it opens the mission screen.
- **The TV is the room's screen** for anything the lobby has to say: the console's start screen with the chosen mission, the room code, who's watching, the countdown. It's the gaming setup from the opening, plugged back in.
- **Thumbs stay clear:** READY sits bottom right, BACK top left, and no station's tap area reaches into the bottom corners ([controls](player.md#controls)).

### Who lives where

Each station is built from the soldier's kit ([kits](kits.md)), so it shows what they do before they're picked.

| Floor | Crewmate | Station | Idle cycles |
|---|---|---|---|
| Ground | **tones2** | A beanbag and a slushie machine beside the couch (the couch itself is everyone's: it's where the players sit) | Slurps the slushie and gets brain freeze; sprays the slushie machine clean with a bit of ten-1; leans over the back of the couch to watch the TV |
| Ground | **kie** | A workbench with the Trollogram projector, half taken apart, and a weasel hutch | Tinkers with the projector, which flickers a tiny Trollogram kie that waves at him (he unplugs it fast); a weasel pops out of the hutch and back in |
| Ground | **kiwicore** | A drum kit by the wall, a shower cubicle in the corner and a fridge of XXXX Gold | A short drum fill with a cymbal grab; throws his flat cap across the room and catches it; steam rises from the shower and a can floats down |
| Ground | **garyoldmancorp** | A coffee cart with a spinning prize wheel, his scooter parked on a ramp by the door | Spins the coffee wheel and waits; scoots a lap of the ground floor and stops a hair from the wall (one time in four: CRASH!) |
| Loft | **kcaj** | A chest freezer of ice cream and a desk buried in sticky notes, with a red string board | Eats two cones at once; stares at the string board, adds a note, and the screen edges dim for a moment (Hyperfixate) |
| Loft | **torikloud** | A lectern with law books, two chairs facing each other, and a kookaburra on a perch | Rehearses an objection at an empty chair; the kookaburra laughs; flips through a book and slams it shut |
| Loft | **ciarra** | A tattoo chair with flash sheets on the wall, a sewing machine, race bibs pinned in a row | Tattoos a little design on her own arm; threads the sewing machine; does a frog leap up onto the chair and back down |
| Loft | **larinovsky** | A hammock with two cats on it and a phone on speaker | Naps, with the cats keeping watch; answers the phone ("NO, I WILL NOT BUY GIFT CARDS") and hangs up |

**Idle cycles** are short loops of 3 to 6 seconds. Each crewmate runs one at random, then a plain breathing idle for a few seconds, then another. They're cosmetic, so they use the cosmetic randomness, never the game's.

### Filling the den as crew are freed

- **A freed crewmate moves in.** The first visit after a freeing plays a short move-in beat (about 3 seconds, tap to skip): the new crewmate walks in through the roller door, looks round their corner, and the station lights up with a chip clunk. tones2 or kie says hi from the couch.
- **Until then, their corner is wrecked and dark:** the raid's mess, a tarp, no lights, no name. Nothing in it says who it belongs to. (Picked by the owner, 2026-10-16.)
- **The coming-soon crew have no corner at all.** When the first of them arrives, the den grows: the camera pans right through a knocked-through wall into a second room for the next four. The game never shows that wall as a door until then.
- **Looks follow into the den:** a crewmate's hat and skin show at their station, so the den is also where you see what you've bought.

### When the den first appears

The den replaces the crew screen. It appears with it, after Mission one ([what the player sees](levels.md#what-the-player-sees-and-when)): before that tones2 is the only soldier and Mission one starts straight from START.

- **The first visit** is the first time the player opens MISSIONS or CREW after [two of the crew](story.md#two-of-the-crew). A short beat plays (about 4 seconds, once ever): the hangout is still trashed, kie heaves the couch back upright, tones2 plugs the TV back in and the room warms up. kie: "HOME SWEET HOME." Then it's the den, with two stations lit and six dark.
- **No narrator in the den.** The narrator tells cutscenes; the den is a menu with the crew in it, so it only has speech bubbles. The music is the title theme ([music](music.md)).

## Crew interactions

Every so often two crewmates who are both in the den do a bit together. It only uses crew who are freed and not picked, so the room stays alive however many are in it.

- **How often:** one every 12 to 20 seconds, never two at once. The two walk to each other (or shout across the room), play it out in 4 to 8 seconds, and go back to their stations.
- **Bubbles** are a few words in capitals, about two seconds each, like the cutscenes' ([cutscenes](story.md#cutscenes)).
- **Each pair has one or two bits;** a pair with none just waves. They pick at random, but the same bit doesn't play twice in a row.

Sample bits (placeholders):

| Pair | What happens | Lines |
|---|---|---|
| tones2 + kie | tones2 holds out a slushie; kie's Trollogram takes it, and it falls through and splashes on the floor | tones2: "WANT ONE?" · kie: "THAT WASN'T ME." |
| tones2 + kiwicore | kiwicore starts a drum fill and tones2 jumps off the couch for an air guitar solo, then sits back down like nothing happened | kiwicore: "ONE TWO THREE FOUR" |
| kie + larinovsky | larinovsky's phone rings; kie answers it in a Trollogram's voice | kie: "THIS IS THE ATO." · larinovsky: "HANG UP HANG UP" |
| kcaj + torikloud | kcaj points at the string board; torikloud stands up at the lectern | torikloud: "OBJECTION. HEARSAY." · kcaj: "IT'S ALL CONNECTED." |
| ciarra + larinovsky | ciarra tiptoes over and tattoos a tiny cat on the sleeping larinovsky; a cat hisses | ciarra: "HOLD STILL." · (a snore) |
| garyoldmancorp + kiwicore | garyoldmancorp scoots past and grabs a can from the fridge; kiwicore throws his cap and knocks it out of his hand | kiwicore: "THAT'S MINE." |
| torikloud + torikloud's twin | torikloud's twin appears in the other chair and argues back, in social-work words | torikloud: "SUBPOENA." · Twin: "SAFEGUARDING." |
| kcaj + tones2 | kcaj offers tones2 a cone; tones2 eats it in one bite and freezes solid for a second | tones2: "BRAIN FREEZE." |

## Picking a soldier and getting ready

**The couch is the soldier pick.** It has two seats, each with a controller on it: the left for player one, the right for player two. Whoever sits in a seat is that player's soldier, and the controller in their hands is the sign they're in.

### Solo

1. **MISSIONS → a mission → the den.** The console's start screen on the TV shows the chosen mission. (CREW opens the den too, with no mission, to look around, check a crewmate's stats and looks, and pick who you'll play next.)
2. **Tap a crewmate to pick them.** They stop what they're doing and head for the couch: ground-floor crew walk over, and loft crew vault the railing and drop down with a thud. They flop into the left seat, grab the controller, and their name card slides up from the bottom (the name, their colours and the ⓘ for their kit), a small cousin of the [character intro](story.md#character-intros) with no slow-mo and no flash. The last soldier you played is already on the couch when the den opens.
3. **Changing your mind:** tap another crewmate. The one on the couch hands over the controller with a shrug and goes back to their station, and the new pick takes the seat.
4. **While you sit there,** your soldier fidgets with the controller, and the crew's bits carry on around them. Unpicked crew can wander over and lean on the back of the couch for a bit (tones2: "MOVE OVER").
5. **The empty seat is the way to bring a mate,** once co-op is open: tap the second controller to host a room, and the room code goes up on the TV ([co-op](#co-op)). You can sit and wait there for a second player.
6. **READY:** your soldier presses START. The controller's light on the console goes green, the TV's start screen flashes, and the camera zooms into the TV, straight into the mission's briefing.

### Co-op

- **Each phone shows its own den,** built from its own player's progress: your freed crew, your looks. Both players have beaten Mission one, because co-op isn't open before then.
- **The host is player one,** on the left seat. Player two's seat stays empty, controller lying on the cushion, until someone joins.
- **Your partner arrives.** When someone joins the room, their pick comes to the second seat: if it's a crewmate in your den, they jump down from their station (or walk over) and take it; if you haven't freed that crewmate yet, they walk in through the roller door as a visitor and sit down. Either way they grab the second controller and wave at the first.
- **Picks show on both phones.** Each seat has a P1 or P2 tag in the player's colour. A crewmate your partner is sitting as is taken: their station dims and tapping them does nothing (two players can't pick the same soldier, [co-op](player.md#co-op)). If you both tap the same one at the same moment, the host's pick wins and the guest's is undone.
- **Ready up:** each player taps READY, and their soldier presses START: that controller's light goes green on the console. When both seats are filled and ready, the two soldiers do a little bit together on the couch (a fist bump, or a pair's own move if their team super is built: [team supers](team-supers.md)).
- **When both are ready,** the TV counts down 3, 2, 1 and the camera zooms into the screen, into the briefing. Either player tapping UNREADY during the countdown stops it.
- **The host picks the mission** on the mission board. Changing it puts both players back to not ready, so nobody is carried into a mission they didn't agree to.
- **The room code and invite** are on the TV: tap it to copy the link ([online](levels.md#online-once-its-open)).
- **Spectators** don't come into the den: the TV shows how many are watching ("2 WATCHING").
- **A partner who drops in mid-mission** still gets the den first, to pick a soldier. READY drops them straight into the mission.
- **Leaving:** BACK gets your soldier up off the couch and out of the roller door; on your partner's phone they wave and leave, and the seat's controller drops back onto the cushion.

## What the engine needs

- **The den scene:** a fixed room built from props, with stations as tap targets, and the crew sprites standing in it. The cutscene stage can draw it ([cutscenes](story.md#cutscenes)), with taps going to the stations.
- **Idle and interaction cycles:** each a little script of moves, sprite frames, props and bubbles, picked at random by cosmetic randomness.
- **Unlock state:** who's freed, which stations have had their move-in beat, and whether the first visit has played, kept with the player's progress so it follows a sign-in.
- **Co-op messages:** a player's pick, ready and unready, and the host's mission choice, on the room's existing connection.

## Open questions

- **The bits and lines:** the owner's rewrite in the crew's voice, and which pairs get bits.
- **Which floor each crewmate lives on,** and whether stations can be rearranged as a look.
