using CollectEggs.Core;
using CollectEggs.Gameplay.Players;
using UnityEngine;

namespace CollectEggs.Gameplay.Eggs
{
    [DefaultExecutionOrder(15)]
    public class CollectSystem : MonoBehaviour
    {
        [SerializeField]
        private float collectRadius = 0.75f;

        private PlayerEntity _collector;

        public void Initialize(PlayerEntity collector)
        {
            _collector = collector;
        }

        private void Update()
        {
            if (_collector == null)
                return;
            if (GameManager.Instance == null || !GameManager.Instance.IsMatchRunning)
                return;
            var center = _collector.transform.position;
            var radiusSq = collectRadius * collectRadius;
            var eggs = EggEntity.Active;
            for (var i = 0; i < eggs.Count; i++)
            {
                var egg = eggs[i];
                if (egg == null || !egg.gameObject.activeInHierarchy)
                    continue;
                var delta = egg.transform.position - center;
                if (delta.sqrMagnitude > radiusSq)
                    continue;
                GameManager.Instance.CollectEgg(_collector, egg.gameObject);
            }
        }
    }
}
