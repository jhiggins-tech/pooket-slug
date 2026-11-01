namespace PooketSlug
{
    /// <summary>
    /// A skit playing on one of the house's screens (<see cref="Skit"/>, <c>docs/design/cutscenes/skits.md</c>): the den's
    /// (<see cref="DenFlow"/>) or the bay's. The screen asks <see cref="Skits.Due"/> as it opens, starts it here, and hands
    /// it each frame with whether anything was pressed; a press skips it (not in its first moment, so the press that
    /// opened the room doesn't), and once it's done or skipped it's kept as played, once ever. The screen draws it and
    /// plays its sounds; its walking waits meanwhile.
    /// </summary>
    sealed class SkitRun
    {
        /// <summary>How long a skit plays before a press can skip it.</summary>
        public const float Settle = 0.3f;

        readonly Game m_Game;

        public SkitRun(Game game) => m_Game = game;

        /// <summary>The skit playing, or null.</summary>
        public Skit Skit { get; private set; }

        public bool Playing => Skit != null;

        public void Start(Skit skit)
        {
            Skit = skit;
            Log.Game.Info("Skit: " + skit.Key);
        }

        /// <summary>The room closed under it: it's not kept, so it plays on the next visit.</summary>
        public void Stop() => Skit = null;

        /// <summary>
        /// Moves the skit on by <paramref name="dt"/>, skipping it if <paramref name="pressed"/>; the skit that's just
        /// ended (kept as played, and saved), or null while it plays on.
        /// </summary>
        public Skit Tick(float dt, bool pressed)
        {
            Skit skit = Skit;
            if (skit == null) return null;
            if (pressed && skit.T > Settle) skit.Skip();
            else skit.Tick(dt);
            if (!skit.Done) return null;
            Game g = m_Game;
            if (Skits.Played(g.Profile, skit)) g.Store.Save(g.Profile);
            Skit = null;
            Log.Game.Info("Skit done: " + skit.Key);
            return skit;
        }
    }
}
