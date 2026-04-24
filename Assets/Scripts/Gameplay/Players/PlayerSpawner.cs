using System.Collections.Generic;
using CollectEggs.Bots;
using CollectEggs.Gameplay.Movement;
using CollectEggs.Gameplay.Players.View;
using CollectEggs.Shared.Data;
using CollectEggs.Shared.Snapshots;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CollectEggs.Gameplay.Players
{
    public class PlayerSpawner : MonoBehaviour
    {
        private static readonly string[] BotPrefabAssetPaths =
        {
            "Assets/Prefabs/Bots/Player_Bot_01.prefab",
            "Assets/Prefabs/Bots/Player_Bot_02.prefab",
            "Assets/Prefabs/Bots/Player_Bot_03.prefab",
            "Assets/Prefabs/Bots/Player_Bot_04.prefab"
        };

        [SerializeField]
        private GameObject localPlayerPrefab;

        private Transform PlayersRoot { get; set; }
        public Transform EggsRoot { get; private set; }
        private Camera _cachedNameCamera;

        public void ApplyDefaultPlayerPrefab(GameObject prefab)
        {
            if (prefab != null)
                localPlayerPrefab = prefab;
        }

        public void RebuildSpawnParents()
        {
            Transform spawnGroups = null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == "SpawnGroups")
                {
                    spawnGroups = root.transform;
                    break;
                }
            }

            if (spawnGroups == null)
                spawnGroups = new GameObject("SpawnGroups").transform;

            var playersChild = spawnGroups.Find("Players");
            if (playersChild == null)
            {
                playersChild = new GameObject("Players").transform;
                playersChild.SetParent(spawnGroups, false);
            }

            var eggsChild = spawnGroups.Find("Eggs");
            if (eggsChild == null)
            {
                eggsChild = new GameObject("Eggs").transform;
                eggsChild.SetParent(spawnGroups, false);
            }

            PlayersRoot = playersChild;
            EggsRoot = eggsChild;
        }

        public PlayerEntity SpawnFromServerData(PlayerSpawnData data, int botVisualIndex)
        {
            if (PlayersRoot == null)
                RebuildSpawnParents();
            var nameCamera = ResolveNameCamera();
            return data.IsLocalPlayer
                ? SpawnLocalFromServer(data, nameCamera)
                : SpawnBotFromServer(data, botVisualIndex, nameCamera);
        }

        private PlayerEntity SpawnLocalFromServer(PlayerSpawnData data, Camera nameCamera)
        {
            if (localPlayerPrefab == null)
                return null;
            var go = Instantiate(localPlayerPrefab, data.SpawnPosition, Quaternion.identity, PlayersRoot);
            go.name = "Player_Local";
            var movement = go.GetComponent<ActorMovement>();
            var controller = go.GetComponent<PlayerController>();
            var entity = go.GetComponent<PlayerEntity>();
            if (entity == null)
                return null;
            entity.ConfigureFromServer(data, movement, controller);
            AttachNameView(entity, data.DisplayName, nameCamera);
            return entity;
        }

        private PlayerEntity SpawnBotFromServer(PlayerSpawnData data, int botVisualIndex, Camera nameCamera)
        {
            var botPrefab = ResolveBotPrefab(botVisualIndex, localPlayerPrefab);
            if (botPrefab == null)
                return null;
            var go = Instantiate(botPrefab, data.SpawnPosition, Quaternion.identity, PlayersRoot);
            go.name = $"Player_Bot_{botVisualIndex + 1:00}";
            var playerController = go.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
                Destroy(playerController);
            }

            // TODO Phase 2+: replace BotController local movement with server-driven ServerBotSimulation + PlayerSnapshot + client RemotePlayerView interpolation.
            if (go.GetComponent<BotController>() == null)
                go.AddComponent<BotController>();
            var entity = go.GetComponent<PlayerEntity>();
            if (entity == null)
                return null;
            entity.ConfigureFromServer(data, go.GetComponent<ActorMovement>(), null);
            AttachNameView(entity, data.DisplayName, nameCamera);
            return entity;
        }

        private static GameObject ResolveBotPrefab(int botIndex, GameObject fallbackPrefab)
        {
            if (fallbackPrefab == null)
                return null;
            if (BotPrefabAssetPaths.Length == 0)
                return fallbackPrefab;
            var assetPath = BotPrefabAssetPaths[Mathf.Abs(botIndex) % BotPrefabAssetPaths.Length];
#if UNITY_EDITOR
            var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return loaded != null ? loaded : fallbackPrefab;
#else
            return fallbackPrefab;
#endif
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
                nameView = entity.gameObject.AddComponent<PlayerNameView>();
            nameView.Initialize(displayName, targetCamera);
        }
    }
}
