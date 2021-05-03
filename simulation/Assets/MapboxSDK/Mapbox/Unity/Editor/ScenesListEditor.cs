#if UNITY_EDITOR
using Mapbox.Unity.Utilities.DebugTools;
using MapboxSDK.Mapbox.Unity.Utilities.DebugTools;
using UnityEditor;
using UnityEngine;

namespace MapboxSDK.Mapbox.Unity.Editor
{
	[CustomEditor(typeof(ScenesList))]
	public class ScenesListEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			ScenesList e = target as ScenesList;

			if (GUILayout.Button("Link Listed Scenes"))
			{
				e.LinkScenes();
			}
		}
	}
}
#endif