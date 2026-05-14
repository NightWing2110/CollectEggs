using UnityEngine;

namespace CollectEggs.Networking.Transport
{
    public sealed class LatencyProfile
    {
        public float MinDelaySeconds { get; private set; }
        public float MaxDelaySeconds { get; private set; }

        public LatencyProfile(float minDelaySeconds, float maxDelaySeconds) => SetDelays(minDelaySeconds, maxDelaySeconds);

        public void SetDelays(float minDelaySeconds, float maxDelaySeconds)
        {
            var min = Mathf.Max(0f, Mathf.Min(minDelaySeconds, maxDelaySeconds));
            var max = Mathf.Max(min, Mathf.Max(minDelaySeconds, maxDelaySeconds));
            MinDelaySeconds = min;
            MaxDelaySeconds = max;
        }

        public float SampleDelaySeconds() => MaxDelaySeconds <= 0f ? 0f : Random.Range(MinDelaySeconds, MaxDelaySeconds);
    }
}
