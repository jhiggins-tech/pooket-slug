// The scene check (docs/scenes.md): the check every scene passes before it goes live, run by the scene deploy
// (scene-files.yml) and the build (webgl-pages.yml) with the game's own reader (Core), so what it passes the game plays.
//
//   dotnet run --project tools/scenecheck -- <scenes dir> [<narration dir> [<out dir>]]
//
// Reads every <scenes dir>/*.txt: each must read cleanly (SceneScript), have its own number for a partner's phone, and
// give every line time to be read; with <narration dir> (the narrator's recordings, <line>.wav), every narrator line
// needs its recording and the time to play it, the lengths read from the clips themselves. With <out dir>, writes what
// the site serves: the scenes, voice/<line>.wav, and voice.txt listing each recording's hash and length (VoiceClip), so
// a phone fetches only the recordings that differ from its build's. Exits 1 if any scene has a problem.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PooketSlug.Tools
{
    static class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("usage: scenecheck <scenes dir> [<narration dir> [<out dir>]]");
                return 2;
            }
            string scenesDir = args[0], voiceDir = args.Length > 1 && args[1] != "-" ? args[1] : null, outDir = args.Length > 2 ? args[2] : null;

            var clips = new List<VoiceClip>();
            var lengths = new Dictionary<string, float>();
            if (voiceDir != null && Directory.Exists(voiceDir))
                foreach (string wav in Directory.GetFiles(voiceDir, "*.wav").OrderBy(f => f, StringComparer.Ordinal))
                {
                    string id = Path.GetFileNameWithoutExtension(wav);
                    if (!SetupText.IsName(id)) continue;
                    byte[] bytes = File.ReadAllBytes(wav);
                    float seconds = SceneCheck.Seconds(bytes);
                    if (seconds < 0f)
                    {
                        Console.WriteLine("::warning::" + id + ".wav isn't a WAV the game can play");
                        continue;
                    }
                    lengths[id] = seconds;
                    clips.Add(new VoiceClip { Id = id, Hash = SceneScript.Hash(bytes), Seconds = seconds });
                }
            else if (voiceDir != null) Console.WriteLine("::warning::No recordings in " + voiceDir + ": narrator lines are checked for reading time only");

            var scenes = new List<SceneScript>();
            int problems = 0;
            foreach (string file in Directory.GetFiles(scenesDir, "*.txt").OrderBy(f => f, StringComparer.Ordinal))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                var found = new List<string>();
                if (!SetupText.IsName(name)) found.Add("its file name can only have letters, digits, - and _");
                SceneScript s = SceneScript.Read(name, File.ReadAllText(file));
                scenes.Add(s);
                found.AddRange(s.Problems);
                found.AddRange(SceneCheck.Timing(s.Def, voiceDir != null ? lengths : null));
                foreach (string p in found) Console.WriteLine("::error file=scenes/" + name + ".txt::" + p);
                problems += found.Count;
                Console.WriteLine(name + ": " + s.Def.Shots.Length + " shots, " + (found.Count == 0 ? "fine" : found.Count + " problem(s)") + " (version " + s.Version.ToString("x8") + ")");
            }
            foreach (string p in SceneCheck.Numbers(scenes))
            {
                Console.WriteLine("::error::" + p);
                problems++;
            }
            foreach (string missing in SceneLibrary.Names.Where(n => scenes.All(s => s.Name != n)))
            {
                Console.WriteLine("::error::scenes/" + missing + ".txt is missing: the game plays it");
                problems++;
            }
            if (problems > 0)
            {
                Console.WriteLine(problems + " problem(s): nothing goes live until they're fixed");
                return 1;
            }

            if (outDir != null)
            {
                Directory.CreateDirectory(Path.Combine(outDir, "voice"));
                foreach (string file in Directory.GetFiles(scenesDir, "*.txt")) File.Copy(file, Path.Combine(outDir, Path.GetFileName(file)), true);
                foreach (VoiceClip c in clips) File.Copy(Path.Combine(voiceDir, c.Id + ".wav"), Path.Combine(outDir, "voice", c.Id + ".wav"), true);
                File.WriteAllText(Path.Combine(outDir, "voice.txt"), string.Join("\n", clips.Select(c => c.ToString())) + "\n");
                Console.WriteLine("Wrote " + scenes.Count + " scenes and " + clips.Count + " recordings to " + outDir);
            }
            return 0;
        }
    }
}
