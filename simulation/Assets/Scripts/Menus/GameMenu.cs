using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.FireScripts;
using Assets.Scripts.CameraScripts;
using UnityEngine.UI;

namespace Assets.Scripts.Menus
{
    public class GameMenu
    {
        public SettingsMenu SettingsMenu;
        public HudMenu HudMenu;

        public GameMenu(ref List<FireBehaviour> allFires,
            CameraAction action, Button ignite, Button pause)
        {
            SettingsMenu = new SettingsMenu();
            HudMenu = new HudMenu(allFires, action, ignite, pause);
        }
    }
}
