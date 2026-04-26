using System.Collections.Generic;
using CollectEggs.Client.View;
using CollectEggs.Core;
using CollectEggs.Gameplay.Players;
using CollectEggs.Shared.Messages;

namespace CollectEggs.Client
{
    public sealed class ClientMatchInitialization
    {
        public readonly List<PlayerEntity> Players = new();
        public readonly Dictionary<string, PlayerEntity> PlayersById = new();
        public PlayerEntity LocalPlayer;
    }

    public sealed class ClientMatchInitializer
    {
        public static ClientMatchInitialization Initialize(
            MatchStartedMessage message,
            PlayerSpawner playerSpawner,
            EggViewManager eggViewManager,
            GameManager gameManager)
        {
            var result = new ClientMatchInitialization();
            if (message == null || playerSpawner == null || eggViewManager == null)
                return result;
            playerSpawner.RebuildSpawnParents();
            var botVisualIndex = 0;
            foreach (var player in message.players)
            {
                var entity = playerSpawner.SpawnFromServerData(player, botVisualIndex);
                if (entity == null)
                    continue;
                result.Players.Add(entity);
                if (!string.IsNullOrWhiteSpace(entity.PlayerId))
                    result.PlayersById[entity.PlayerId] = entity;
                if (player.isLocalClientPlayer)
                    result.LocalPlayer = entity;
                else
                    botVisualIndex++;
            }

            var root = playerSpawner.EggsRoot;
            eggViewManager.ClearTrackedEggsForNewMatch();
            foreach (var egg in message.eggs)
                eggViewManager.SpawnFromServerData(egg, root);
            var bootstrapper = gameManager != null ? gameManager.GetComponent<GameBootstrapper>() : null;
            if (gameManager != null && bootstrapper != null)
                gameManager.ApplyServerMatchStarted(message, result.Players, result.LocalPlayer, bootstrapper);
            return result;
        }
    }
}
