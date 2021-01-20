using Assets.Scripts.FireScripts;
using Assets.Scripts.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.CameraScripts
{
    public class CameraScript : MonoBehaviour
    {
        [SerializeField] private CameraSettings _settings = new CameraSettings();
        private CameraAction _cameraAction = new CameraAction();

        public Button IgniteButton;
        public UnityService UnityService;
    
        private Vector3 _north = Vector3.forward;
        private GameObject _mapObject;
        private Camera _camera;
        private Vector3 _anchorPoint;
        private Quaternion _anchorRot;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _mapObject = GameObject.Find("Map");
            UnityService = new UnityService();
        }

        // Update is called once per frame
        private void Update()
        {
            HandleLeftMouseButton();
            HandleRightMouseButton();
            HandleFireIgnition();

            transform.Translate(CalculateMovement());
        }



        public void ToggleIgniting()
        {
            _cameraAction.ToggleIgniting();
            IgniteButton.GetComponent<Image>().color =
                CameraUISettings.GetIgnitingButtonColor(_cameraAction.GetIgniting());

        }

        private void HandleLeftMouseButton()
        {
            if (UnityService.GetMouseButtonDown(0))
            {
                _cameraAction.SetMousePressed(true);
            }
            else if (UnityService.GetMouseButtonUp(0))
            {
                _cameraAction.SetMousePressed(false);
            }
        }

        private Vector3 CalculateMovement()
        {
            Vector3 move = Vector3.zero;
            if (UnityService.GetKey(KeyCode.W))
                move += Vector3.forward * _settings.Speed;
            if (UnityService.GetKey(KeyCode.S))
                move -= Vector3.forward * _settings.Speed;
            if (UnityService.GetKey(KeyCode.D))
                move += Vector3.right * _settings.Speed;
            if (UnityService.GetKey(KeyCode.A))
                move -= Vector3.right * _settings.Speed;
            if (UnityService.GetKey(KeyCode.E))
                move += Vector3.up * _settings.Speed;
            if (UnityService.GetKey(KeyCode.Q))
                move -= Vector3.up * _settings.Speed;

            return move;
        }

        private void HandleRightMouseButton()
        {
            if (UnityService.GetMouseButtonDown(1))
            {
                _anchorPoint = new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
                _anchorRot = transform.rotation;
            }
            if (UnityService.GetMouseButton(1))
            {
                Quaternion rot = _anchorRot;
                Vector3 dif = _anchorPoint - new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
                rot.eulerAngles += dif * _settings.Sensitivity;
                transform.rotation = rot;
            }
        }

        private void HandleFireIgnition()
        {
            if (_cameraAction.IgniteFire())
            {
                if (EventSystem.current.IsPointerOverGameObject()) return;

                Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity))
                {
                    FireBehaviour newFire = _mapObject.AddComponent<FireBehaviour>();
                    newFire.Activate(hitInfo.point, ref _settings.FireController);
                }
            }
        }

    }
}
