using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;

namespace GameMenu
{
    public class Settings : MonoBehaviour
    {
        private bool _open;
        public GameObject settingsMenu;
        public Hud hudMenu;

        public void ToggleSettings()
        {
            _open = !_open;

            settingsMenu.SetActive(_open);
            hudMenu.SetInteractable(!_open);
        }

        public bool IsOpen() => _open;
    }
}
