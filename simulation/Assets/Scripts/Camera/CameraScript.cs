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
    void FixedUpdate()
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


    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _cameraAction.SetMousePressed(true);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _cameraAction.SetMousePressed(false);
        }

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

    float BearingBetweenInDegrees(Vector3 a, Vector3 b)
    {
        Vector3 normal = Vector3.up;
        // angle in [0, 180]
        float angle = Vector3.Angle(a, b);
        float sign = Mathf.Sign(Vector3.Dot(normal, Vector3.Cross(a, b)));

        // angle in [-179, 180]
        float signedAngle = angle * sign;

        // angle in [0, 360]
        float bearing = (signedAngle + 360) % 360;
        return bearing;
    }

    Vector2d GetLatLon(RaycastHit hitInfo)
    {
        return _map.WorldToGeoPosition(hitInfo.point);
    }

    float GetAltitudeInMeters(RaycastHit hitInfo)
    {
        return _map.QueryElevationInMetersAt(GetLatLon(hitInfo));
    }

    float GetSlopeInDegrees(RaycastHit hitInfo)
    {
        Vector3 normal = hitInfo.normal;
        return Vector3.Angle(normal, Vector3.up);
    }

    float GetSlopeBearingInDegrees(RaycastHit hitInfo)
    {
        Vector3 normal = hitInfo.normal;

        Vector3 left = Vector3.Cross(normal, Vector3.down);
        Vector3 upslope = Vector3.Cross(normal, left);
        Vector3 upslopeFlat = new Vector3(upslope.x, 0, upslope.z).normalized;

        return BearingBetweenInDegrees(_north, upslopeFlat);
    }

    public void ToggleIgniting()
    {
        _cameraAction.ToggleIgniting();
        if (_cameraAction.GetIgniting())
        {
            IgniteButton.GetComponent<Image>().color = Color.red;
        } else
        {
            IgniteButton.GetComponent<Image>().color = Color.gray;
        }
    }

}
