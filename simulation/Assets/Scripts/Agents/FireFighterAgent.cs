using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;

namespace Agents
{
    public class FireFighterAgent : Agent
    {
        public override void OnActionReceived(ActionBuffers actions)
        {
            Debug.Log(actions.DiscreteActions[0]);
        }
    }
}