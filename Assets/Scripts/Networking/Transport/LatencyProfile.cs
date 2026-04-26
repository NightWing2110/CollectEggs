using UnityEngine;

namespace CollectEggs.Networking.Transport
{
    public sealed class LatencyProfile
    {
        private float MinDelaySeconds { get; }
        private float MaxDelaySeconds { get; }

        public LatencyProfile(float minDelaySeconds, float maxDelaySeconds)
        {
            MinDelaySeconds = minDelaySeconds;
            MaxDelaySeconds = maxDelaySeconds;
        }

        public float SampleDelaySeconds() => MaxDelaySeconds <= 0f ? 0f : Random.Range(MinDelaySeconds, MaxDelaySeconds);
    }
}
