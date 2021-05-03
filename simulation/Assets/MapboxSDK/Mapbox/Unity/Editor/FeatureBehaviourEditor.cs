#if UNITY_EDITOR
using Mapbox.Unity.MeshGeneration.Components;
using UnityEditor;
using UnityEngine;

namespace MapboxSDK.Mapbox.Unity.Editor
{
	[CustomEditor(typeof(FeatureBehaviour))]
	public class FeatureBehaviourEditor : UnityEditor.Editor
	{
		FeatureBehaviour _beh;

		public void OnEnable()
		{
			_beh = (FeatureBehaviour)target;
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			if (GUILayout.Button("Show Properties"))
			{
				_beh.ShowDebugData();
			}
		}
	}
}
#endif