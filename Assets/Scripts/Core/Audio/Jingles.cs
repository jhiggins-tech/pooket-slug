namespace PooketSlug
{
    /// <summary>The moments that have both a sound effect's jingle and a music sting.</summary>
    public enum Jingle
    {
        /// <summary>A mission starts: <c>Sfx.Start</c>, or the start sting.</summary>
        MissionStart,
        /// <summary>The mission's won: <c>Sfx.Victory</c>, or the mission complete sting.</summary>
        Victory,
        /// <summary>This phone's soldier is out for good: <c>Sfx.GameOver</c> (the sad trombone), or the game over sting.</summary>
        GameOver,
    }

    /// <summary>
    /// One sound at a time for a jingle (<c>docs/design/music.md</c>): with the music on, the music's sting plays at
    /// that moment, so the sound effect's jingle is skipped and the two never sound together. With the music off, the
    /// effect plays, so the moment still has its sound.
    /// </summary>
    public static class Jingles
    {
        /// <summary>Whether the jingle's sound effect plays.</summary>
        /// <param name="musicOn">The music is on (<c>!Music.Muted</c>).</param>
        /// <param name="runEnds">
        /// For <see cref="Jingle.GameOver"/> only: the run ends with this soldier (solo, or the last of the team out), so
        /// the game over sting follows. A co-op soldier going out while a partner plays on gets the trombone, music or not.
        /// </param>
        public static bool EffectPlays(Jingle jingle, bool musicOn, bool runEnds = true)
        {
            if (!musicOn) return true;
            return jingle == Jingle.GameOver && !runEnds; // no sting follows, so nothing to make way for
        }
    }
}
