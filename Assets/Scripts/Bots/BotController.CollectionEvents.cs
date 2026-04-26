using CollectEggs.Gameplay.Collection;

namespace CollectEggs.Bots
{
    public partial class BotController
    {
        private void ForceRetargetAfterCollect()
        {
            _retargetTimer = 0f;
            _repathTimer = 0f;
            EvaluateTarget();
        }

        private void HandleExternalTargetInvalidationEarly()
        {
            if (_targetEgg == null)
                return;
            if (IsEggValid(_targetEgg))
                return;
            ClearTargetState();
            ForceRetargetAfterExternalInvalidation();
        }

        private void ForceRetargetAfterExternalInvalidation()
        {
            _retargetTimer = 0f;
            _repathTimer = 0f;
            EvaluateTarget();
        }

        private void SubscribeEggCollectedEvent()
        {
            var collection = EggCollectRequestController.Active;
            if (_eggCollectedSubscribed || collection == null)
                return;
            collection.EggCollectionConfirmed += HandleEggCollectionConfirmedEvent;
            _eggCollectedSubscribed = true;
        }

        private void UnsubscribeEggCollectedEvent()
        {
            var collection = EggCollectRequestController.Active;
            if (!_eggCollectedSubscribed || collection == null)
                return;
            collection.EggCollectionConfirmed -= HandleEggCollectionConfirmedEvent;
            _eggCollectedSubscribed = false;
        }

        private void HandleEggCollectionConfirmedEvent(string collectorId, string eggId, int _)
        {
            if (_targetEgg == null || string.IsNullOrEmpty(eggId))
                return;
            if (_entity != null && collectorId == _entity.PlayerId)
                return;
            if (_targetEgg.EggId != eggId)
                return;
            ClearTargetState();
            ForceRetargetAfterExternalInvalidation();
        }

        private void OnDisable() => UnsubscribeEggCollectedEvent();

        private void OnDestroy() => UnsubscribeEggCollectedEvent();
    }
}
