#if UNITY_EDITOR
using Mapbox.Unity.Map;
using UnityEditor;
using UnityEngine;

namespace MapboxSDK.Mapbox.Unity.Editor.PropertyDrawers
{
	[CustomPropertyDrawer(typeof(LayerPerformanceOptions))]
	public class LayerPerformanceOptionsDrawer : PropertyDrawer
	{
		SerializedProperty isActiveProperty;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			isActiveProperty = property.FindPropertyRelative("isEnabled");

			isActiveProperty.boolValue = EditorGUILayout.Toggle(new GUIContent("Enable Coroutines"), isActiveProperty.boolValue);

			if (isActiveProperty.boolValue == true)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(property.FindPropertyRelative("entityPerCoroutine"), true);
				EditorGUI.indentLevel--;
			}
		}
	}
}
#endif