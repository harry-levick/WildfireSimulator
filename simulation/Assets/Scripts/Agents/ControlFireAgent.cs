using System;
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
        private const int maxX = 2000;
        private const int minX = 0;
        private const int maxZ = -1000;
        private const int minZ = -3000;
        private float maxContained = 0f;

        public override void OnEpisodeBegin()
        {
            fire.Reset();
            var ignitionPoint = FindIgnitionPoint(new Vector2(Random.Range(minX, maxX), Random.Range(minZ, maxZ)));
            fire.Initialise(ignitionPoint);
            FuelModelProvider.ClearControlLines();
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            // 1 action for x value of control line
            // 1 action for z value of control line
            // 1 action for rotation of control line
            var rotate = actions.ContinuousActions[2] > 0;
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

            var perimeter = fire.PerimeterNodes;
            for (var i = 0; i < 100; i++)
            {
                if (perimeter.Any())
                {
                    sensor.AddObservation(perimeter[perimeter.Count - 1].Center);
                    perimeter.RemoveAt(perimeter.Count - 1);
                } else sensor.AddObservation(fire.IgnitionPoint);
            }
        }

        private Vector3 FindIgnitionPoint(Vector2 point)
        {
            if (Physics.Raycast(new Vector3(point.x, 1000, point.y),
                Vector3.down, out var hitInfo, Mathf.Infinity))
            {
                return hitInfo.point;
            }
            
            throw new Exception("Out of bounds");
        }

        private void PlaceControlLine(Vector2 position, bool rotate)
        {
            const int aboveTerrain = 10000;
            const float rotateAngle = 90f;

            if (Physics.Raycast(new Vector3(position.x, aboveTerrain, position.y),
                Vector3.down, out var hitInfo, Mathf.Infinity))
            {
                var controlLine = Instantiate(Resources.Load(ControlLinePrefab) as GameObject);
                controlLine.transform.position = hitInfo.point;
                
                if (rotate) controlLine.transform.Rotate(Vector3.up, rotateAngle);
            }
            else
            {
                AddReward(-1f);
            }
        }
    }
}