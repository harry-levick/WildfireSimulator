#if UNITY_EDITOR
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEditor;
using UnityEngine;

namespace MapboxSDK.Mapbox.Unity.Editor
{
	[CustomEditor(typeof(LayerModifier))]
	public class LayerModifierEditor : UnityEditor.Editor
	{
		public SerializedProperty layerId_Prop;
		private MonoScript script;

		void OnEnable()
		{
			layerId_Prop = serializedObject.FindProperty("_layerId");

			script = MonoScript.FromScriptableObject((LayerModifier)target);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			GUI.enabled = false;
			script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
			GUI.enabled = true;

			layerId_Prop.intValue = EditorGUILayout.LayerField("Layer", layerId_Prop.intValue);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif