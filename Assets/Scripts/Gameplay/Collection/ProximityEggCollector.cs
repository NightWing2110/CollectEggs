using CollectEggs.Gameplay.Players;
using UnityEngine;

namespace CollectEggs.Gameplay.Collection
{
    [DefaultExecutionOrder(15)]
    public sealed class ProximityEggCollector : MonoBehaviour
    {
        [SerializeField]
        private float collectRadiusSlack = 0.14f;

        private float _serverRuleCollectRadius;
        private PlayerEntity _collector;

        public void Initialize(PlayerEntity collector) => _collector = collector;

        public void SetCollectRadiusFromServerRules(float radius) =>
            _serverRuleCollectRadius = Mathf.Max(0.01f, radius);

        private void Update()
        {
            if (_collector == null || _serverRuleCollectRadius <= 0f)
                return;
            var center = _collector.transform.position;
            EggCollectProximity.RequestCollectFirstEggInRadius(
                _collector,
                new Vector2(center.x, center.z),
                _serverRuleCollectRadius,
                collectRadiusSlack);
        }
    }
}
