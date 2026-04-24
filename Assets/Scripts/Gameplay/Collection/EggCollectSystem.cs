using CollectEggs.Gameplay.Players;
using UnityEngine;

namespace CollectEggs.Gameplay.Collection
{
    [DefaultExecutionOrder(15)]
    public sealed class EggCollectSystem : MonoBehaviour
    {
        [SerializeField]
        private float collectRadiusSlack = 0.14f;

        private float _authorityCollectRadius;
        private PlayerEntity _collector;

        public void Initialize(PlayerEntity collector) => _collector = collector;

        public void SetCollectRadiusFromAuthority(float radius) =>
            _authorityCollectRadius = Mathf.Max(0.01f, radius);

        private void Update()
        {
            if (_collector == null || _authorityCollectRadius <= 0f)
                return;
            var center = _collector.transform.position;
            EggCollectProximity.CollectAnyNearbyEgg(
                _collector,
                new Vector2(center.x, center.z),
                _authorityCollectRadius,
                collectRadiusSlack);
        }
    }
}
