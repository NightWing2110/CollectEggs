using System;
using System.Collections.Generic;
using System.Linq;
using CollectEggs.Gameplay.Players;

namespace CollectEggs.UI.Results
{
    public sealed class WinnerNameResolver
    {
        public static List<string> Resolve(
            IReadOnlyCollection<string> winnerPlayerIds,
            IReadOnlyList<PlayerEntity> players,
            IReadOnlyList<MatchResultEntry> sortedResults)
        {
            if (winnerPlayerIds == null || winnerPlayerIds.Count == 0)
                return sortedResults?.Where(r => r.Rank == 1).Select(r => r.DisplayName).ToList() ?? new List<string>();
            var names = new List<string>(winnerPlayerIds.Count);
            foreach (var playerId in winnerPlayerIds)
            {
                var player = players?.FirstOrDefault(p => p != null && p.PlayerId == playerId);
                names.Add(player == null || string.IsNullOrWhiteSpace(player.DisplayName) ? playerId : player.DisplayName);
            }
            names.Sort(StringComparer.Ordinal);
            return names;
        }
    }
}
