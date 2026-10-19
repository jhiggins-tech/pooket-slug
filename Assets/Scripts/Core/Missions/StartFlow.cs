namespace PooketSlug
{
    /// <summary>
    /// What START plays, and where a won solo debrief goes (Jack's start flow, 19 Oct): the story's missions before the
    /// den (<see cref="MissionStory.BeforeDen"/>: the opening and Mission one) play straight through, each debrief going on
    /// into the next; once they're beaten, START and every debrief go to the den, where Mission two and co-op are.
    /// Signed in or not, it's the same run; signed in, the progress follows the account.
    /// </summary>
    public static class StartFlow
    {
        /// <summary>The first mission of the run before the den that <paramref name="profile"/> hasn't beaten, or null once they all are.</summary>
        public static MissionDef Next(Profile profile)
        {
            foreach (MissionDef m in Missions.All)
                if (m.Story.BeforeDen && !MissionBoard.Cleared(profile, m)) return m;
            return null;
        }

        /// <summary>The den's open: the story's run before it is all beaten.</summary>
        public static bool DenOpen(Profile profile) => Next(profile) == null;
    }
}
