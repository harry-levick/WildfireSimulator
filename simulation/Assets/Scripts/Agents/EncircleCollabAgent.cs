using System;
using Fire;
using Mapbox.Unity.Map;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using static Constants.StringConstants;

namespace Agents
{
    public class EncircleCollabAgent : Agent
    {
        public AbstractMap map;
        public FireBehaviour fire;
        
        public EncircleSettings m_EncircleSettings;
        public EncircleEnvController m_EncircleEnvController;
        public float angularVelocity;
        
        private float radiusCoeff = 0.5f;
        private float angleCoeff = 0.5f;
        private float velCoeff = 0f;
        
        public float maxReward;
        public float maxDistToTarget;
        public float maxPhaseDifference;
        public Vector3 maxVectorPos;
        public Vector3 minVectorPos;

        public int m_CollisionCounter;
        public int m_NumAgents;
        public int SafeRadius;
        public int TargetSafeRadius;
        public float desiredEncirclementRadius;
        public float desiredAngularVelocity;

        public EncircleCollabAgent m_clockwiseNeighbour;
        public EncircleCollabAgent m_counterClockwiseNeighbour;

        public Vector3 m_targetCenterPos;
        public Vector3 m_targetPerimPos;
        
        public override void CollectObservations(VectorSensor sensor)
        {
            var agentPos = NormaliseVector(transform.position, maxVectorPos, minVectorPos);
            var targetPos = m_EncircleEnvController.isCirclingFire 
                    ? NormaliseVector(m_targetCenterPos, maxVectorPos, minVectorPos)
                    : NormaliseVector(m_targetCenterPos, maxVectorPos, minVectorPos);
            
            var clockwiseNeighbourPos =
                NormaliseVector(m_clockwiseNeighbour.transform.position, maxVectorPos, minVectorPos);
            var counterClockwiseNeighbourPos = NormaliseVector(m_counterClockwiseNeighbour.transform.position,
                maxVectorPos, minVectorPos);
                
            sensor.AddObservation(agentPos);
            sensor.AddObservation(clockwiseNeighbourPos - agentPos);
            sensor.AddObservation(counterClockwiseNeighbourPos - agentPos);
            sensor.AddObservation(targetPos - agentPos);

            // Normalise the desired radius
            var maxRadius = (maxVectorPos.x - minVectorPos.x) / 2.0f;
            var minRadius = SafeRadius + TargetSafeRadius;
            sensor.AddObservation(NormaliseFloat(desiredEncirclementRadius, maxRadius, minRadius));
            sensor.AddObservation(desiredAngularVelocity);
        }

        public float GetReward(float radiusErr, float phaseErr, float angVelErr)
        {
            var radiusReward = EncirclementRadiusReward(radiusErr);
            var phaseReward = AngularDifferenceReward(phaseErr);
            var angularVelReward = AngularVelocityReward(phaseErr, angVelErr);
            
            return ((radiusCoeff * radiusReward) +
                    (angleCoeff * phaseReward) +
                    (velCoeff * angularVelReward)) 
                   / m_EncircleEnvController.maxEnvironmentSteps;
        }

        /// <summary>
        /// Moves the agent according to the selected action.
        /// </summary>
        /// <param name="act"></param>
        public void MoveAgent(ActionSegment<int> act)
        {
            var dirToGo = Vector3.zero;

            dirToGo += act[0] == 1 ? Vector3.forward : Vector3.zero;
            dirToGo += act[1] == 1 ? Vector3.forward * -1f : Vector3.zero;
            dirToGo += act[2] == 1 ? Vector3.right : Vector3.zero;
            dirToGo += act[3] == 1 ? Vector3.right * -1f : Vector3.zero;
            
            var agentTransform = transform;
            
            var currentPos = agentTransform.position;
            var prevPos = currentPos;
            
            currentPos += dirToGo * m_EncircleSettings.agentSpeed;
            agentTransform.position = currentPos;

            angularVelocity = CalcAngularVelocity(prevPos, currentPos);
        }

        private float CalcAngularVelocity(Vector3 prevPos, Vector3 currentPos)
        {
            var deltaAngle = Vector3.Angle(prevPos - m_targetCenterPos, currentPos - m_targetCenterPos);
            deltaAngle = m_EncircleEnvController.IsClockwise(prevPos, currentPos) ? deltaAngle : -deltaAngle;
            
            return deltaAngle / Time.deltaTime;
        }

        /// <summary>
        /// Called every step of the engine. Here the agent takes an action.
        /// </summary>
        /// <param name="actions"></param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            MoveAgent(actions.DiscreteActions);
        }
        
        private float EncirclementRadiusReward(float radiusError)
        {
            return Mathf.Pow(-1f * radiusError + maxReward, 3f);
        }

