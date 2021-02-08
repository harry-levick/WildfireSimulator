using Mapbox.Unity.Map;
using Model;
using Services;
using System;
using System.Collections.Generic;
using External;
using UnityEngine;
using static Constants.StringConstants;

namespace Fire
{
    public class FireBehaviour : MonoBehaviour
    {
        private bool _active;                             // fire currently burning
        private int _minutesPassed;                       // number of minutes since the fire was started
        private const int FireNodeSizeMetres = 100;       // the size of each node in metres
        private FireNode _fire;                           // the root node in the fire
        private RothermelService _rothermelService;       // service used for surface fire rate of spread calculations
        private Dictionary<Vector2, bool> _visitedNodes;  // internal fire nodes in the tree
        private List<FireNode> _perimeterNodes;           // root fire nodes in the tree
        private WeatherProvider _weatherProvider;         // provider to fetch weather forecast
        
        private void Awake()
        {
            _visitedNodes = new Dictionary<Vector2, bool>();
            _perimeterNodes = new List<FireNode>();
            _active = false;
            _minutesPassed = 0;
        }

        public void Initialise(Vector3 ignitionPoint, AbstractMap map)
        {
            _rothermelService = new RothermelService(map);
            _weatherProvider = new WeatherProvider();

            var latlon = _rothermelService.GetLatLonFromUnityCoords(ignitionPoint);
            var weatherReport = _weatherProvider.GetWeatherReport(latlon)
                .GetAwaiter().GetResult();
            
            CreateWindArrow(ignitionPoint, weatherReport);

            _fire = new FireNode(null, _rothermelService, weatherReport, ignitionPoint, 
                FireNodeSizeMetres, ref _visitedNodes);
            
            _perimeterNodes.Add(_fire);
            _active = true;
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
        
        public void PrintFireBoundary() =>
            _perimeterNodes.ForEach(node => Debug.DrawRay(node.Center, Vector3.up * 100, Color.red, 1000f));

        public void Stop() => _active = false;

        private static void CreateWindArrow(Vector3 point, Weather weatherReport)
        {
            if (point == null)
                throw new Exception("Attempted to create wind arrow before ignition point defined.");
            
            var windArrow = Resources.Load(WindArrowPrefab) as GameObject;
            var arrow = Instantiate(windArrow, point, Quaternion.identity);
            
            var yAxis = new Vector3(0, 1, 0);
            arrow.transform.Rotate(yAxis, weatherReport.current.wind_deg);
            arrow.GetComponentInChildren<TextMesh>().text = 
                $"{weatherReport.current.WindSpeedMetresPerSecond.ToString()}m/s";
        }
    }
}
