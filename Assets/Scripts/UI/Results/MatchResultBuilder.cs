using System;
using System.Collections.Generic;
using System.Linq;
using CollectEggs.Gameplay.Players;
using CollectEggs.Gameplay.Scoring;

namespace CollectEggs.UI.Results
{
    public sealed class MatchResultBuilder
    {
        public static List<MatchResultEntry> Build(IReadOnlyList<PlayerEntity> players, ScoreService scoreService)
        {
            var results = new List<MatchResultEntry>(players?.Count ?? 0);
            if (players == null || scoreService == null)
                return results;
            var sortedPlayers = (from player in players
                    where player != null
                    let score = scoreService.GetScore(player.PlayerId)
                    let displayName = string.IsNullOrWhiteSpace(player.DisplayName) ? player.PlayerId : player.DisplayName
                    select new { displayName, score })
                .OrderByDescending(x => x.score)
                .ThenBy(x => x.displayName, StringComparer.Ordinal)
                .ToList();

            var rank = 0;
            var previousScore = -1;
            for (var i = 0; i < sortedPlayers.Count; i++)
            {
                var player = sortedPlayers[i];
                if (i == 0 || player.score != previousScore)
                {
                    rank++;
                    previousScore = player.score;
                }
                results.Add(new MatchResultEntry(rank, player.displayName, player.score));
            }

            return results;
        }
    }
}
