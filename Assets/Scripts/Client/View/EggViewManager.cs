using System.Collections.Generic;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Shared.Snapshots;
using UnityEngine;

namespace CollectEggs.Client.View
{
    public sealed class EggViewManager : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        private GameObject eggPrefab;

        private MaterialPropertyBlock _block;
        private readonly HashSet<string> _spawnedEggIds = new();

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
        }

        public void SetEggPrefab(GameObject prefab)
        {
            eggPrefab = prefab;
        }

        public void ClearTrackedEggsForNewMatch()
        {
            _spawnedEggIds.Clear();
        }

        public GameObject SpawnFromServerData(EggSpawnData data, Transform parent)
        {
            if (eggPrefab == null || string.IsNullOrEmpty(data.EggId))
                return null;
            if (_spawnedEggIds.Contains(data.EggId))
                return null;
            var egg = Instantiate(eggPrefab, data.Position, Quaternion.identity, parent);
            egg.name = data.EggId;
            egg.tag = "Egg";
            var entity = egg.GetComponent<EggEntity>();
            if (entity != null)
                entity.Configure(data.EggId, data.ScoreValue);
            _spawnedEggIds.Add(data.EggId);
            ApplyColor(egg, data.Color);
            return egg;
        }

        private void ApplyColor(GameObject egg, Color color)
        {
            if (egg == null || _block == null)
                return;
            var renderer = egg.GetComponent<Renderer>() ?? egg.GetComponentInChildren<Renderer>();
            if (renderer == null)
                return;
            _block.Clear();
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(BaseColorId))
                _block.SetColor(BaseColorId, color);
            else if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(ColorId))
                _block.SetColor(ColorId, color);
            else
                _block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(_block);
        }
    }
}
