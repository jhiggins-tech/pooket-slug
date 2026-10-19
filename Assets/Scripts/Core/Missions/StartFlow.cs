namespace PooketSlug
{
    /// <summary>
    /// What START plays, and where a won solo debrief goes (Jack's start flow, 19 Oct, on the Opening 5 page): the story
    /// runs straight through before the den opens (the opening and Mission one, then the Mission two briefing and Mission
    /// two, each debrief going straight on into the next), and once it's all beaten, START and every debrief go to the
    /// den. Signed in or not, it's the same run; signed in, the progress follows the account.
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
