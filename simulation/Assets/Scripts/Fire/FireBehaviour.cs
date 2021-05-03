using Mapbox.Unity.Map;
using Model;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using External;
using Mapbox.Utils;
using UnityEngine;
using static Constants.StringConstants;
using Random = UnityEngine.Random;

namespace Fire
{
    public class FireBehaviour : MonoBehaviour
    {
        private bool _isLearning;
        public Vector3 ignitionPoint;                     // the point at which the fire was created.
        public AbstractMap map;
        public Weather weatherReport;                     // the weather report generated at ignition.
        public int FireNodeSizeMetres = 200;              // the size of each node in metres.
        public bool Active { get; private set; }
        private int _minutesPassed;                       // number of minutes since the fire was started.
        private FireNode _fire;                           // the root node in the fire.
        private RothermelService _rothermelService;       // service used for surface fire rate of spread calculations.
        private Dictionary<Vector2, bool> _visitedNodes;  // internal fire nodes in the tree.
        private List<FireNode> _perimeterNodes;           // root fire nodes in the tree.
        private GameObject _windArrow;                    // shows the wind speed and direction.
        private GameObject _perimeterPoint;
        private List<GameObject> _perimeterPoints;
        public Guid controlLineId;                        // identify which instance to add / remove control lines from
        private int _counter = 0;

        public int NumPerimeterNodes => _perimeterNodes.Count;
        
        private void Awake()
        {
            Active = false;
            _minutesPassed = 0;
            _perimeterPoint = Resources.Load(PerimeterPointPrefab) as GameObject;
            controlLineId = Guid.NewGuid();

            _isLearning = true;
            //_isLearning = GameObject.Find("Agent") != null;
        }

        public void Initialise(Vector3 point)
        {
            ignitionPoint = point;
            _perimeterNodes = new List<FireNode>();
            _perimeterPoints = new List<GameObject>();
            _visitedNodes = new Dictionary<Vector2, bool>
            {
                {new Vector2(ignitionPoint.x, ignitionPoint.z), false}
            };
            
            _rothermelService = new RothermelService(map);

            var latlon = _rothermelService.GetLatLonFromUnityCoords(ignitionPoint);
            weatherReport = GetWeatherReport(latlon);
            
            CreateWindArrow(weatherReport);

            _fire = new FireNode(null, _rothermelService, weatherReport, ignitionPoint, 
                FireNodeSizeMetres, controlLineId, ref _visitedNodes);
            
            _perimeterNodes.Add(_fire);
            Active = true;
        }

        private void Update()
        {
            if (!Active) return;
            
            if (_counter == 10)
            {
                AdvanceFire(60);
                _counter = 0;
                PrintFireBoundary();
            }
            else _counter += 1;
        }

        public void Reset()
        {
            Stop();
            _minutesPassed = 0;
            _fire = null;
            
            _perimeterPoints?.ForEach(point => point.Destroy());
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
            Debug.Log($"Minutes: {_minutesPassed}");
        }

        public void PrintFireBoundary()
        {
            //_perimeterPoints.ForEach(point => point.Destroy());
            //_perimeterPoints.Clear();
            
            foreach (var nodeCenter in _perimeterNodes.Select(node => Instantiate(_perimeterPoint, node.Center, Quaternion.identity)))
            {
                _perimeterPoints.Add(nodeCenter);
            }
        }

        public void PutControlLine(Vector2d min, Vector2d max)
        {
            FuelModelProvider.PutControlLine(min, max, controlLineId.ToString());
        }
        
        private void Stop() => Active = false;

        private void CreateWindArrow(Weather weatherReport)
        {
            if (ignitionPoint == null)
                throw new Exception("Attempted to create wind arrow before ignition point defined.");
            
            if (_windArrow) { _windArrow.Destroy(); }
            
            var windArrow = Resources.Load(WindArrowPrefab) as GameObject;
            _windArrow = Instantiate(windArrow);
            _windArrow.transform.localPosition = ignitionPoint;
            
            var yAxis = new Vector3(0, 1, 0);
            _windArrow.transform.Rotate(yAxis, weatherReport.current.wind_deg);
            _windArrow.GetComponentInChildren<TextMesh>().text = 
                $"{weatherReport.current.WindSpeedMetresPerSecond.ToString()}m/s";
        }

        private Weather GetWeatherReport(Vector2d latlon)
        {

            return new WeatherProvider().GetWeatherReport(latlon)
                .GetAwaiter().GetResult();

        }

        public Vector3 GetClosestPerimeterNode(Vector3 point)
        {
            var perimeterPoints = _perimeterNodes.Select(node => new Vector3(node.Center.x, point.y, node.Center.z)).ToList();
            perimeterPoints = perimeterPoints.OrderBy(p => Vector3.Distance(p, point)).ToList();

            return perimeterPoints.First();
        }

        public Vector3 GetFurthestPerimeterNode(Vector3 point)
        {
            var perimeterPoints = _perimeterNodes.Select(node => new Vector3(node.Center.x, point.y, node.Center.z))
                .ToList();
            perimeterPoints = perimeterPoints.OrderBy(p => Vector3.Distance(p, point)).ToList();
            return perimeterPoints.Last();
        }
    }
}