        public float EncirclementRadiusError(Vector3 targetPos)
        {
            var radius = Vector3.Distance(transform.position, targetPos);
            var radiusErr = Mathf.Abs(radius - desiredEncirclementRadius);
            
            // normalise radiusError
            var maxErr = maxDistToTarget - desiredEncirclementRadius; // |max distance from target - desired distance|
            const float minErr = 0f; // |min distance from target - desired distance|

            return NormaliseFloat(radiusErr, maxErr, minErr);
        }
        
        private float AngularDifferenceReward(float phaseError)
        {
            return Mathf.Pow(-1f * phaseError + maxReward, 3f);
        }

        public float PhaseError()
        {
            var pos = transform.position;
            
            var angleClockwise = 
                AngleBetween(pos, m_clockwiseNeighbour.transform.position, true);

            var angleCounterClockwise =
                AngleBetween(pos, m_counterClockwiseNeighbour.transform.position, false);
            
            var averageAngle = (angleClockwise + angleCounterClockwise) / 2f;
            
            var desiredPhaseDifference = m_NumAgents > 1 ? 360f / m_NumAgents : 0f;

            return NormaliseFloat(Mathf.Abs(averageAngle - desiredPhaseDifference), 
                maxPhaseDifference, 0);
        }

        private float AngularVelocityReward(float phaseError, float angularVelocityError)
        {
            return Mathf.Pow(-1f * angularVelocityError + maxReward, 3f);
        }

        public float GetMaxAngularVelocity()
        {
            return 360f;
        }
        
        public float AngularVelocityError(float radiusError)
        {
            //if (radiusError > 0.1f) return 1f;
            
            var maxAngularVelocity = GetMaxAngularVelocity();
            var error = Mathf.Abs(angularVelocity - desiredAngularVelocity);

            var maxError = Mathf.Max(Mathf.Abs(maxAngularVelocity - desiredAngularVelocity),
                                        Math.Abs(-maxAngularVelocity - desiredAngularVelocity));
            return NormaliseFloat(
                error,
                Mathf.Max(maxError, error),
                0f);
        }
        
        /// <summary>
        /// Normalise a floating point value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="max"></param>
        /// <param name="min"></param>
        /// <returns></returns>
        private float NormaliseFloat(float value, float max, float min)
        {
            return (value - min) / (max - min);
        }

        private Vector3 NormaliseVector(Vector3 vec, Vector3 maxVec, Vector3 minVec)
        {
            vec.x = NormaliseFloat(vec.x, maxVec.x, minVec.x);
            vec.y = 0f;
            vec.z = NormaliseFloat(vec.z, maxVec.z, minVec.z);
            
            return vec;
        }
        
        private float AngleBetween(Vector3 a, Vector3 b, bool isClockwise){
            // angle in [0,180]
            a -= m_targetCenterPos;
            b -= m_targetCenterPos;
            var angle = Vector3.Angle(a, b);
            var clockwise = Mathf.Sign(Vector3.Dot(Vector3.up,Vector3.Cross(a,b))) >= 0f;

            if (clockwise == isClockwise) return angle;
            return 360f - angle;
        }
        
        public void OnTriggerEnter(Collider other)
        {
            Die();
        }

        private void Die()
        {
            m_EncircleEnvController.Failed(-1f);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discreteActionsOut = actionsOut.DiscreteActions;
            discreteActionsOut[0] = 0;
            discreteActionsOut[1] = 0;
            discreteActionsOut[2] = 0;
            discreteActionsOut[3] = 0;

            if (Input.GetKey(KeyCode.UpArrow)) discreteActionsOut[0] = 1;
            if (Input.GetKey(KeyCode.DownArrow)) discreteActionsOut[1] = 1;
            if (Input.GetKey(KeyCode.RightArrow)) discreteActionsOut[2] = 1;
            if (Input.GetKey(KeyCode.LeftArrow)) discreteActionsOut[3] = 1;
        }

        private void DropControlLine(GameObject obj)
        {
            var bounds = obj.GetComponent<Renderer>().bounds;
            var height = transform.position.y;

            var minWorld = bounds.min;
            var maxWorld = bounds.max;

            minWorld.y = transform.position.y;
            maxWorld.y = transform.position.y;

            if (Physics.Raycast(minWorld, Vector3.down, out var minHit, Mathf.Infinity) &&
                Physics.Raycast(maxWorld, Vector3.down, out var maxHit, Mathf.Infinity))
            {
                var minPos = map.WorldToGeoPosition(minHit.point);
                var maxPos = map.WorldToGeoPosition(maxHit.point);
                
                fire.PutControlLine(minPos, maxPos);
            }
        }
        /*
        private void MakeControlLine()
        {
            var controlLinePrefab = Resources.Load(ControlLinePrefab) as GameObject;
            var holding = Instantiate(controlLinePrefab);

            _controlLines.Add(holding);

            holding.transform.position = GetTerrainPoint();
        }
         */
        
    }
}