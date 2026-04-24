using UnityEngine;

namespace CollectEggs.Networking.Transport
{
    public sealed class LatencyProfile
    {
        public float MinDelaySeconds { get; }
        public float MaxDelaySeconds { get; }

        public LatencyProfile(float minDelaySeconds, float maxDelaySeconds)
        {
            MinDelaySeconds = minDelaySeconds;
            MaxDelaySeconds = maxDelaySeconds;
        }

        public float SampleDelaySeconds()
        {
            if (MaxDelaySeconds <= 0f)
                return 0f;
            return Random.Range(MinDelaySeconds, MaxDelaySeconds);
        }
    }
}
