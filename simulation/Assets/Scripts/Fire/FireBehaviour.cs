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
        private int _minutesPassed;                       // number of minutes since the fire was started
        private const int FireNodeSizeMetres = 100;       // the size of each node in metres
        private FireNode _fire;                           // the root node in the fire
        private RothermelService _rothermelService;       // service used for surface fire rate of spread calculations
        private Dictionary<Vector2, bool> _visitedNodes;  // internal fire nodes in the tree
        private List<FireNode> _perimeterNodes;           // root fire nodes in the tree
        private WeatherProvider _weatherProvider;         // provider to fetch weather forecast
        private GameObject _windArrow;                    // shows the wind speed and direction
        private GameObject _perimeterPoint;
        private List<GameObject> _perimeterPoints;

        public bool Active { get; private set; }

        private void Awake()
        {
            _visitedNodes = new Dictionary<Vector2, bool>();
            _perimeterNodes = new List<FireNode>();
            _perimeterPoints = new List<GameObject>();
            Active = false;
            _minutesPassed = 0;
            _perimeterPoint = Resources.Load(PerimeterPointPrefab) as GameObject;
        }

        public void Initialise(Vector3 ignitionPoint, AbstractMap map)
        {
            _visitedNodes.Add(new Vector2(ignitionPoint.x, ignitionPoint.z), false);
            _rothermelService = new RothermelService(map);
            _weatherProvider = new WeatherProvider();

            var latlon = _rothermelService.GetLatLonFromUnityCoords(ignitionPoint);
            var weatherReport = _weatherProvider.GetWeatherReport(latlon)
                .GetAwaiter().GetResult();
            
            CreateWindArrow(ignitionPoint, weatherReport);

            _fire = new FireNode(null, _rothermelService, weatherReport, ignitionPoint, 
                FireNodeSizeMetres, ref _visitedNodes);
            
            _perimeterNodes.Add(_fire);
            Active = true;
        }

        public void Reset()
        {
            Stop();
            _minutesPassed = 0;
            _fire = null;
            _visitedNodes.Clear();
            _perimeterNodes.Clear();
            _perimeterPoints.ForEach(point => point.Destroy());
            _perimeterPoints.Clear();
        }

        public void AdvanceFire(int minutes)
        {
            var newPerimeterNodes = new List<FireNode>();
            
            foreach (var node in _perimeterNodes)
            {
                StartCoroutine(node.Update(minutes, returnVal =>
                {
                    newPerimeterNodes.AddRange(returnVal);
                }));
            }

            _perimeterNodes = newPerimeterNodes;
            _minutesPassed += minutes;
        }
        /*
         
        public void PrintFireBoundary() =>
            _perimeterNodes.ForEach(node => Debug.DrawRay(node.Center, Vector3.up * 100, Color.red, 1000f));
         */

        public void PrintFireBoundary()
        {
            foreach (var node in _perimeterNodes)
            {
                var newPoint = Instantiate(_perimeterPoint, node.Center, Quaternion.identity);
                _perimeterPoints.Add(newPoint);
            }
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
