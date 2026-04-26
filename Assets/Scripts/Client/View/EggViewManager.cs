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
        private readonly Dictionary<string, GameObject> _spawnedEggs = new();

        private void Awake() => _block = new MaterialPropertyBlock();

        public void SetEggPrefab(GameObject prefab) => eggPrefab = prefab;

        public void ClearTrackedEggsForNewMatch() => _spawnedEggs.Clear();

        public void SpawnFromServerData(EggSpawnData data, Transform parent)
        {
            if (eggPrefab == null || string.IsNullOrEmpty(data.eggId)) return;
            if (_spawnedEggs.ContainsKey(data.eggId)) return;
            Spawn(data.eggId, data.position, data.color, data.scoreValue, parent);
        }

        public void SpawnFromSnapshot(EggSnapshot snapshot, Transform parent)
        {
            if (snapshot is not { isActive: true } || string.IsNullOrEmpty(snapshot.eggId)) return;
            if (_spawnedEggs.ContainsKey(snapshot.eggId)) return;
            Spawn(snapshot.eggId, snapshot.position, snapshot.color, snapshot.scoreValue, parent);
        }

        private void Spawn(string eggId, Vector3 position, Color color, int scoreValue, Transform parent)
        {
            var egg = Instantiate(eggPrefab, position, Quaternion.identity, parent);
            egg.name = eggId;
            egg.tag = "Egg";
            var entity = egg.GetComponent<EggEntity>();
            if (entity != null)
                entity.Configure(eggId, scoreValue);
            _spawnedEggs[eggId] = egg;
            ApplyColor(egg, color);
        }

        public void RemoveFromServerData(string eggId)
        {
            if (string.IsNullOrEmpty(eggId))
                return;
            if (!_spawnedEggs.Remove(eggId, out var egg) || egg == null)
                return;
            egg.SetActive(false);
            Destroy(egg);
        }

        private void ApplyColor(GameObject egg, Color color)
        {
            if (egg == null || _block == null)
                return;
            var renderer = egg.GetComponent<Renderer>() ?? egg.GetComponentInChildren<Renderer>();
            if (renderer == null)
                return;
            _block.Clear();
            var material = renderer.sharedMaterial;
            if (material != null && material.HasProperty(BaseColorId))
                _block.SetColor(BaseColorId, color);
            else if (material != null && material.HasProperty(ColorId))
                _block.SetColor(ColorId, color);
            else
                _block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(_block);
        }
    }
}
