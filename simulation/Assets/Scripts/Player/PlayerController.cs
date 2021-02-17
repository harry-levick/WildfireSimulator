using System;
using External;
using Fire;
using GameMenu;
using Mapbox.Unity.Map;
using Services;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        private Vector3 _anchorPoint;
        private Quaternion _anchorRot;
        private bool _mousePressed;
        private bool _allFiresPaused;
        private bool _gamePaused;

        [SerializeField] public PlayerSettings settings = new PlayerSettings();
        [SerializeField] public FireBehaviour fire;
        public IUnityService UnityService;
        public Hud hudMenu;
        public Settings settingsMenu;
        public new Camera camera;
        public AbstractMap map;
        private int _counter;
        
        private void Awake()
        {
            FuelModelProvider.ClearControlLines(); // clear all control lines set on previous instances
            
            map = FindObjectOfType<AbstractMap>();
            _mousePressed = false;
            UnityService = new UnityService();
            _counter = 0;
        }

        // Update is called once per frame
        private void Update()
        {
            if (!_gamePaused && fire.Active)
            {
                if (_counter == 10)
                {
                    fire.AdvanceFire(30);
                    _counter = 0;
                    fire.PrintFireBoundary();
                }
                else _counter += 1;
            }
            HandleLeftMouseButton();
            HandleRightMouseButton();
            HandleFireIgnition();

            transform.Translate(CalculateMovement());

            if (!hudMenu.holding) return;
            
            // Handle holding object logic
            var ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity))
            {
                hudMenu.holding.transform.position = hitInfo.point;
            }
                
            var scrollDelta = UnityService.GetMouseScrollDelta();
            var currentScale = hudMenu.holding.transform.localScale;
                
            if (scrollDelta != 0f)
            {
                if (UnityService.GetKey(KeyCode.LeftShift)) // rotate on shift scroll
                {
                    const float rotateDegrees = 90f;
                    hudMenu.holding.transform.Rotate(Vector3.up, Mathf.Sign(scrollDelta) * rotateDegrees);
                }
                else // scale on scroll
                {
                    var scaleDelta = new Vector3(2 * scrollDelta, scrollDelta, 0);
                    var resultScale = currentScale + scaleDelta;
                    // only scale down to zero, don't invert
                    if (resultScale.x >= 0 && resultScale.y >= 0) hudMenu.holding.transform.localScale = resultScale;
                }
            }
                
            if (UnityService.GetMouseButtonDown(0))
            {
                hudMenu.DropControlLine();
            }
        }
        
        private void HandleLeftMouseButton()
        {
            if (settingsMenu.IsOpen()) return;
            
            if (UnityService.GetMouseButtonDown(0))
            {
                _mousePressed = true;
            }
            else if (UnityService.GetMouseButtonUp(0))
            {
                _mousePressed = false;
            }
        }

        private Vector3 CalculateMovement()
        {
            if (settingsMenu.IsOpen()) return Vector3.zero;
            
            var move = Vector3.zero;
            if (UnityService.GetKey(KeyCode.W))
                move += Vector3.forward * settings.Speed;
            if (UnityService.GetKey(KeyCode.S))
                move += Vector3.back * settings.Speed;
            if (UnityService.GetKey(KeyCode.D))
                move += Vector3.right * settings.Speed;
            if (UnityService.GetKey(KeyCode.A))
                move += Vector3.left * settings.Speed;
            if (UnityService.GetKey(KeyCode.E))
                move += Vector3.up * settings.Speed;
            if (UnityService.GetKey(KeyCode.Q))
                move += Vector3.down * settings.Speed;

            return move;
        }

        private void HandleRightMouseButton()
        {
            if (settingsMenu.IsOpen()) return;
            
            if (UnityService.GetMouseButtonDown(1))
            {
                _anchorPoint = new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
                _anchorRot = transform.rotation;
            }

            if (UnityService.GetMouseButton(1))
            {
                var rot = _anchorRot;
                var dif = _anchorPoint - new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
                rot.eulerAngles += dif * settings.Sensitivity;
                transform.rotation = rot;
            }
            
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void HandleFireIgnition()
        {
            if (!CanIgnite() || EventSystem.current.IsPointerOverGameObject()) return;
            var ray = camera.ScreenPointToRay(Input.mousePosition);
                
            if (!Physics.Raycast(ray, out var hitInfo, Mathf.Infinity)) return;
                
            CreateFire(hitInfo.point);
            _mousePressed = false;
        }

        private bool CanIgnite()
        {
            return _mousePressed && hudMenu.IgniteClicked;
        }

        public void PauseAllFires(bool pause) => _gamePaused = pause;

        private void CreateFire(Vector3 ignitionPoint)
        {
            try
            {
                fire.Reset();
                fire.Initialise(ignitionPoint, map);
            }
            catch (Exception e)
            {
                // ignored - cant start fire here
                print(e.Message);
            }
        }

        public void Increment(int minutes)
        {
            fire.AdvanceFire(minutes);
        }
    }
}