using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;

public class CameraScriptBehaviour : MonoBehaviour
{
    [SerializeField]
    float speed = 0.5f;
    [SerializeField]
    float sensitivity = 0.1f;
    bool isMousePressed = false;
    Vector3 North = new Vector3(0, 0, 1);
    AbstractMap Map;
    GameObject MapObject;
    Camera Cam;
    Vector3 AnchorPoint;
    Quaternion AnchorRot;

    private void Awake()
    {
        Cam = GetComponent<Camera>();
        Map = UnityEngine.Object.FindObjectOfType<AbstractMap>();
        MapObject = GameObject.Find("Map");
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
            move += Vector3.forward * speed;
        if (Input.GetKey(KeyCode.S))
            move -= Vector3.forward * speed;
        if (Input.GetKey(KeyCode.D))
            move += Vector3.right * speed;
        if (Input.GetKey(KeyCode.A))
            move -= Vector3.right * speed;
        if (Input.GetKey(KeyCode.E))
            move += Vector3.up * speed;
        if (Input.GetKey(KeyCode.Q))
            move -= Vector3.up * speed;
        transform.Translate(move);

        if (Input.GetMouseButtonDown(1))
        {
            AnchorPoint = new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
            AnchorRot = transform.rotation;
        }
        if (Input.GetMouseButton(1))
        {
            Quaternion rot = AnchorRot;
            Vector3 dif = AnchorPoint - new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
            rot.eulerAngles += dif * sensitivity;
            transform.rotation = rot;
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isMousePressed = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMousePressed = false;
        }

        if (isMousePressed)
        {

            Ray ray = Cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity))
            {
                FireBehaviour newFire = MapObject.AddComponent<FireBehaviour>();
                newFire.IgnitionPoint = hitInfo.point;
                newFire.Map = this.Map;
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
        return Map.WorldToGeoPosition(hitInfo.point);
    }

    float GetAltitudeInMeters(RaycastHit hitInfo)
    {
        return Map.QueryElevationInMetersAt(GetLatLon(hitInfo));
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

        return BearingBetweenInDegrees(North, upslopeFlat);
    }

}
