using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Fire;
using Assets.Scripts.Player;
using UnityEngine.UI;

namespace Assets.Scripts.Menu
{
    public class Game
    {
        public Settings SettingsMenu;
        public Hud HudMenu;

        public Game(ref List<FireBehaviour> allFires,
            PlayerAction action, Button ignite, Button pause)
        {
            SettingsMenu = new Settings();
            HudMenu = new Hud(allFires, action, ignite, pause);
        }

        public void ToggleSettings()
        {

        }
    }
}
