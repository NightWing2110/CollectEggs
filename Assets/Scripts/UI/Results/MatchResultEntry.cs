namespace CollectEggs.UI.Results
{
    public readonly struct MatchResultEntry
    {
        public readonly string DisplayName;
        public readonly int EggCount;

        public MatchResultEntry(string displayName, int eggCount)
        {
            DisplayName = displayName;
            EggCount = eggCount;
        }
    }
}
