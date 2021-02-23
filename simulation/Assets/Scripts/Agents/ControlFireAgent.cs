using System;
using System.Collections.Generic;
using System.Linq;
using External;
using Fire;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;
using static Constants.StringConstants;

namespace Agents
{
    public class ControlFireAgent : Agent
    {

        [SerializeField] public FireBehaviour fire;
        private List<GameObject> _controlLines = new List<GameObject>();
        
        private const int maxX = 1000;
        private const int minX = -1000;
        private const int maxZ = 1000;
        private const int minZ = -1000;
        private float maxContained = 0f;

        public override void OnEpisodeBegin()
        {
            try
            {
                FuelModelProvider.ClearControlLines();
                _controlLines.ForEach(line => line.Destroy());
                _controlLines.Clear();
                fire.Reset();
                var position = transform.position;
                var ignitionPoint2d = new Vector2(position.x + Random.Range(minX, maxX),
                    position.z + Random.Range(minZ, maxZ));
                
                var ignitionPoint = FindIgnitionPoint(ignitionPoint2d);

                fire.Initialise(ignitionPoint);
                
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                EndEpisode();
            }

        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            Debug.Log($"{actions.DiscreteActions[0]} ; x: {actions.ContinuousActions[0]} , z: {actions.ContinuousActions[1]}");
            // 1 action for x value of control line
            // 1 action for z value of control line
            // 1 action for rotation of control line
            var rotate = actions.DiscreteActions[0] == 1;
            PlaceControlLine(
                new Vector2(actions.ContinuousActions[0], actions.ContinuousActions[1]), 
                rotate
                );
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // add all of the data that the agent might need for learning.
            // - all perimeter nodes of the fire
            //      - position of node
            //      - rates of spread of node
            // - wind speed and direction
            // - all previously placed control lines

            var contained = fire.ContainedPercentage();
            if (contained > maxContained)
            {
                maxContained = contained;
                AddReward(1f);
            }
            else
            {
                AddReward(-1f);
            }

            if (contained >= 0.5f)
            {
                SetReward(10f);
                EndEpisode();
            }

            var perimeter = fire.GetPerimeterNodes();
            for (var i = 0; i < 100; i++)
            {
                sensor.AddObservation(i < perimeter.Count ? perimeter[i].Center : fire.IgnitionPoint);
            }
        }

        private Vector3 FindIgnitionPoint(Vector2 point)
        {
            var start = new Vector3(point.x, 600, point.y);
            if (Physics.Raycast(start, Vector3.down, out var hitInfo, Mathf.Infinity))
            {
                return hitInfo.point;
            }
            
            throw new Exception($"Out of bounds: {start}");
        }

        private void PlaceControlLine(Vector2 position, bool rotate)
        {
            const int aboveTerrain = 10000;
            const float rotateAngle = 90f;

            if (Physics.Raycast(new Vector3(transform.position.x + position.x, aboveTerrain, transform.position.z + position.y),
                Vector3.down, out var hitInfo, Mathf.Infinity))
            {
                var controlLine = Instantiate(Resources.Load(ControlLinePrefab) as GameObject);
                controlLine.transform.position = hitInfo.point;
                
                if (rotate) controlLine.transform.Rotate(Vector3.up, rotateAngle);
                
                _controlLines.Add(controlLine);
            }
            else
            {
                AddReward(-1f);
            }
        }
    }
}