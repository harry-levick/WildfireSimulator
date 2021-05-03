using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fire;
using Unity.MLAgents;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Agents
{
    public class EncircleEnvController : MonoBehaviour
    {
        [Serializable]
        public class PlayerInfo
        {
            public EncircleCollabAgent agent;
            [HideInInspector] public Vector3 startingPos;
        }
        
        /// <summary>
        /// Max Academy steps before this platform resets.
        /// </summary>
        [Header("Max Environment Steps")] public int maxEnvironmentSteps;

        /// <summary>
        /// The area bounds.
        /// </summary>
        [HideInInspector] public Bounds areaBounds;

        /// <summary>
        /// The ground.
        /// </summary>
        public GameObject ground;

        /// <summary>
        /// We will be changing the ground material based on success/failure.
        /// </summary>
        private Renderer _mGroundRenderer;

        /// <summary>
        /// Cached on Awake()
        /// </summary>
        private Material _mGroundMaterial;

        /// <summary>
        /// List of Agents on the Platform
        /// </summary>
        public List<PlayerInfo> agentsList = new List<PlayerInfo>();
        private List<PlayerInfo> activeAgentsList = new List<PlayerInfo>();
        private int NumActiveAgents;
        /// <summary>
        /// The group containing the agents
        /// </summary>
        private SimpleMultiAgentGroup _mAgentGroup;

        /// <summary>
        /// The target which the agents are encircling.
        /// </summary>
        public GameObject target;

        public bool useRandomAgentPosition;
        public bool useRandomDesiredRadius;
        public bool useRandomDesiredAngularVelocity;
        public bool isCirclingFire;
        
        /// <summary>
        /// The height above 0 that agents are spawned
        /// </summary>
        private float _agentHeight;

        /// <summary>
        /// Settings defining the agents actions
        /// </summary>
        private EncircleSettings _mEncircleSettings;
        
        /// <summary>
        /// Timer counting the number of steps
        /// </summary>
        private int _mResetTimer;

        /// <summary>
        /// Used to record tensorboard stats
        /// </summary>
        private StatsRecorder _statsRecorder;

        private void Start()
        {
            areaBounds = ground.GetComponent<Collider>().bounds;
            _mGroundRenderer = ground.GetComponent<Renderer>();
            _mEncircleSettings = FindObjectOfType<EncircleSettings>();
            _mGroundMaterial = _mGroundRenderer.material;
            _agentHeight = isCirclingFire ? 600f : 5f;
            NumActiveAgents = agentsList.Count;

            _statsRecorder = Academy.Instance.StatsRecorder;
            
            // Initialize the TeamManager
            _mAgentGroup = new SimpleMultiAgentGroup();
            
            foreach (var item in agentsList)
            {
                var agentTransform = item.agent.transform;
                item.agent.m_targetCenterPos = target.transform.position;
                item.startingPos = agentTransform.position;

                var pos = useRandomAgentPosition ? GetRandomSpawnPos() : item.startingPos;
                item.agent.transform.position = pos;
                
                item.agent.m_EncircleSettings = _mEncircleSettings;
                item.agent.SafeRadius = _mEncircleSettings.agentSafeRadius;
                item.agent.TargetSafeRadius = _mEncircleSettings.targetSafeRadius;

                item.agent.minVectorPos = areaBounds.min;
                item.agent.maxVectorPos = areaBounds.max;
                item.agent.maxReward = 1.0f;
                item.agent.maxDistToTarget = MaxDistanceToTarget();
                item.agent.maxPhaseDifference = 180f;
                item.agent.m_NumAgents = NumActiveAgents;
                
                _mAgentGroup.RegisterAgent(item.agent);
            }

            ResetScene();
        }

        private Vector3 GetClosestFirePerimeterPos(Vector3 agentPos)
        {
            return target.GetComponent<FireBehaviour>()
                            .GetClosestPerimeterNode(agentPos);
        }

        private float MaxDistanceToTarget()
        {
            var span = areaBounds.max - areaBounds.min;
            var boundsLength = span.x;
            var boundsWidth = span.z;

            return Mathf.Sqrt(Mathf.Pow(boundsLength, 2f) + Mathf.Pow(boundsWidth, 2f));
        }

        /// <summary>
        /// Sort the list of agents by bearing around the target so that
        /// we can get the correct adjacent neighbours for each agent.
        /// </summary>
        /// <returns></returns>
        private void AgentsSortedByBearing()
        {
            agentsList = agentsList.OrderBy(item => GetBearing(item.agent.transform.position)).ToList();
        }

        private void SetAgentNeigbours(PlayerInfo item)
        {
            var itemPos = agentsList.IndexOf(item);
            var maxPos = agentsList.Count - 1;
                
            item.agent.m_counterClockwiseNeighbour = itemPos > 0
                ? agentsList[itemPos - 1].agent
                : agentsList[maxPos].agent;
                
            item.agent.m_clockwiseNeighbour = itemPos < maxPos
                ? agentsList[itemPos + 1].agent
                : agentsList[0].agent;
        }

        private float GetBearing(Vector3 agentPos)
        {
            var targetPos = target.transform.position;
            var north = targetPos + Vector3.forward;
            var isRight = IsClockwise(north, agentPos);

            var angle = Vector3.Angle(agentPos - targetPos, north - targetPos);
            
            return isRight ? angle : 360f - angle;
        }

        private void Update()
        {
            _mResetTimer += 1;
            if (_mResetTimer >= maxEnvironmentSteps && maxEnvironmentSteps > 0)
            {
                _mAgentGroup.GroupEpisodeInterrupted();
                StartCoroutine(GoalScoresSwapGroundMaterial(_mEncircleSettings.failMaterial, 0.5f));
                ResetScene();
            }
            
            // Hurry up penalty.
            _mAgentGroup.AddGroupReward(-1.0f / maxEnvironmentSteps);

            var groupWon = true;
            AgentsSortedByBearing();

            var meanRadiusError = 0.0f;
            var meanPhaseError = 0.0f;
            var meanAngularVelocityError = 0.0f;
            var meanNumCollisions = 0;
            
            foreach (var item in agentsList)
            {
                var numCollisions = item.agent.m_CollisionCounter;
                SetAgentNeigbours(item);
                var targetCenterPos = target.transform.position;
                item.agent.m_targetCenterPos = targetCenterPos;

                if (isCirclingFire)
                {
                    var desiredRadius = Vector3.Distance(target.transform.position,
                        target.GetComponent<FireBehaviour>().GetFurthestPerimeterNode(target.transform.position));

                    item.agent.desiredEncirclementRadius = desiredRadius;
                }
                
                var targetPos = isCirclingFire 
                    ? GetClosestFirePerimeterPos(item.agent.transform.position)
                    : targetCenterPos;

                item.agent.m_targetPerimPos = targetPos;
                
                var radiusError = item.agent.EncirclementRadiusError(targetPos);
                var phaseError = item.agent.PhaseError();
                var angularVelocityError = item.agent.AngularVelocityError(radiusError);
                
                meanRadiusError += radiusError;
                meanPhaseError += phaseError;
                meanAngularVelocityError += angularVelocityError;
                                
                var agentReward = item.agent.GetReward(radiusError, phaseError, angularVelocityError);
                item.agent.AddReward(agentReward);
                
                // Hurry up penalty
                item.agent.AddReward(-0.5f / maxEnvironmentSteps);
                
                if (radiusError > 0.05f || phaseError > 0.1f || angularVelocityError > 0.005f)
                {
                    groupWon = false;
                }

                if (item.agent.m_CollisionCounter > numCollisions) meanNumCollisions += 1;
            }
            
            _statsRecorder.Add("radius error", meanRadiusError / NumActiveAgents);
            _statsRecorder.Add("angular difference error", meanPhaseError / NumActiveAgents);
            _statsRecorder.Add("angular velocity error", meanAngularVelocityError / NumActiveAgents);
            _statsRecorder.Add("num collisions", meanNumCollisions / NumActiveAgents);
            
            if (groupWon) ReachedGoal(1.0f);
        }
        
        public Vector3 GetRandomSpawnPos()
        {
            var foundNewSpawnLocation = false;
            var randomSpawnPos = Vector3.zero;
            while (!foundNewSpawnLocation)
            {
                var randomPosX = Random.Range(-areaBounds.extents.x * _mEncircleSettings.spawnAreaMarginMultiplier, 
                    areaBounds.extents.x * _mEncircleSettings.spawnAreaMarginMultiplier);
                var randomPosZ = Random.Range(-areaBounds.extents.z * _mEncircleSettings.spawnAreaMarginMultiplier, 
                    areaBounds.extents.z * _mEncircleSettings.spawnAreaMarginMultiplier);

                randomSpawnPos = ground.transform.position + new Vector3(randomPosX, _agentHeight, randomPosZ);

                if (!Physics.CheckBox(randomSpawnPos, new Vector3(1.5f, 0.01f, 1.5f)))
                {
                    foundNewSpawnLocation = true;
                }
            }

            return randomSpawnPos;
        }

        private float GetRandomDesiredRadius(Vector3 targetPos)
        {
            var maxBounds = areaBounds.max;
            var minBounds = areaBounds.min;

            var minXDifference = 
                Mathf.Min(Mathf.Abs(maxBounds.x - targetPos.x), Mathf.Abs(minBounds.x - targetPos.x));
            var minZDifference = 
                Mathf.Min(Mathf.Abs(maxBounds.z - targetPos.z), Mathf.Abs(minBounds.z - targetPos.z));

            var maxRadius = Mathf.Min(minXDifference, minZDifference);
            var minRadius = _mEncircleSettings.targetSafeRadius + _mEncircleSettings.agentSafeRadius;
            var radius = Random.Range(minRadius, maxRadius);

            return radius;
        }

        private float GetRandomDesiredAngularVelocity()
        {
            return Random.Range(0f, 20f);
        }
        
        /// <summary>
        /// Determine whether the agent is moving around the target in a clockwise or anti-clockwise direction.
        /// </summary>
        /// <param name="prevPos"></param>
        /// <param name="currentPos"></param>
        /// <returns></returns>
        public bool IsClockwise(Vector3 prevPos, Vector3 currentPos)
        {
            var targetPos = target.transform.position;
            return ((prevPos.x - targetPos.x) * (currentPos.z - targetPos.z) 
                    - (prevPos.z - targetPos.z) * (currentPos.x - targetPos.x)) <= 0;
        }

        /// <summary>
        /// Called when the fire is encircled to a certain degree.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="score"></param>
        public void ReachedGoal(float score)
        {
            // Give the agent rewards
            _mAgentGroup.AddGroupReward(score);
            
            // Swap ground material to indicate we scored
            StartCoroutine(GoalScoresSwapGroundMaterial(_mEncircleSettings.goalReachedMaterial, 0.5f));
            _mAgentGroup.EndGroupEpisode();
            ResetScene();
        }

        IEnumerator GoalScoresSwapGroundMaterial(Material mat, float time)
        {
            _mGroundRenderer.material = mat;
            yield return new WaitForSeconds(time);
            _mGroundRenderer.material = _mGroundMaterial;
        }

        public void Failed(float score)
        {
            // Give the agent rewards
            _mAgentGroup.AddGroupReward(score);
            
            // Swap the ground material to indicate we failed
            StartCoroutine(GoalScoresSwapGroundMaterial(_mEncircleSettings.failMaterial, 0.5f));
            _mAgentGroup.EndGroupEpisode();
            ResetScene();
        }
        
        public void ResetScene()
        {
            _mResetTimer = 0;
            //activeAgentsList.Clear();
            
            // Reset target
            var targetPos = useRandomAgentPosition ? GetRandomSpawnPos() : target.transform.position;
            target.transform.position = targetPos;
            
            var desiredRadius = useRandomDesiredRadius 
                ? GetRandomDesiredRadius(targetPos)
                : _mEncircleSettings.desiredEncirclementRadius;

            var desiredAngularVelocity = useRandomDesiredAngularVelocity
                ? GetRandomDesiredAngularVelocity()
                : _mEncircleSettings.desiredEncirclementAngluarVelocity;

            //var toRemove = NumAgentsToDeactivate();
            //NumActiveAgents = agentsList.Count - toRemove;

            // Reset agents.
            foreach (var item in agentsList)
            {
                item.agent.gameObject.SetActive(true);
                var pos = useRandomAgentPosition ? GetRandomSpawnPos() : item.startingPos;
                    
                item.agent.transform.position = pos;
                item.agent.m_CollisionCounter = 0;
                item.agent.desiredEncirclementRadius = desiredRadius;
                item.agent.desiredAngularVelocity = desiredAngularVelocity;
                /*
                if (toRemove > 0)
                {
                    item.agent.gameObject.SetActive(false);
                    toRemove -= 1;
                }
                else
                {
                    activeAgentsList.Add(item);
                    
                    item.agent.gameObject.SetActive(true);
                    var pos = useRandomAgentPosition ? GetRandomSpawnPos() : item.startingPos;
                    
                    item.agent.transform.position = pos;
                    item.agent.m_CollisionCounter = 0;
                    item.agent.desiredEncirclementRadius = desiredRadius;
                    item.agent.desiredAngularVelocity = desiredAngularVelocity;
                    item.agent.m_NumAgents = NumActiveAgents;
                }
                 */
            }
            
            if (isCirclingFire) StartFire();
        }

        private void StartFire()
        {
            var fire = target.GetComponent<FireBehaviour>();
            
            fire.Reset();
            fire.Initialise(GetTerrainPoint(target.transform.position));
        }

        private Vector3 GetTerrainPoint(Vector3 point)
        {
            if (Physics.Raycast(point, Vector3.down, out var hitInfo, Mathf.Infinity))
            {
                return hitInfo.point;
            }
            throw new Exception("ERROR: Not Above Terrain");
        }

        private int NumAgentsToDeactivate()
        {
            var maxAgents = agentsList.Count;
            const int minAgents = 3;
            var maxAgentsToDeactivate = maxAgents - minAgents;

            return Random.Range(0, maxAgentsToDeactivate + 1);
        }
    }
}