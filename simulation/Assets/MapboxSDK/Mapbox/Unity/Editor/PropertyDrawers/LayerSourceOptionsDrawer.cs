#if UNITY_EDITOR
using Mapbox.Unity.Map;
using UnityEditor;
using UnityEngine;

namespace MapboxSDK.Mapbox.Unity.Editor.PropertyDrawers
{
	[CustomPropertyDrawer(typeof(LayerSourceOptions))]
	public class LayerSourceOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("layerSource"), label);
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return lineHeight;
		}
	}
}
#endif