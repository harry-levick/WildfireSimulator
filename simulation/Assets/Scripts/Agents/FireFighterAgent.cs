using System;
using System.Runtime.CompilerServices;
using Fire;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Agents
{
    public class FireFighterAgent : Agent
    {
        public FireBehaviour fire;

        public override void OnEpisodeBegin()
        {
            const float max = 1600;
            const float min = -1600;

            var localPosition = new Vector3(Random.Range(min, max), 500, Random.Range(min, max));
            transform.localPosition = localPosition;

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
            
            transform.position += new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;
            
            Debug.Log(actions.DiscreteActions[0]);
            Debug.Log(actions.ContinuousActions[0]);
            Debug.Log(actions.ContinuousActions[1]);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
            continuousActions[0] = Input.GetAxisRaw("Horizontal");
            continuousActions[1] = Input.GetAxisRaw("Vertical");
        }

        private void OnTriggerEnter(Collider other)
        {
            SetReward(-1f);
            EndEpisode();
        }
    }
}