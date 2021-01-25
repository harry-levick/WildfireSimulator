using System;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Unity.Map;
using Services;
using UnityEngine;

namespace Fire
{
    public class FireNode
    {
        public Vector3 Center;
        private readonly AbstractMap _map;
        private readonly FireNode _parent;
        private readonly List<FireNode> _children;
        private readonly List<FireNode> _newChildren;
        private readonly int _nodeSizeMetres; // the length/width of the node in the grid in metres
        private readonly Dictionary<Vector3, double> _minutesUntilSpread; // minutes until fire spreads to next node
        private Dictionary<Vector2, bool> _visitedNodes;
        private bool _isBurning;
        private int _timeBurning;
        private int _counter;

        public FireNode(FireNode parent, AbstractMap map, Vector3 center, int size, ref Dictionary<Vector2, bool> visited)
        {
            _children = new List<FireNode>();
            _newChildren = new List<FireNode>();
            _visitedNodes = visited;
            _parent = parent;
            _map = map;
            _nodeSizeMetres = size;
            Center = center;
            Debug.DrawRay(CalculateYCoord(Center), Vector3.up * 100, Color.red, 1000f);
            _isBurning = true;
            var rothermelService = new RothermelService(_map);

            // 1. Calculate rate of spread for each cardinal direction
            var ratesOfSpread = rothermelService.GetSpreadInCardinalDirectionsMetresPerMinute(Center);
            
            // 2. Calculate minutes until spread to next node in each 3 directions
            _minutesUntilSpread = CalculateTimeUntilSpread(ratesOfSpread);
        }

        public void UpdateTime()
        {
            // update node
            if (_isBurning)
            {
                _counter += 1;
                UpdateSpreadCounter();
            }
            
            // update children
            _children.ForEach(child => child.UpdateTime());
            // update the children with the new batch of children
            _children.AddRange(_newChildren);
            _newChildren.Clear();
        }

        private void UpdateSpreadCounter()
        {
            var keys = _minutesUntilSpread.Keys
                                                            .ToList()
                                                            .Where(key => _minutesUntilSpread[key] > 0.0);

            _isBurning = keys.Any();
            
            foreach (var key in keys)
            {
                _minutesUntilSpread[key] -= 1.0;
                
                if (!(_minutesUntilSpread[key] <= 0.0)) continue; // TODO: Check that this works
                
                // spread in direction
                var newNodeCenter = CalculateAdjacentCenter(key, Center);
                
                var nodeIn2d = new Vector2(newNodeCenter.x, newNodeCenter.z);
                if (_visitedNodes.ContainsKey(nodeIn2d)) continue; // don't revisit node
                
                _newChildren.Add(new FireNode(this, _map, newNodeCenter, 
                    _nodeSizeMetres, ref _visitedNodes));
                _visitedNodes.Add(nodeIn2d, true);
            }
        }

        private Vector3 CalculateAdjacentCenter(Vector3 direction, Vector3 thisCenter)
        {
            return thisCenter + (direction * _nodeSizeMetres);
        }

        private Dictionary<Vector3, double> CalculateTimeUntilSpread(Dictionary<Vector3, double> spreadRates)
        {
            return spreadRates.ToDictionary(entry => entry.Key, entry => _nodeSizeMetres / entry.Value);
        }
        
        private Vector3 CalculateYCoord(Vector3 vec)
        {
            const int altitudeAboveTerrain = 500;
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