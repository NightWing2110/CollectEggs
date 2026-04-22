using UnityEngine;
using UnityEngine.SceneManagement;

namespace CollectEggs.Gameplay.Players
{
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField]
        private string localPlayerId = "local";

        [SerializeField]
        private string localDisplayName = "Local Player";

        private Transform PlayersRoot { get; set; }
        public Transform EggsRoot { get; private set; }

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

        public GameObject Spawn(GameObject playerPrefab, Vector3 spawnPosition)
        {
            if (playerPrefab == null)
                return null;
            var player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity, PlayersRoot);
            player.name = "Player";
            var movement = player.GetComponent<PlayerMovement>();
            var controller = player.GetComponent<PlayerController>();
            var entity = player.GetComponent<PlayerEntity>();
            if (entity != null)
                entity.Configure(localPlayerId, localDisplayName, true, PlayerType.Local, movement, controller);
            return player;
        }
    }
}
