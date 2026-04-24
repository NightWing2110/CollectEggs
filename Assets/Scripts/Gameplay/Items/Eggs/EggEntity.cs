using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.Gameplay.Eggs
{
    public sealed class EggEntity : MonoBehaviour
    {
        private static readonly List<EggEntity> ActiveEggs = new();

        private string eggId = "";
        private int scoreValue;
        private bool isCollected;

        public string EggId => eggId;
        public int ScoreValue => scoreValue;

        public static IReadOnlyList<EggEntity> Active => ActiveEggs;

        public void Configure(string id, int score)
        {
            eggId = id ?? "";
            scoreValue = Mathf.Max(0, score);
            isCollected = false;
        }

        public bool MarkCollected()
        {
            if (isCollected)
                return false;
            isCollected = true;
            return true;
        }

        private void OnEnable()
        {
            isCollected = false;
            if (!ActiveEggs.Contains(this))
                ActiveEggs.Add(this);
        }

        private void OnDisable()
        {
            ActiveEggs.Remove(this);
        }
    }
}
