using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.Gameplay.Eggs
{
    public sealed class EggEntity : MonoBehaviour
    {
        private static readonly List<EggEntity> ActiveEggs = new();

        private string _eggId = "";
        private int _pointValue;

        public string EggId => _eggId;
        public int PointValue => _pointValue;

        public static IReadOnlyList<EggEntity> Active => ActiveEggs;

        public void Configure(string id, int score)
        {
            _eggId = id ?? "";
            _pointValue = Mathf.Max(0, score);
        }

        private void OnEnable()
        {
            if (!ActiveEggs.Contains(this))
                ActiveEggs.Add(this);
        }

        private void OnDisable() => ActiveEggs.Remove(this);
    }
}
