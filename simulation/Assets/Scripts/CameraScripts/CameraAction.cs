
namespace Assets.Scripts.CameraScripts
{
    public class CameraAction
    {
        private bool _mousePressed;
        private bool _igniting;
        private bool _paused;

        public CameraAction()
        {
            _mousePressed = false;
            _igniting = false;
            _paused = false;
        }

        public void SetMousePressed(bool isPressed) => _mousePressed = isPressed;
        public bool GetMousePressed() => _mousePressed;
        public void SetIgniting(bool isIgniting) => _igniting = isIgniting;
        public bool GetIgniting() => _igniting;
        public void SetPaused(bool isPaused) => _paused = isPaused;
        public bool GetPaused() => _paused;


        public bool IgniteFire() => GetMousePressed() && GetIgniting();
        public void ToggleIgniting() => SetIgniting(!GetIgniting());
        public void TogglePaused() => SetPaused(!GetPaused());

    }
}
