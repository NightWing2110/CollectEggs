using CollectEggs.Bots;
using CollectEggs.Gameplay.Movement;
using CollectEggs.Gameplay.Navigation;
using CollectEggs.Gameplay.Players.View;
using CollectEggs.Shared.Snapshots;
using UnityEngine;

namespace CollectEggs.Gameplay.Players
{
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField]
        private GameObject localPlayerPrefab;

        [SerializeField]
        private GameObject[] botPrefabs;

        [SerializeField]
        private Transform playersRoot;

        [SerializeField]
        private Transform eggsRoot;

        private Transform PlayersRoot => playersRoot;
        public Transform EggsRoot => eggsRoot;
        private Camera _cachedNameCamera;
        private GridMap _navigationGrid;

        public void SetNavigationGrid(GridMap navigationGrid) => _navigationGrid = navigationGrid;

        public void RebuildSpawnParents()
        {
            if (playersRoot == null)
                Debug.LogError("PlayerSpawner.playersRoot is not assigned.");
            if (eggsRoot == null)
                Debug.LogError("PlayerSpawner.eggsRoot is not assigned.");
        }

        public PlayerEntity SpawnFromServerData(PlayerSpawnData data, int botVisualIndex)
        {
            if (PlayersRoot == null)
                RebuildSpawnParents();
            var nameCamera = ResolveNameCamera();
            return data.isLocalClientPlayer
                ? SpawnLocalFromServer(data, nameCamera)
                : SpawnBotFromServer(data, botVisualIndex, nameCamera);
        }

        private PlayerEntity SpawnLocalFromServer(PlayerSpawnData data, Camera nameCamera)
        {
            if (localPlayerPrefab == null)
                return null;
            var go = Instantiate(localPlayerPrefab, data.spawnPosition, Quaternion.identity, PlayersRoot);
            go.name = "Player_Local";
            var movement = go.GetComponent<ActorMovement>();
            var controller = go.GetComponent<PlayerController>();
            var entity = go.GetComponent<PlayerEntity>();
            if (entity == null)
                return null;
            entity.ConfigureFromServer(data, movement, controller);
            AttachNameView(entity, data.displayName, nameCamera);
            return entity;
        }

        private PlayerEntity SpawnBotFromServer(PlayerSpawnData data, int botVisualIndex, Camera nameCamera)
        {
            var botPrefab = ResolveBotPrefab(botVisualIndex);
            if (botPrefab == null)
                return null;
            var go = Instantiate(botPrefab, data.spawnPosition, Quaternion.identity, PlayersRoot);
            go.name = $"Player_Bot_{botVisualIndex + 1:00}";
            var playerController = go.GetComponent<PlayerController>();
            if (playerController != null)
                Debug.LogError($"{botPrefab.name} should not include PlayerController.");

            var botController = go.GetComponent<BotController>();
            if (botController == null)
                Debug.LogError($"{botPrefab.name} is missing BotController.");
            else
                botController.SetNavigationGrid(_navigationGrid);
            var entity = go.GetComponent<PlayerEntity>();
            if (entity == null)
                return null;
            entity.ConfigureFromServer(data, go.GetComponent<ActorMovement>(), null);
            AttachNameView(entity, data.displayName, nameCamera);
            return entity;
        }

        private GameObject ResolveBotPrefab(int botIndex)
        {
            if (botPrefabs == null || botPrefabs.Length == 0)
                return localPlayerPrefab;
            var prefab = botPrefabs[Mathf.Abs(botIndex) % botPrefabs.Length];
            return prefab != null ? prefab : localPlayerPrefab;
        }

        private Camera ResolveNameCamera()
        {
            if (_cachedNameCamera != null)
                return _cachedNameCamera;
            _cachedNameCamera = Camera.main;
            return _cachedNameCamera;
        }

        private static void AttachNameView(PlayerEntity entity, string displayName, Camera targetCamera)
        {
            if (entity == null)
                return;
            var nameView = entity.GetComponent<PlayerNameView>();
            if (nameView == null)
            {
                Debug.LogError($"{entity.name} is missing PlayerNameView.");
                return;
            }
            nameView.Initialize(displayName, targetCamera);
        }
    }
}
