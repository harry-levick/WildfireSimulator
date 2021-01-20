using System;
using UnityEngine;

namespace Assets.Scripts.CameraScripts
{
    public static class CameraUISettings
    {
        private static Color _ignitingTrueColor = Color.red;
        private static Color _ignitingFalseColor = Color.gray;

        public static Color GetIgnitingButtonColor(bool isIgniting)
        {
            if (isIgniting) return _ignitingTrueColor;
            else return _ignitingFalseColor;
        }
    }
}
