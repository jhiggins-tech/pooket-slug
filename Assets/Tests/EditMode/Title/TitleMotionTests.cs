using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>The title logo's parts (Core/Title/TitleLogo.cs) and how they move (TitleMotion): Jack's A2, H2 and G3.</summary>
    public class TitleMotionTests
    {
        // the build's own parts file, from the project's folder (as TestScenes reads the scenes)
        static TitleLogo Logo => TitleLogo.Parse(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets/Resources/Title/parts.txt")));

        static int Index(TitleLogo logo, string name)
        {
            for (int i = 0; i < logo.Parts.Count; i++)
                if (logo.Parts[i].Name == name) return i;
            return -1;
        }

        [Test]
        public void TheBuildsParts_AreTenLettersAndTheTabk_InsideTheLogoAndTheSheet()
        {
            TitleLogo logo = Logo;
            Assert.IsNotNull(logo);
            Assert.AreEqual(11, logo.Parts.Count);
            Assert.AreEqual(TitleLogo.Tabk, logo.TabkPart.Name);
            foreach (TitleLogo.Part p in logo.Parts)
            {
                Assert.That(p.X >= 0 && p.Y >= 0 && p.X + p.W <= logo.Width && p.Y + p.H <= logo.Height, p.Name + " sits in the logo");
                Assert.That(p.U + p.W <= logo.SheetWidth && p.V + p.H <= logo.SheetHeight, p.Name + " is in the sheet");
            }
            Assert.Less(logo.TabkPart.Y + logo.TabkPart.H, logo.Height * 0.7f, "the tabk rides on top of the word");
        }

        [Test]
        public void Parse_TurnsDownAFileWithoutATabkOrSizes()
        {
            Assert.IsNull(TitleLogo.Parse(""));
            Assert.IsNull(TitleLogo.Parse("logo 10 10\nsheet 10 10\nP 0 0 1 1 0 0"));
            Assert.IsNull(TitleLogo.Parse("sheet 10 10\ntabk 0 0 1 1 0 0"));
            Assert.IsNull(TitleLogo.Parse("logo 10 10\nsheet 10 10\ntabk 0 0 1 1"));
            Assert.IsNotNull(TitleLogo.Parse("# a comment\nlogo 10 10\nsheet 10 10\ntabk 0 0 1 1 0 0\n"));
        }

        [Test]
        public void TheTabk_DrivesInFromOffTheLeft_AndStopsInItsPlace()
        {
            TitleLogo logo = Logo;
            int tabk = logo.TabkIndex;
            Assert.AreEqual(TitleMotion.DriveFrom, TitleMotion.Part(logo, tabk, -0.1f).X);
            Assert.Less(TitleMotion.DriveFrom + logo.TabkPart.X + logo.TabkPart.W, -logo.Width * 0.1f, "it starts off the screen");
            float last = TitleMotion.DriveFrom;
            for (float t = 0.1f; t <= TitleMotion.Stop; t += 0.1f)
            {
                float x = TitleMotion.Part(logo, tabk, t).X;
                Assert.GreaterOrEqual(x, last, "it only rolls forwards");
                last = x;
            }
            Assert.AreEqual(0f, TitleMotion.Part(logo, tabk, TitleMotion.Stop).X, 1e-3f);
            Assert.Less(TitleMotion.Part(logo, tabk, TitleMotion.Fire + 0.05f).X, 0f, "it recoils at the shot");
            Assert.AreEqual(TitleMotion.Pose.Rest.X, TitleMotion.Part(logo, tabk, TitleMotion.End(logo)).X);
        }

        [Test]
        public void TheLetters_SpringUpAsTheNosePasses_LeftToRight_ThenSlugAfterTheShot()
        {
            TitleLogo logo = Logo;
            string[] word = { "P", "O1", "O2", "K", "E", "T", "S", "L", "U", "G" };
            float last = -1f;
            foreach (string name in word)
            {
                float at = TitleMotion.SpringsAt(logo, Index(logo, name));
                Assert.Greater(at, last, name + " after the letter before");
                last = at;
                Assert.AreEqual(0f, TitleMotion.Part(logo, Index(logo, name), at - 0.01f).Height, name + " hidden till then");
                Assert.Greater(TitleMotion.Part(logo, Index(logo, name), at + TitleMotion.Spring * 0.5f).Height, 1f, name + " stretches past full height");
                Assert.AreEqual(1f, TitleMotion.Part(logo, Index(logo, name), at + TitleMotion.Spring).Height, name + " settles");
            }
            Assert.Less(TitleMotion.SpringsAt(logo, Index(logo, "T")), TitleMotion.Stop, "T comes up under the drive");
            foreach (string name in new[] { "S", "L", "U", "G" })
                Assert.Greater(TitleMotion.SpringsAt(logo, Index(logo, name)), TitleMotion.Fire, name + " pops after the shot");
        }

        [Test]
        public void TheSounds_EngineFromTheStart_TheCannonOnce_MusicJustAfterTheShot()
        {
            TitleLogo logo = Logo;
            int fired = 0;
            for (float t = 0f; t < 4f; t += 1f / 60f)
                if (TitleMotion.Fired(t, t + 1f / 60f)) fired++;
            Assert.AreEqual(1, fired);
            Assert.Greater(TitleMotion.MusicAt, TitleMotion.Fire);
            Assert.Less(TitleMotion.MusicAt, TitleMotion.SpringsAt(logo, Index(logo, "G")), "the music's going as S-L-U-G pop up");
            Assert.AreEqual(10, TitleMotion.Sprang(logo, -1f, TitleMotion.End(logo)));
        }

        [Test]
        public void TheMenu_FadesInOnceTheLogoIsIn()
        {
            TitleLogo logo = Logo;
            float end = TitleMotion.End(logo);
            Assert.AreEqual(0f, TitleMotion.MenuIn(logo, end - 0.1f));
            Assert.AreEqual(1f, TitleMotion.MenuIn(logo, end + 0.6f));
        }

        [Test]
        public void TheHover_IsStillDuringTheEntrance_ThenBobsGently_OverAShadowThatBreathes()
        {
            TitleLogo logo = Logo;
            float end = TitleMotion.End(logo);
            Assert.AreEqual(0f, TitleMotion.Bob(logo, end * 0.5f));
            float high = 0f, low = 0f;
            for (float t = end; t < end + 10f; t += 0.05f)
            {
                high = Mathf.Max(high, TitleMotion.Bob(logo, t));
                low = Mathf.Min(low, TitleMotion.Bob(logo, t));
            }
            Assert.AreEqual(TitleMotion.BobHeight, high, 0.01f);
            Assert.AreEqual(-TitleMotion.BobHeight, low, 0.01f);
            float top = end + TitleMotion.BobPeriod * 1.25f; // past the ramp, a quarter-period on: the top
            Assert.Less(TitleMotion.ShadowAlpha(logo, top), 1f, "the shadow fades as the logo rises");
            Assert.Greater(TitleMotion.ShadowAlpha(logo, top + TitleMotion.BobPeriod * 0.5f), 1f - 1e-3f, "and darkens as it dips");
        }

        [Test]
        public void TheGlint_SweepsAsTheLogoLands_ThenEverySixSeconds_EndingInAPingOffTheBarrel()
        {
            TitleLogo logo = Logo;
            float end = TitleMotion.End(logo);
            Assert.Less(TitleMotion.Glint(logo, end - 0.1f), 0f);
            Assert.AreEqual(0f, TitleMotion.Glint(logo, end), 1e-3f);
            Assert.Greater(TitleMotion.Glint(logo, end + TitleMotion.Sweep * 0.5f), 0.3f);
            Assert.Less(TitleMotion.Glint(logo, end + TitleMotion.Sweep + 0.1f), 0f, "quiet between sweeps");
            Assert.GreaterOrEqual(TitleMotion.Glint(logo, end + TitleMotion.GlintEvery + 0.1f), 0f, "and again");
            Assert.Greater(TitleMotion.SparkleAt(logo, end + TitleMotion.PingAt + TitleMotion.Sparkle * 0.5f, out _), 1f, "the ping");
            Assert.AreEqual(0f, TitleMotion.SparkleAt(logo, end + 3f, out _));
            Assert.Greater(TitleMotion.SparkleAt(logo, TitleMotion.Fire + TitleMotion.Sparkle * 0.5f, out _), TitleMotion.PingSparkle, "the muzzle flash is bigger");
            Assert.That(TitleMotion.BarrelTip.x > logo.TabkPart.CentreX && TitleMotion.BarrelTip.x <= logo.TabkPart.X + logo.TabkPart.W, "the barrel tip's on the tabk's right");
        }

        [Test]
        public void TheShake_IsAShortJoltAtTheShot()
        {
            Assert.AreEqual(Vector2.zero, TitleMotion.Shake(TitleMotion.Fire - 0.01f));
            Assert.Greater(TitleMotion.Shake(TitleMotion.Fire + 0.02f).magnitude, 0f);
            Assert.AreEqual(Vector2.zero, TitleMotion.Shake(TitleMotion.Fire + TitleMotion.ShakeFor + 0.01f));
        }
    }
}
