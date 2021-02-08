using External;
using Mapbox.Utils;
using Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Services;
using UnityEngine;
using static Constants.DirectionConstants;
using static Constants.StringConstants;

namespace Fire
{
    public class FireNode
    {
        public string FuelModelCode;
        private Dictionary<Vector2, bool> _visitedNodes;           // referenced list of all visited nodes
        private Dictionary<Vector3, double> _ratesOfSpread;        // the rate of spread at the current node in all 4 directions
        private int _timeBurning;                                  // the number of minutes that this node has been burning
        private List<FireNode> _children;                          // all children nodes
        private Vector3 _center;                                   // doesn't consider the y value
        private readonly Dictionary<Vector3, double> _distancesTravelled; // the distance's travelled inside the current node
        private readonly FireNode _parent;                         // a reference to the parent node
        private readonly FuelMoistureProvider _fuelMoistureProvider;
        private readonly FuelModelProvider _fuelModelProvider;
        private readonly int _nodeSizeMetres;                      // the length/width of the node in the grid in metres
        private readonly RothermelService _rothermelService;       // service used for surface fire rate of spread equations
        private readonly Vector2d _latlon;                         // latitude and longitude of this node
        private readonly Weather _weatherReportAtIgnition;         // the weather forecast given at the point of ignition
        
        // the vector position of this node including the y coordinate
        public Vector3 Center
        {
            get => CalculateYCoord(_center);
            private set => _center = value;
        }
        
        public FireNode(FireNode parent, RothermelService rothermelService, Weather weatherReportAtIgnition,
            Vector3 center, int size, ref Dictionary<Vector2, bool> visited)
        {
            Center = center;
            _parent = parent;
            _rothermelService = rothermelService;
            _nodeSizeMetres = size;
            _visitedNodes = visited;
            _weatherReportAtIgnition = weatherReportAtIgnition;
            _children = new List<FireNode>();
            _latlon = _rothermelService.GetLatLonFromUnityCoords(Center);
            _fuelMoistureProvider = new FuelMoistureProvider();
            _fuelModelProvider = new FuelModelProvider();
            _distancesTravelled = new Dictionary<Vector3, double>  { {NORTH_VECTOR, 0}, {SOUTH_VECTOR, 0}, {EAST_VECTOR, 0}, {WEST_VECTOR, 0} };
            
            CalculateSpreadRates();
        }

        private void CalculateSpreadRates()
        {
            var model = _fuelModelProvider.GetFuelModelParameters(_latlon)
                .GetAwaiter().GetResult();
            FuelModelCode = model.code;
            var fuelMoisture = _fuelMoistureProvider.GetFuelMoistureContent(_latlon)
                .GetAwaiter().GetResult();
            _ratesOfSpread = _rothermelService.GetSpreadInCardinalDirectionsMetresPerMinute(_center, 
                model, _weatherReportAtIgnition, fuelMoisture);
        }
        
        public IEnumerator Update(int minutes, Action<List<FireNode>> callback = null)
        {
            var newNodes = new List<FireNode>();
            var directions = _ratesOfSpread.Keys.ToList();
            
            foreach (var direction in directions)
            {
                _distancesTravelled[direction] += _ratesOfSpread[direction] * minutes;

                var travelled = _distancesTravelled[direction];

                if (!(travelled >= _nodeSizeMetres)) continue;

                _ratesOfSpread.Remove(direction); // remove the key from the dict as we have already spread in this dir
                var numLeaps = (int) Math.Floor(travelled / _nodeSizeMetres);
                var newNodeCenter = CalculateNextCenter(direction, _center, numLeaps);

                var nodeCenterIn2d = new Vector2(newNodeCenter.x, newNodeCenter.z);
                
                if (_visitedNodes.ContainsKey(nodeCenterIn2d)) continue; // don't revisit node
                
                var remaining = travelled % _nodeSizeMetres;
                var newNode = new FireNode(this, _rothermelService, _weatherReportAtIgnition, newNodeCenter, 
                                    _nodeSizeMetres, ref _visitedNodes) 
                    {
                        _distancesTravelled = {[direction] = remaining}
                    };
                // add node only if burnable
                if (!NonBurnableCodes.Contains(newNode.FuelModelCode)) newNodes.Add(newNode);
                
                _visitedNodes.Add(nodeCenterIn2d, true);

                yield return null;
            }
            
            if (_ratesOfSpread.Keys.Any()) newNodes.Add(this); // if we are not finished burning add ourself back
            
            _children.AddRange(newNodes);
            callback?.Invoke(newNodes);
        }

        private Vector3 CalculateNextCenter(Vector3 direction, Vector3 thisCenter, int leap)
        {
            return thisCenter + (direction * (_nodeSizeMetres * leap));
        }

        private static Vector3 CalculateYCoord(Vector3 vec)
        {
            // cast a ray down to find the y coordinate
            const int altitudeAboveTerrain = 1000;
            var newVec = new Vector3(vec.x, altitudeAboveTerrain, vec.z);
            if (!Physics.Raycast(newVec, Vector3.down, out var hitInfo, int.MaxValue))
            {
                throw new Exception("Out of terrain bounds");
            }

            newVec.y = hitInfo.point.y;

            return newVec;
        }
    }
}