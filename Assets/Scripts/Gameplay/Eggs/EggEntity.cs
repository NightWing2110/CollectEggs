using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.Gameplay.Eggs
{
    public class EggEntity : MonoBehaviour
    {
        private static readonly List<EggEntity> ActiveEggs = new();

        [SerializeField]
        private string eggId = "egg-0";

        [SerializeField]
        private bool isCollected;

        public string EggId => eggId;
        public static IReadOnlyList<EggEntity> Active => ActiveEggs;

        public void Configure(string id)
        {
            eggId = id;
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
