#if UNITY_EDITOR
using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEditor;
using UnityEngine;

namespace MapboxSDK.Mapbox.Unity.Editor.PropertyDrawers
{
	[CustomPropertyDrawer(typeof(MaterialList))]
	public class MaterialListDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var matArray = property.FindPropertyRelative("Materials");
			if (matArray.arraySize == 0)
			{
				matArray.arraySize = 1;
			}
			EditorGUILayout.PropertyField(property.FindPropertyRelative("Materials").GetArrayElementAtIndex(0), label);
			EditorGUI.EndProperty();
		}
	}
}
#endif