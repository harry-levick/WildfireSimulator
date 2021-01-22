using System;
using System.Collections.Generic;
using Assets.Scripts.CameraScripts;
using Assets.Scripts.FireScripts;
using UnityEngine.UI;

namespace Assets.Scripts.Menus
{
    public class HudMenu
    {
        public List<FireBehaviour> AllFires;
        public CameraAction CameraAction;
        public Button Ignite;
        public Button Pause;

        public HudMenu(List<FireBehaviour> allFires, CameraAction action, Button ignite, Button pause)
        {
            AllFires = allFires;
            CameraAction = action;
            Ignite = ignite;
            Pause = pause;
        }

        public void ToggleIgniting()
        {
            CameraAction.ToggleIgniting();
            Ignite.GetComponent<Image>().color =
                CameraUISettings.GetIgnitingButtonColor(CameraAction.GetIgniting());

        }

        public void TogglePaused()
        {
            CameraAction.TogglePaused();

            var paused = CameraAction.GetPaused();
            Pause.GetComponent<Image>().color =
                CameraUISettings.GetPausedButtonColor(paused);
            Pause.GetComponentInChildren<Text>().text =
                CameraUISettings.GetPausedButtonText(paused);

            if (paused) AllFires.ForEach(fire => fire.Pause());
            else AllFires.ForEach(fire => fire.Play());
        }

        public void ToggleSettings()
        {

        }
    }
}
