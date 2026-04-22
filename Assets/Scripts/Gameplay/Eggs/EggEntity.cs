using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.Gameplay.Eggs
{
    public class EggEntity : MonoBehaviour
    {
        private static readonly List<EggEntity> ActiveEggs = new List<EggEntity>();

        [SerializeField]
        private string eggId = "egg-0";

        public string EggId => eggId;
        public static IReadOnlyList<EggEntity> Active => ActiveEggs;

        public void Configure(string id)
        {
            eggId = id;
        }

        private void OnEnable()
        {
            if (!ActiveEggs.Contains(this))
                ActiveEggs.Add(this);
        }

        private void OnDisable()
        {
            ActiveEggs.Remove(this);
        }
    }
}
