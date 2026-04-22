using System;
using UnityEngine;

namespace CollectEggs.Gameplay.Timer
{
    public class MatchTimer : MonoBehaviour
    {
        [SerializeField]
        private float matchDurationSeconds = 60f;

        public float RemainingSeconds { get; private set; }
        public bool IsRunning { get; private set; }
        public event Action MatchEnded;

        public void Begin()
        {
            RemainingSeconds = Mathf.Max(1f, matchDurationSeconds);
            IsRunning = true;
        }

        private void Update()
        {
            if (!IsRunning)
                return;
            RemainingSeconds -= Time.deltaTime;
            if (RemainingSeconds > 0f)
                return;
            RemainingSeconds = 0f;
            IsRunning = false;
            MatchEnded?.Invoke();
        }
    }
}
