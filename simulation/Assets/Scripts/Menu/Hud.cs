using System.Collections.Generic;
using Assets.Scripts.Player;
using Assets.Scripts.Fire;
using UnityEngine.UI;

namespace Assets.Scripts.Menu
{
    public class Hud
    {
        public List<FireBehaviour> AllFires;
        public PlayerAction CameraAction;
        public Button Ignite;
        public Button Pause;

        public Hud(List<FireBehaviour> allFires, PlayerAction action, Button ignite, Button pause)
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
                MenuConstants.GetIgnitingButtonColor(CameraAction.GetIgniting());

        }

        public void TogglePaused()
        {
            CameraAction.TogglePaused();

            var paused = CameraAction.GetPaused();
            Pause.GetComponent<Image>().color =
                MenuConstants.GetPausedButtonColor(paused);
            Pause.GetComponentInChildren<Text>().text =
                MenuConstants.GetPausedButtonText(paused);

            if (paused) AllFires.ForEach(fire => fire.Pause());
            else AllFires.ForEach(fire => fire.Play());
        }

    }
}
