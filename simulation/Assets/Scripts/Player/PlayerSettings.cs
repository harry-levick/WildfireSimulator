using System;
using Fire;
using UnityEngine;

namespace Player
{
    [Serializable]
    public class PlayerSettings
    {
        [SerializeField]
        public float Speed;

        [SerializeField]
        public float Sensitivity;



        public PlayerSettings()
        {
            Speed = 6.0f;
            Sensitivity = 0.45f;
        }
    }
}

