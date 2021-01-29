using Player;
using UnityEngine;
using UnityEngine.UI;

namespace GameMenu
{
    public class Hud : MonoBehaviour
    {
        public Button ignite;
        public Button pause;
        public Button settings;
        public Button next30Min;

        private bool _pauseClicked;
        
        public bool IgniteClicked { get; set; }
        public PlayerController playerController;
        
        public void ToggleIgniting()
        {
            IgniteClicked = !IgniteClicked;
            ignite.GetComponent<Image>().color =
                MenuConstants.GetIgnitingButtonColor(IgniteClicked);

        }

        public void TogglePaused()
        {
            _pauseClicked = !_pauseClicked;
            
            pause.GetComponent<Image>().color =
                MenuConstants.GetPausedButtonColor(_pauseClicked);
            pause.GetComponentInChildren<Text>().text =
                MenuConstants.GetPausedButtonText(_pauseClicked);

            playerController.PauseAllFires(_pauseClicked);
        }

        public void SetInteractable(bool interactable)
        {
            ignite.interactable = interactable;
            pause.interactable = interactable;
        }

        public void Next30Min()
        {
            playerController.Increment(30);
        }

    }
}
