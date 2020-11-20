using UnityEngine;
using Mapbox.Unity.Map;

public class CameraController : MonoBehaviour
{
    [SerializeField] float speed = 0.5f;
    [SerializeField] float sensitivity = 0.1f;
    bool isMousePressed = false;

    AbstractMap map;
    Camera cam;
    Vector3 anchorPoint;
    Quaternion anchorRot;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        map = UnityEngine.Object.FindObjectOfType<AbstractMap>();
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
            anchorPoint = new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
            anchorRot = transform.rotation;
        }
        if (Input.GetMouseButton(1))
        {
            Quaternion rot = anchorRot;
            Vector3 dif = anchorPoint - new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
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
            var latlongDelta = GetTerrainLatLong(Input.mousePosition);
            print($"Latitude: {latlongDelta.x} , Longitude: {latlongDelta.y}");
        }
    }

    Mapbox.Utils.Vector2d GetTerrainLatLong(Vector3 mousePosScreen)
    {
        mousePosScreen.z = Camera.main.transform.localPosition.y;
        var mousePosTerrain = Camera.main.ScreenToWorldPoint(mousePosScreen);
        var latLongDelta = map.WorldToGeoPosition(mousePosTerrain);

        return latLongDelta;
    }
}

