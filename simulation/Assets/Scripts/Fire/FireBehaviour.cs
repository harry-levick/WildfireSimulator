using Mapbox.Unity.Map;
using Model;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using External;
using UnityEngine;
using static Constants.StringConstants;

namespace Fire
{
    public class FireBehaviour : MonoBehaviour
    {
        [SerializeField] public AbstractMap map;
        public List<FireNode> PerimeterNodes;             // root fire nodes in the tree
        public Vector3 IgnitionPoint;
        private int _minutesPassed;                       // number of minutes since the fire was started
        private const int FireNodeSizeMetres = 100;       // the size of each node in metres
        private FireNode _fire;                           // the root node in the fire
        private RothermelService _rothermelService;       // service used for surface fire rate of spread calculations
        private Dictionary<Vector2, bool> _visitedNodes;  // internal fire nodes in the tree
        private WeatherProvider _weatherProvider;         // provider to fetch weather forecast
        private GameObject _windArrow;                    // shows the wind speed and direction

        public bool Active { get; private set; }

        private void Awake()
        {
            _visitedNodes = new Dictionary<Vector2, bool>();
            PerimeterNodes = new List<FireNode>();
            Active = false;
            _minutesPassed = 0;
        }

        public void Initialise(Vector3 ignitionPoint)
        {
            _rothermelService = new RothermelService(map);
            _weatherProvider = new WeatherProvider();

            var latlon = _rothermelService.GetLatLonFromUnityCoords(ignitionPoint);
            var weatherReport = _weatherProvider.GetWeatherReport(latlon)
                .GetAwaiter().GetResult();
            
            CreateWindArrow(ignitionPoint, weatherReport);

            _fire = new FireNode(null, _rothermelService, weatherReport, ignitionPoint, 
                FireNodeSizeMetres, ref _visitedNodes);
            
            PerimeterNodes.Add(_fire);
            Active = true;
        }

        public void Reset()
        {
            Stop();
            _minutesPassed = 0;
            _fire = null;
            _visitedNodes.Clear();
            PerimeterNodes.Clear();
        }

        public void AdvanceFire(int minutes)
        {
            var newPerimeterNodes = new List<FireNode>();
            
            foreach (var node in PerimeterNodes)
            {
                StartCoroutine(node.Update(minutes, returnVal =>
                {
                    newPerimeterNodes.AddRange(returnVal);
                }));
            }

            PerimeterNodes = newPerimeterNodes;
            _minutesPassed += minutes;
        }
        
        public void PrintFireBoundary() =>
            PerimeterNodes.ForEach(node => Debug.DrawRay(node.Center, Vector3.up * 100, Color.red, 1000f));

        public float ContainedPercentage()
        {
            if (!PerimeterNodes.Any()) return 0f;

            var perimeterNodes = (float) PerimeterNodes.Count;
            var containedNodes = (float) PerimeterNodes.Sum(node => node.Contained ? 1 : 0);

            return containedNodes / perimeterNodes;
        }
        
        private void Stop() => Active = false;

        private void CreateWindArrow(Vector3 point, Weather weatherReport)
        {
            if (point == null)
                throw new Exception("Attempted to create wind arrow before ignition point defined.");
            
            if (_windArrow) { _windArrow.Destroy(); }
            
            var windArrow = Resources.Load(WindArrowPrefab) as GameObject;
            _windArrow = Instantiate(windArrow, point, Quaternion.identity);
            
            var yAxis = new Vector3(0, 1, 0);
            _windArrow.transform.Rotate(yAxis, weatherReport.current.wind_deg);
            _windArrow.GetComponentInChildren<TextMesh>().text = 
                $"{weatherReport.current.WindSpeedMetresPerSecond.ToString()}m/s";
        }
    }
}
