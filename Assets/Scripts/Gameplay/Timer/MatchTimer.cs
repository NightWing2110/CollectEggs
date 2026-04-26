using UnityEngine;

namespace CollectEggs.Gameplay.Timer
{
    public sealed class MatchTimer : MonoBehaviour
    {
        public float RemainingSeconds { get; private set; }
        public bool IsRunning { get; private set; }

        public void StartFromServerRules(float durationSeconds)
        {
            RemainingSeconds = Mathf.Max(0f, durationSeconds);
            IsRunning = RemainingSeconds > 0f;
        }

        public void SetRemainingSecondsFromNetwork(float remainingSeconds)
        {
            RemainingSeconds = Mathf.Max(0f, remainingSeconds);
            IsRunning = RemainingSeconds > 0f;
        }

        public void StopFromServerState()
        {
            RemainingSeconds = 0f;
            IsRunning = false;
        }

        private void Update()
        {
            if (!IsRunning)
                return;

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
        }
    }
}
