using Player;
using UnityEngine;
using UnityEngine.UI;

namespace GameMenu
{
    public class Hud : MonoBehaviour
    {
        public Button ignite;
        public Button pause;

        private bool _pauseClicked;
        
        public bool IgniteClicked { get; set; }
        private PlayerController _playerController;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
        }

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

            _playerController.PauseAllFires(_pauseClicked);
        }

    }
}
