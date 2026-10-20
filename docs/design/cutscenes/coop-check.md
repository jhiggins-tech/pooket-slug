# The co-op check

Part of [the cutscenes](README.md), the one script for every scene: the rules, the format and every scene at a glance are there.

Not part of the story: a stand-in for later missions' co-op scenes, so the two-phone check (`?test=scene`, CI's
co-op test) has a scene to tap through, since Mission one's own scenes never play in a co-op room. Six plain shots of
6 seconds outside the hangout, each held long enough that a phone tapping alone is plainly waiting. CI counts the
moves between them, so it stays at six. Scene file: `scenes/coop-test.txt`.

```text
1-6. EXT. THE HANGOUT - NIGHT                     6 s each

TONES2 stands outside the hangout in the snow.

                    TONES2
          TEST SHOT 1 OF 6: TAP TOGETHER

(And so on, up to 6 of 6.)
```
