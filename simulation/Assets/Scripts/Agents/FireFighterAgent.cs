using Fire;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Agents
{
    public class FireFighterAgent : Agent
    {
        public FireBehaviour fire;
        
        public override void OnActionReceived(ActionBuffers actions)
        {
            var moveX = actions.ContinuousActions[0];
            var moveZ = actions.ContinuousActions[1];
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(fire.weatherReport.current.wind_deg);
            sensor.AddObservation(fire.weatherReport.current.wind_speed);
            sensor.AddObservation(fire.ignitionPoint);
        }
    }
}