namespace CollectEggs.UI.Results
{
    public readonly struct MatchResultEntry
    {
        public readonly int Rank;
        public readonly string DisplayName;
        public readonly int EggCount;

        public MatchResultEntry(int rank, string displayName, int eggCount)
        {
            Rank = rank;
            DisplayName = displayName;
            EggCount = eggCount;
        }
    }
}
