#if UNITY_EDITOR
using Mapbox.Unity.MeshGeneration.Factories;
using UnityEditor;

namespace MapboxSDK.Mapbox.Unity.Editor
{
	[CustomEditor(typeof(AbstractTileFactory))]
	public class FactoryEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
		}
	}
}
#endif