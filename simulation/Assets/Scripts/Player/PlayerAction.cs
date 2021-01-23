
namespace Player
{
    public class PlayerAction
    {
        private bool _mousePressed;
        private bool _igniting;
        private bool _paused;
        private bool _settingsMenu;

        public PlayerAction()
        {
            _mousePressed = false;
            _igniting = false;
            _paused = false;
            _settingsMenu = false;
        }

        public void SetMousePressed(bool isPressed) => _mousePressed = isPressed;
        public bool GetMousePressed() => _mousePressed;
        public void SetIgniting(bool isIgniting) => _igniting = isIgniting;
        public bool GetIgniting() => _igniting;
        public void SetPaused(bool isPaused) => _paused = isPaused;
        public bool GetPaused() => _paused;
        public bool GetSettingsMenu() => _settingsMenu;
        public void SetSettingsMenu(bool inSettings) => _settingsMenu = inSettings;


        public bool IgniteFire() => GetMousePressed() && GetIgniting();
        public void ToggleIgniting() => SetIgniting(!GetIgniting());
        public void TogglePaused() => SetPaused(!GetPaused());
        public void ToggleSettings() => SetSettingsMenu(!GetSettingsMenu());

    }
}
