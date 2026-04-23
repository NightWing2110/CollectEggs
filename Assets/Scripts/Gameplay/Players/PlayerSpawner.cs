using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using CollectEggs.Gameplay.Movement;
using CollectEggs.Gameplay.Players.View;
using CollectEggs.Bots;
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
        private string localPlayerId = "local";

        [SerializeField]
        private string localDisplayName = "You";

        [SerializeField]
        private int botCount = 4;
        // public int botCount = 4;

        [SerializeField]
        private float botSpawnRadius = 6f;

        [SerializeField]
        private List<Vector3> spawnPoints = new()
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(8.5f, 0f, -4f),
            new Vector3(-9f, 0f, 0f),
            new Vector3(-5f, 0f, 7f),
            new Vector3(7f, 0f, 7f)
        };

        private Transform PlayersRoot { get; set; }
        public Transform EggsRoot { get; private set; }
        private Camera _cachedNameCamera;

        public void RebuildSpawnParents()
        {
            Transform spawnGroups = (from root in SceneManager.GetActiveScene().GetRootGameObjects() where root.name == "SpawnGroups" select root.transform).FirstOrDefault();

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

        public List<PlayerEntity> SpawnPlayers(GameObject playerPrefab, Vector3 localSpawnPosition, out PlayerEntity localPlayer)
        {
            localPlayer = null;
            var result = new List<PlayerEntity>();
            if (playerPrefab == null)
                return result;
            var nameCamera = ResolveNameCamera();
            var spawnPositions = SpawnPositions(localSpawnPosition, botCount + 1);
            localPlayer = SpawnLocalPlayer(playerPrefab, spawnPositions[0], nameCamera);
            if (localPlayer != null)
                result.Add(localPlayer);

            for (var i = 0; i < botCount; i++)
            {
                var bot = SpawnBot(playerPrefab, spawnPositions[i + 1], i, nameCamera);
                if (bot != null)
                    result.Add(bot);
            }

            return result;
        }

        private PlayerEntity SpawnLocalPlayer(GameObject playerPrefab, Vector3 localSpawnPosition, Camera nameCamera)
        {
            var localGo = Instantiate(playerPrefab, localSpawnPosition, Quaternion.identity, PlayersRoot);
            localGo.name = "Player_Local";
            var localMovement = localGo.GetComponent<ActorMovement>();
            var localController = localGo.GetComponent<PlayerController>();
            var localEntity = localGo.GetComponent<PlayerEntity>();
            if (localEntity == null)
                return null;
            localEntity.Configure(localPlayerId, localDisplayName, true, PlayerType.Local, localMovement, localController);
            AttachNameView(localEntity, localDisplayName, nameCamera);
            return localEntity;
        }

        private PlayerEntity SpawnBot(GameObject playerPrefab, Vector3 botPosition, int botIndex, Camera nameCamera)
        {
            var botPrefab = ResolveBotPrefab(botIndex, playerPrefab);
            var botGo = Instantiate(botPrefab, botPosition, Quaternion.identity, PlayersRoot);
            botGo.name = $"Player_Bot_{botIndex + 1:00}";
            var botController = botGo.GetComponent<PlayerController>();
            if (botController != null)
            {
                botController.enabled = false;
                Destroy(botController);
            }
            var aiController = botGo.GetComponent<BotController>();
            if (aiController == null)
                botGo.AddComponent<BotController>();
            var botEntity = botGo.GetComponent<PlayerEntity>();
            if (botEntity == null)
                return null;
            var botId = $"bot-{botIndex + 1:00}";
            var displayName = $"Bot {botIndex + 1}";
            botEntity.Configure(botId, displayName, false, PlayerType.Bot, null, null);
            AttachNameView(botEntity, displayName, nameCamera);
            return botEntity;
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

        private List<Vector3> SpawnPositions(Vector3 fallbackCenter, int count)
        {
            var positions = new List<Vector3>(count);
            var shuffled = new List<Vector3>(spawnPoints ?? new List<Vector3>());
            Shuffle(shuffled);
            for (var i = 0; i < shuffled.Count && positions.Count < count; i++)
                positions.Add(shuffled[i]);

            for (var i = positions.Count; i < count; i++)
            {
                if (i == 0)
                {
                    positions.Add(fallbackCenter);
                    continue;
                }

                var botIndex = i - 1;
                var angle = botIndex * Mathf.PI * 2f / Mathf.Max(1, botCount);
                positions.Add(fallbackCenter + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * botSpawnRadius);
            }

            return positions;
        }

        private static void Shuffle(List<Vector3> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
