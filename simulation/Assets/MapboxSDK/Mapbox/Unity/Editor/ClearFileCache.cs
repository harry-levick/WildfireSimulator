#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MapboxSDK.Mapbox.Unity.Editor
{
	[InitializeOnLoad]
	public class ClearFileCache : MonoBehaviour
	{
		
		[MenuItem("Mapbox/Clear File Cache")]
		public static void ClearAllCachFiles()
		{
			global::Mapbox.Unity.MapboxAccess.Instance.ClearAllCacheFiles();
		}


	}
}
#endif
