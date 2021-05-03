#if UNITY_EDITOR
using Mapbox.Unity.Map;
using UnityEditor;
using UnityEngine;

namespace MapboxSDK.Mapbox.Unity.Editor.PropertyDrawers
{
	[CustomPropertyDrawer(typeof(LineGeometryOptions))]
	public class LineGeometryOptionsDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(property.FindPropertyRelative("Width"));
			property.FindPropertyRelative("JoinType").enumValueIndex = (int)((LineJoinType)EditorGUILayout.EnumPopup("Join Type", (LineJoinType)property.FindPropertyRelative("JoinType").intValue));
			if (property.FindPropertyRelative("JoinType").enumValueIndex == (int)LineJoinType.Miter || property.FindPropertyRelative("JoinType").enumValueIndex == (int)LineJoinType.Bevel)
			{
				EditorGUILayout.PropertyField(property.FindPropertyRelative("MiterLimit"));
			}
			else if (property.FindPropertyRelative("JoinType").enumValueIndex == (int)LineJoinType.Round)
			{
				EditorGUILayout.PropertyField(property.FindPropertyRelative("RoundLimit"));
			}

			property.FindPropertyRelative("CapType").enumValueIndex = (int)((LineCapType)EditorGUILayout.EnumPopup("Cap Type", (LineCapType)property.FindPropertyRelative("CapType").intValue));

			if (EditorGUI.EndChangeCheck())
			{
				EditorHelper.CheckForModifiedProperty(property);
			}
		}
	}
}
#endif