using System;
using Assets.Scripts.Fire;
using UnityEngine;

namespace Assets.Scripts.Player
{
    [Serializable]
    public class PlayerSettings
    {
        [SerializeField]
        public float Speed;

        [SerializeField]
        public float Sensitivity;

        [SerializeField] public FireController FireController;


        public PlayerSettings()
        {
            Speed = 6.0f;
            Sensitivity = 0.45f;
            FireController = new FireController();
        }
    }
}

