
namespace Assets.Scripts.CameraScripts
{
    public class CameraAction
    {
        private bool _mousePressed;
        private bool _igniting;

        public CameraAction()
        {
            _mousePressed = false;
            _igniting = false;
        }

        public void SetMousePressed(bool isPressed) => _mousePressed = isPressed;
        public bool GetMousePressed() => _mousePressed;
        public void SetIgniting(bool isIgniting) => _igniting = isIgniting;
        public bool GetIgniting() => _igniting;


        public bool IgniteFire() => GetMousePressed() && GetIgniting();
        public void ToggleIgniting() => SetIgniting(!GetIgniting());

    }
}
