using CollectEggs.Gameplay.Players;
using UnityEngine;

namespace CollectEggs.Gameplay.Collection
{
    [DefaultExecutionOrder(15)]
    public class EggCollectSystem : MonoBehaviour
    {
        [SerializeField]
        private float collectRadius = 0.75f;

        [SerializeField]
        private float collectRadiusSlack = 0.14f;

        private PlayerEntity _collector;

        public void Initialize(PlayerEntity collector) => _collector = collector;

        private void Update()
        {
            if (_collector == null)
                return;
            var center = _collector.transform.position;
            EggCollectProximity.CollectAnyNearbyEgg(
                _collector,
                new Vector2(center.x, center.z),
                collectRadius,
                collectRadiusSlack);
        }
    }
}
