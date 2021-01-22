using System;
using Assets.Scripts.FireScripts;
using UnityEngine;

namespace Assets.Scripts.CameraScripts
{
    [Serializable]
    public class CameraSettings
    {
        [SerializeField]
        public float Speed;

        [SerializeField]
        public float Sensitivity;

        [SerializeField] public FireController FireController;


        public CameraSettings()
        {
            Speed = 6.0f;
            Sensitivity = 0.45f;
            FireController = new FireController();
        }
    }
}

