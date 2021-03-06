using System;
using System.Collections.Generic;
using Fire;
using Mapbox.Unity.Map;
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
        public FireBehaviour fire;                              // the fire that is being controlled
        public AbstractMap map;                                 // the map which the fire is burning on
        private GameObject _controlLinePrefab;                  // prefab of the control line
        private List<GameObject> _controlLines;                 // list of all control lines that we have placed this episode
        private const float VarianceFromCenter = 1600;          // the variance in transform position of the agent from the local center
        private const int HeightAboveTerrain = 500;             // the y coordinate of the agents local position
        private const float AgentSpeed = 400f;                  // speed of the agent

        public void Awake()
        {
            _controlLinePrefab = Resources.Load(ControlLinePrefab) as GameObject;
            _controlLines = new List<GameObject>();
        }

        public override void OnEpisodeBegin()
        {
            var posX = Random.Range(-VarianceFromCenter, VarianceFromCenter);
            var posZ = Random.Range(-VarianceFromCenter, VarianceFromCenter);
            
            transform.localPosition = new Vector3(posX, HeightAboveTerrain, posZ);

            _controlLines.ForEach(line => line.Destroy());
            _controlLines.Clear();
            
            fire.Reset();
            fire.Initialise(GetTerrainPoint());
        }
        
        private Vector3 GetTerrainPoint()
        {
            if (Physics.Raycast(transform.position, transform.up * -1, out var hitInfo, Mathf.Infinity))
            {
                return hitInfo.point;
            }

            SetReward(-1f);
            EndEpisode();
            throw new Exception();
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
            
            transform.position += new Vector3(moveX, 0, moveZ) * Time.deltaTime * AgentSpeed;

            var placeControlLine = actions.DiscreteActions[0] == 1;
            var rotate = actions.DiscreteActions[1] == 1;

            if (!placeControlLine) return;
            
            var holding = Instantiate(_controlLinePrefab);
            _controlLines.Add(holding);
                
            holding.transform.position = GetTerrainPoint();

            if (rotate)
            {
                const float rotateDegrees = 90f;
                holding.transform.Rotate(Vector3.up, rotateDegrees);
            }
                
            DropControlLine(holding);
        }
        
        private void DropControlLine(GameObject obj)
        {
            var bounds = obj.GetComponent<Renderer>().bounds;
            var height = transform.position.y;
            
            var minWorld = bounds.min;
            var maxWorld = bounds.max;

            minWorld.y = height;
            maxWorld.y = height;

            if (Physics.Raycast(minWorld, Vector3.down, out var minHit, Mathf.Infinity) &&
                Physics.Raycast(maxWorld, Vector3.down, out var maxHit, Mathf.Infinity))
            {
                var minPos = map.WorldToGeoPosition(minHit.point);
                var maxPos = map.WorldToGeoPosition(maxHit.point);

                fire.PutControlLine(minPos, maxPos);
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuousActions = actionsOut.ContinuousActions;
            var discreteActions = actionsOut.DiscreteActions;
            
            continuousActions[0] = Input.GetAxisRaw("Horizontal");
            continuousActions[1] = Input.GetAxisRaw("Vertical");

            discreteActions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
            discreteActions[1] = Input.GetKey(KeyCode.LeftShift) ? 1 : 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            SetReward(-1f);
            EndEpisode();
        }
    }
}