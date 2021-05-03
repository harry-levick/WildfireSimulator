using UnityEngine;

namespace Agents
{
    public class EncircleSettings : MonoBehaviour
    {
        /// <summary>
        /// The 'moving speed' of the agents in the scene
        /// </summary>
        public float agentSpeed;
        
        /// <summary>
        /// The safe radius of the agent. Used for calculating the collision
        /// avoidance reward.
        /// </summary>
        public int agentSafeRadius;

        /// <summary>
        /// The safe radius of the target. Used for calculating the collision
        /// avoidance reward.
        /// </summary>
        public int targetSafeRadius;
        
        /// <summary>
        /// The spawn area margin multiplier.
        /// ex: .9 means 90% of the spawn area will be used.
        /// .1 margin will be left so that players don't spawn off the edge.
        /// The higher this value, the longer training time required.
        /// </summary>
        public float spawnAreaMarginMultiplier;

        /// <summary>
        /// When the goal is reached the ground will swap to this material.
        /// </summary>
        public Material goalReachedMaterial;

        /// <summary>
        /// When an agent fails, the ground will turn this material.
        /// </summary>
        public Material failMaterial;

        public int desiredEncirclementRadius;
        public float desiredEncirclementAngluarVelocity;
    }
}