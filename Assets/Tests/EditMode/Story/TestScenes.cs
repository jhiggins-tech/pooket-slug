using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>The game's scenes as the tests read them: straight from <c>scenes/</c> in the repository.</summary>
    public static class TestScenes
    {
        static Dictionary<string, SceneScript> s_Read;

        public static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "scenes"));

        /// <summary>Every scene file, read.</summary>
        public static IReadOnlyDictionary<string, SceneScript> All
        {
            get
            {
                if (s_Read != null) return s_Read;
                s_Read = new Dictionary<string, SceneScript>();
                foreach (string file in Directory.GetFiles(Folder, "*.txt"))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    s_Read[name] = SceneScript.Read(name, File.ReadAllText(file));
                }
                return s_Read;
            }
        }

        public static SceneScript Script(string name)
        {
            Assert.IsTrue(All.TryGetValue(name, out SceneScript s), "scenes/" + name + ".txt is there");
            return s;
        }

        public static CutsceneDef Opening => Script(SceneLibrary.Opening).Def;
        public static CutsceneDef MotorPool => Script(SceneLibrary.MotorPool).Def;
        public static CutsceneDef FreeingKie => Script(SceneLibrary.FreeingKie).Def;
        public static CutsceneDef CoopTest => Script(SceneLibrary.CoopTest).Def;
    }
}
