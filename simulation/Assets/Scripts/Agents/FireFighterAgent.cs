using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fire;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;
using static Constants.StringConstants;

namespace Agents
{
    public class FireFighterAgent : Agent
    {
        public FireBehaviour fire;
        private GameObject controlLine;
        private List<GameObject> controlLines = new List<GameObject>();

        public void Awake()
        {
            controlLine = Resources.Load(ControlLinePrefab) as GameObject;
        }

        public override void OnEpisodeBegin()
        {
            const float max = 1600;
            const float min = -1600;

            var localPosition = new Vector3(Random.Range(min, max), 500, Random.Range(min, max));
            transform.localPosition = localPosition;

            controlLines.ForEach(line => line.Destroy());
            controlLines.Clear();
            
            fire.Reset();
            fire.Initialise(GetTerrainPoint());
        }
        
        private Vector3 GetTerrainPoint()
        {
            if (Physics.Raycast(transform.position, transform.up * -1, out var hitInfo, Mathf.Infinity))
            {
                return hitInfo.point;
            }

            throw new Exception("Out of world bounds.");
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(fire.weatherReport.current.wind_deg);
            sensor.AddObservation(fire.weatherReport.current.wind_speed);
            sensor.AddObservation(fire.ignitionPoint);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var moveX = actions.ContinuousActions[0];
            var moveZ = actions.ContinuousActions[1];

            const float moveSpeed = 400f;
            const float rotateDegrees = 90f;

            transform.position += new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;

            var placeControlLine = actions.DiscreteActions[0] == 1;
            var rotate = actions.DiscreteActions[1] == 1;

            if (!placeControlLine) return;
            
            var holding = Instantiate(controlLine);
            controlLines.Add(holding);
            
            holding.transform.position = GetTerrainPoint();

            if (!rotate) return;

            holding.transform.Rotate(Vector3.up, rotateDegrees);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
            ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
            
            continuousActions[0] = Input.GetAxisRaw("Horizontal");
            continuousActions[1] = Input.GetAxisRaw("Vertical");

            discreteActions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            SetReward(-1f);
            EndEpisode();
        }
    }
}