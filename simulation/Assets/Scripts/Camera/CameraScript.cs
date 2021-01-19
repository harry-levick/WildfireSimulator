using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraScript : MonoBehaviour
{
    [SerializeField] private CameraSettings _settings = new CameraSettings();
    private CameraAction _cameraAction = new CameraAction();

    public Button IgniteButton;
    
    private Vector3 _north = Vector3.forward;
    private AbstractMap _map;
    private GameObject _mapObject;
    private Camera _camera;
    private Vector3 _anchorPoint;
    private Quaternion _anchorRot;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _map = FindObjectOfType<AbstractMap>();
        _mapObject = GameObject.Find("Map");
    }

    /* 
     * FixedUpdate runs once per physics frame. Should be used when applying
     * forces, torques, or other physics-related functions because you know 
     * it will be executed exactly in sync with the physics engine itself.
    */
    private void FixedUpdate()
    {
        HandleMovement();
    }


    // Update is called once per frame
    private void Update()
    {
        HandleLeftMouseButton();
        HandleFireIgnition();
    }



    public void ToggleIgniting()
    {
        _cameraAction.ToggleIgniting();
        IgniteButton.GetComponent<Image>().color =
            CameraUISettings.GetIgnitingButtonColor(_cameraAction.GetIgniting());

    }

    private void HandleLeftMouseButton()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _cameraAction.SetMousePressed(true);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _cameraAction.SetMousePressed(false);
        }
    }

    private void HandleMovement()
    {
        HandleMovementKeys();
        HandleRightMouseButton();
    }

    private void HandleMovementKeys()
    {
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            move += Vector3.forward * _settings.Speed;
        if (Input.GetKey(KeyCode.S))
            move -= Vector3.forward * _settings.Speed;
        if (Input.GetKey(KeyCode.D))
            move += Vector3.right * _settings.Speed;
        if (Input.GetKey(KeyCode.A))
            move -= Vector3.right * _settings.Speed;
        if (Input.GetKey(KeyCode.E))
            move += Vector3.up * _settings.Speed;
        if (Input.GetKey(KeyCode.Q))
            move -= Vector3.up * _settings.Speed;
        transform.Translate(move);
    }

    private void HandleRightMouseButton()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _anchorPoint = new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
            _anchorRot = transform.rotation;
        }
        if (Input.GetMouseButton(1))
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
