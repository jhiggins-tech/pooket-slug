using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The game's sounds: Pooket Tabks' 8-bit recipes (src/audio/sfx.ts), on the <see cref="Chip"/> synth, each
    /// rendered once into a clip at startup. Each recipe below says which Tabks sound it's from; a few are cut down, because
    /// a machine gun fires far more often than an artillery tank.
    /// </summary>
    public static class ChipSounds
    {
        /// <summary>Makes every clip in the sound bank (<see cref="Sfx"/>).</summary>
        public static void Make()
        {
            // ---- Shots ----
            // Tabks' "pew" (a stepped downward sweep), short and light for the pistol.
            Sfx.Shot = Render("chipShot", 0.13f, c =>
            {
                c.Tone(0f, 0.12f, 1600f, 300f, duty: 0.25f, steps: 6, vol: 0.15f);
                c.Noise(0f, 0.05f, rate: 0.9f, vol: 0.1f);
            });
            // The heavy machine gun: a fatter, shorter pew with more crunch.
            Sfx.Heavy = Render("chipHeavy", 0.1f, c =>
            {
                c.Tone(0f, 0.09f, 1100f, 160f, duty: 0.5f, steps: 5, vol: 0.14f);
                c.Noise(0f, 0.06f, rate: 0.6f, vol: 0.14f);
            });
            // The goons' rifles: a lower, quieter pew, so yours stand out.
            Sfx.EnemyShot = Render("chipEnemyShot", 0.15f, c => c.Tone(0f, 0.14f, 900f, 200f, duty: 0.125f, steps: 6, vol: 0.12f));
            // Tabks' plain shell: pew and a thump of noise, with the boss's big bottom end.
            Sfx.Cannon = Render("chipCannon", 0.3f, c =>
            {
                c.Tone(0f, 0.22f, 1400f, 180f, duty: 0.25f, steps: 10, vol: 0.18f);
                c.Noise(0f, 0.12f, rate: 0.45f, vol: 0.2f);
                c.Tone(0f, 0.25f, 120f, 45f, duty: 0.5f, steps: 6, vol: 0.18f);
            });
            // Tabks' slide whistle (The Rizzler), short: a grenade lobbed.
            Sfx.Throw = Render("chipThrow", 0.2f, c => c.Tone(0f, 0.18f, 450f, 1100f, triangle: true, vol: 0.14f));

            // ---- Blasts and hits ----
            Sfx.Explosion = Render("chipExplosion", 0.7f, c => Boom(c, 30f));
            Sfx.BigExplosion = Render("chipBigExplosion", 1.1f, c => Boom(c, 60f));
            // Tabks' "oof": a big one when you're hurt, a small one when you hit something.
            Sfx.Hurt = Render("chipHurt", 0.22f, c => c.Tone(0f, 0.2f, 520f, 160f, duty: 0.25f, steps: 4, vol: 0.16f));
            Sfx.Hit = Render("chipHit", 0.11f, c => c.Tone(0f, 0.1f, 700f, 380f, duty: 0.25f, steps: 4, vol: 0.08f));

            // ---- Getting about ----
            // Tabks' hop (ciarra's frog boing).
            Sfx.Jump = Render("chipJump", 0.2f, c =>
            {
                c.Tone(0f, 0.11f, 240f, 760f, duty: 0.25f, vol: 0.1f);
                c.Tone(0.11f, 0.07f, 760f, 520f, duty: 0.25f, vol: 0.06f);
            });

            // ---- Jingles ----
            // Tabks' refunded round: ding-DING.
            Sfx.Pickup = Render("chipPickup", 0.45f, c =>
            {
                c.Tone(0f, 0.07f, Chip.Midi(83), duty: 0.5f, vol: 0.14f);
                c.Tone(0.07f, 0.35f, Chip.Midi(88), duty: 0.5f, vol: 0.14f);
            });
            // Tabks' wake-up (Take a Nap's level-up arpeggio): mission start.
            Sfx.Start = Render("chipStart", 0.45f, c => c.Arp(new[] { 72f, 76f, 79f, 84f, 88f, 91f }, 0.065f, duty: 0.25f, vol: 0.14f));
            // Tabks' victory fanfare.
            Sfx.Victory = Render("chipVictory", 1.65f, c =>
            {
                c.Arp(new[] { 72f, 76f, 79f, 84f }, 0.11f, duty: 0.25f, vol: 0.15f);
                c.Arp(new[] { 79f, 84f }, 0.18f, at: 0.5f, dur: 0.16f, duty: 0.25f, vol: 0.15f);
                c.Tone(0.9f, 0.7f, Chip.Midi(88), duty: 0.25f, vibratoRate: 6f, vibratoDepth: 10f, vol: 0.15f);
            });
            // The build-up to a character's intro, like the run-up before a drop: a snare roll that speeds up, a noise
            // sweep and a tone both climbing, all swelling to the peak, where the hit lands (CutscenePlayer.BuildUp long).
            Sfx.IntroRise = Render("chipIntroRise", 2.5f, c =>
            {
                float[] swell = { 1f, 1f };
                c.Noise(0f, 2.5f, rate: 0.15f, to: 1f, vol: 0.1f, env: swell);
                c.Tone(0f, 2.5f, Chip.Midi(48), Chip.Midi(72), duty: 0.125f, vol: 0.07f, env: swell);
                c.Tone(0f, 2.5f, Chip.Midi(55), Chip.Midi(79), duty: 0.25f, vol: 0.05f, env: swell);
                // the roll: hits a quarter-second apart, then eighths, then sixteenths, then a blur, louder as it goes
                for (float t = 0f, gap = 0.25f; t < 2.45f; t += gap)
                {
                    c.Noise(t, 0.05f, rate: 0.8f, vol: 0.05f + 0.1f * t / 2.5f);
                    if (t > 1f) gap = 0.125f;
                    if (t > 1.6f) gap = 0.0625f;
                    if (t > 2.1f) gap = 0.035f;
                }
            });
            // A character's intro (story.md, Character intros): a big musical hit, every note of a wide chord at once over a
            // crash and a bass drop, ringing out with a wobble as the slow-mo holds.
            Sfx.Intro = Render("chipIntro", 1.7f, c =>
            {
                foreach (float n in new[] { 48f, 60f, 67f, 72f, 76f, 79f })
                    c.Tone(0f, 1.6f, Chip.Midi(n), duty: 0.25f, vibratoRate: 5.5f, vibratoDepth: Chip.Midi(n) * 0.006f, vol: 0.06f);
                c.Noise(0f, 0.6f, rate: 0.55f, to: 0.2f, vol: 0.22f);
                c.Tone(0f, 0.5f, 160f, 45f, duty: 0.5f, steps: 8, vol: 0.2f);
            });
            // Tabks' did-not-finish: sad trombone, wah wah wah waaaah.
            Sfx.GameOver = Render("chipGameOver", 1.9f, c =>
            {
                float[] notes = { 67f, 66f, 65f };
                for (int i = 0; i < notes.Length; i++)
                    c.Tone(i * 0.32f, 0.28f, Chip.Midi(notes[i]), Chip.Midi(notes[i]) * 0.97f, duty: 0.5f, vol: 0.14f);
                c.Tone(0.96f, 0.9f, Chip.Midi(64), Chip.Midi(63), duty: 0.5f, vibratoRate: 6f, vibratoDepth: 9f, vol: 0.14f);
            });

            MakeKitSounds();
        }

        /// <summary>tones2's kit and the enemies' extras, from the matching Tabks weapons (tones2 is ten's kit there).</summary>
        static void MakeKitSounds()
        {
            // ten-1's hiss and gurgle, as a seamless loop: 96 Hz and 8 Hz both fit a whole number of times in half a second.
            Sfx.JetLoop = Render("chipJetLoop", 0.5f, c =>
            {
                c.Noise(0f, 0.5f, rate: 0.9f, vol: 0.12f, env: Flat);
                c.Tone(0f, 0.5f, 96f, duty: 0.125f, vibratoRate: 8f, vibratoDepth: 30f, vol: 0.07f, env: Flat);
            });
            // Tabks' splashback, the slap part.
            Sfx.Splat = Render("chipSplat", 0.1f, c => c.Noise(0f, 0.08f, rate: 0.9f, to: 0.3f, vol: 0.14f));
            // ten-2's putt-putt engine (a rumbling loop of it).
            Sfx.RocketRumble = Render("chipRocketRumble", 0.5f, c =>
            {
                for (float t = 0f; t < 0.45f; t += 0.09f)
                {
                    c.Noise(t, 0.05f, rate: 0.5f, vol: 0.1f);
                    c.Tone(t, 0.05f, 110f, duty: 0.5f, vol: 0.08f);
                }
            });
            // Tabks' jetpack launch: FWOOOSH-boing.
            Sfx.RocketLaunch = Render("chipRocketLaunch", 0.9f, c =>
            {
                c.Noise(0f, 0.9f, rate: 0.25f, to: 1.5f, vol: 0.25f);
                c.Tone(0f, 0.5f, 180f, 1300f, duty: 0.25f, steps: 12, vol: 0.15f);
            });
            // Tabks' hologram blowing up: the stuttering glitch-out, the power-down and the pixel sparkles.
            Sfx.Glitch = Render("chipGlitch", 0.75f, c =>
            {
                float[] chirps = { 2400f, 700f, 3100f, 520f, 1900f, 3600f };
                for (int i = 0; i < chirps.Length; i++) c.Tone(i * 0.028f, 0.022f, chirps[i], duty: 0.125f, vol: 0.09f);
                c.Tone(0.15f, 0.42f, 1500f, 45f, duty: 0.5f, steps: 16, vol: 0.18f);
                c.Noise(0.15f, 0.55f, rate: 1.1f, to: 0.12f, vol: 0.2f);
                c.Arp(new[] { 100f, 96f, 98f, 93f, 95f, 89f }, 0.045f, at: 0.42f, dur: 0.035f, duty: 0.125f, vol: 0.06f);
            });
            // Tabks' fizzle (a hologram whose owner is out), short: a dose of poison.
            Sfx.Poison = Render("chipPoison", 0.16f, c =>
            {
                c.Tone(0f, 0.15f, 700f, 200f, duty: 0.125f, steps: 5, vol: 0.1f);
                c.Noise(0f, 0.15f, rate: 1.4f, to: 0.3f, vol: 0.05f);
            });
            // ten-3: BLEURGH.
            Sfx.Spew = Render("chipSpew", Tones2Tuning.Spew.Duration, c =>
            {
                float dur = Tones2Tuning.Spew.Duration;
                c.Tone(0f, dur, 240f, 65f, duty: 0.125f, vibratoRate: 17f, vibratoDepth: 35f, vol: 0.2f);
                c.Noise(0f, dur, rate: 0.22f, to: 0.12f, vol: 0.14f);
            });
            // Tabks' cook, the sizzle part.
            Sfx.Sizzle = Render("chipSizzle", 0.15f, c => c.Noise(0f, 0.14f, rate: 2f, vol: 0.08f));
            // Band Aid's floor tom: a heavy landing.
            Sfx.Thump = Render("chipThump", 0.22f, c =>
            {
                c.Tone(0f, 0.2f, 170f, 80f, duty: 0.5f, steps: 6, vol: 0.22f);
                c.Noise(0f, 0.05f, rate: 0.5f, vol: 0.12f);
            });
            // The boss tank charging: ten-2's engine revving up into a stepped whine.
            Sfx.Charge = Render("chipCharge", 0.95f, c =>
            {
                float t = 0f;
                while (t < 0.9f)
                {
                    float k = t / 0.9f;
                    c.Noise(t, 0.05f, rate: 0.25f + k * 0.6f, vol: 0.04f + k * 0.06f);
                    t += 0.2f - k * 0.14f;
                }
                c.Tone(0f, 0.9f, 180f, 900f, duty: 0.125f, steps: 12, vol: 0.1f, env: new[] { 0.1f, 0.4f, 1f, 1f });
            });
            // The boss intro's siren: one wail up and back down, square and a little thin, like an arcade alarm.
            Sfx.Siren = Render("chipSiren", 0.56f, c =>
            {
                c.Tone(0f, 0.28f, 520f, 880f, duty: 0.25f, vol: 0.15f, env: Flat);
                c.Tone(0.28f, 0.28f, 880f, 520f, duty: 0.25f, vol: 0.15f, env: new[] { 0.8f, 1f, 1f, 0f });
            });
            // The spinners' tick: the turret's servo.
            Sfx.Servo = Render("chipServo", 0.04f, c => c.Tone(0f, 0.03f, 1760f, duty: 0.125f, vol: 0.08f));

            // kie's weasels tumbling out: three little squeaks, up and up.
            Sfx.WeaselThrow = Render("chipWeaselThrow", 0.2f, c =>
            {
                for (int i = 0; i < 3; i++) c.Tone(i * 0.05f, 0.045f, 1300f + i * 300f, 2100f + i * 300f, duty: 0.125f, vol: 0.08f);
            });
            // Tabks' Pop Goes the Weasel, its last line: "POP! goes the wea-sel", the pop landing on the high A.
            Sfx.WeaselTune = Render("chipWeaselTune", 1.6f, c =>
            {
                (float note, int steps)[] pop = { (81f, 3), (74f, 2), (77f, 1), (76f, 3), (72f, 3) };
                (float note, int steps)[] bass = { (53f, 3), (50f, 3), (55f, 3), (48f, 3) };
                const float step = 0.11f;
                float t = 0f;
                foreach (var (note, steps) in pop)
                {
                    c.Tone(t, steps * step * 0.8f, Chip.Midi(note), duty: 0.25f, vol: 0.13f);
                    t += steps * step;
                }
                t = 0f;
                foreach (var (note, steps) in bass)
                {
                    c.Tone(t, steps * step * 0.6f, Chip.Midi(note), triangle: true, vol: 0.2f);
                    t += steps * step;
                }
                c.Noise(0f, 0.06f, rate: 0.7f, vol: 0.18f); // the pop
            });
            // A Trollogram flickering on: a shimmering rising sweep with sparkles.
            Sfx.TrollDrop = Render("chipTrollDrop", 0.45f, c =>
            {
                c.Tone(0f, 0.35f, 300f, 1800f, duty: 0.125f, steps: 10, vibratoRate: 30f, vibratoDepth: 40f, vol: 0.1f);
                c.Arp(new[] { 96f, 100f, 103f, 108f }, 0.05f, at: 0.25f, dur: 0.04f, duty: 0.125f, vol: 0.06f);
            });
            // Swapping places with it: a quick there-and-back blip.
            Sfx.TrollSwap = Render("chipTrollSwap", 0.22f, c =>
            {
                c.Tone(0f, 0.09f, 2200f, 500f, duty: 0.25f, steps: 5, vol: 0.1f);
                c.Tone(0.1f, 0.1f, 500f, 2200f, duty: 0.25f, steps: 5, vol: 0.1f);
            });
        }

        static readonly float[] Flat = { 0f, 1f, 1f, 1f };

        /// <summary>Tabks' blast: crunchy noise, bigger blasts longer and lower (<paramref name="r"/> is its radius there).</summary>
        static void Boom(Chip c, float r)
        {
            float dur = 0.18f + r / 70f;
            c.Noise(0f, dur, rate: 0.8f, to: 0.12f, vol: Mathf.Min(0.45f, 0.12f + r / 90f));
            c.Tone(0f, dur * 0.6f, 130f, 40f, duty: 0.5f, steps: 6, vol: 0.2f);
        }

        static AudioClip Render(string name, float seconds, Action<Chip> recipe)
        {
            var chip = new Chip(seconds);
            recipe(chip);
            return Sfx.Clip(name, chip.Finish(), Chip.Rate);
        }
    }
}
