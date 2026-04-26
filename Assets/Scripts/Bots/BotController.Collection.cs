using CollectEggs.Core;
using CollectEggs.Gameplay.Collection;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Players;
using UnityEngine;

namespace CollectEggs.Bots
{
    internal readonly struct BotCollectTargetRequest
    {
        public readonly PlayerEntity Entity;
        public readonly EggEntity TargetEgg;
        public readonly Transform Transform;
        public readonly CharacterController Controller;
        public readonly float CollectReach;
        public readonly float CollectProximitySlack;

        public BotCollectTargetRequest(
            PlayerEntity entity,
            EggEntity targetEgg,
            Transform transform,
            CharacterController controller,
            float collectReach,
            float collectProximitySlack)
        {
            Entity = entity;
            TargetEgg = targetEgg;
            Transform = transform;
            Controller = controller;
            CollectReach = collectReach;
            CollectProximitySlack = collectProximitySlack;
        }
    }

    public partial class BotController
    {
        private void LateUpdate()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsMatchRunning)
                return;
            if (_startDelayTimer > 0f)
                return;
            CollectTargetEggByProximity();
        }

        private void CollectTargetEggByProximity()
        {
            var targetBeforeCollect = _targetEgg;
            var request = new BotCollectTargetRequest(
                    _entity,
                    targetBeforeCollect,
                    transform,
                    _cc,
                    _authorityCollectReach,
                    collectProximitySlack);
            if (!CollectTarget(request))
                return;
            if (targetBeforeCollect != null)
                ClearTargetState();

            ForceRetargetAfterCollect();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var targetBeforeCollect = _targetEgg;
            if (!CollectFromCollider(_entity, hit.collider))
                return;
            if (targetBeforeCollect != null && hit.collider != null && hit.collider.gameObject == targetBeforeCollect.gameObject)
                ClearTargetState();

            ForceRetargetAfterCollect();
        }

        private static bool CollectTarget(BotCollectTargetRequest request)
        {
            var refXZ = ResolveHorizontalReference(request.Transform, request.Controller);
            var extraRadius = request.Controller != null ? request.Controller.radius * 0.35f : 0f;
            return EggCollectProximity.RequestCollectTargetEgg(
                request.Entity,
                request.TargetEgg,
                refXZ,
                request.CollectReach,
                request.CollectProximitySlack,
                extraRadius);
        }

        private static bool CollectFromCollider(PlayerEntity entity, Collider collider) => EggCollectProximity.RequestCollectFromEggCollider(entity, collider);

        private static Vector2 ResolveHorizontalReference(Transform transform, CharacterController controller)
        {
            if (controller == null)
                return new Vector2(transform.position.x, transform.position.z);
            var worldCenter = transform.TransformPoint(controller.center);
            return new Vector2(worldCenter.x, worldCenter.z);
        }
    }
}
