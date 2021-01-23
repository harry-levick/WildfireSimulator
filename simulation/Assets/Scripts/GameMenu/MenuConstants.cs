using System;
using UnityEngine;

namespace GameMenu
{
    public static class MenuConstants
    {
        private static Color _ignitingTrueColor = Color.red;
        private static Color _ignitingFalseColor = Color.gray;
        private static Color _pausedTrueColor = Color.green;
        private static Color _pausedFalseColor = Color.yellow;

        private static string _pausedTrueText = "Play";
        private static string _pausedFalseText = "Pause";

        public static Color GetIgnitingButtonColor(bool isIgniting)
        {
            if (isIgniting) return _ignitingTrueColor;
            return _ignitingFalseColor;
        }

        public static Color GetPausedButtonColor(bool isPaused)
        {
            if (isPaused) return _pausedTrueColor;
            return _pausedFalseColor;
        }

        public static string GetPausedButtonText(bool isPaused)
        {
            if (isPaused) return _pausedTrueText;
            return _pausedFalseText;
        }
    }
}
