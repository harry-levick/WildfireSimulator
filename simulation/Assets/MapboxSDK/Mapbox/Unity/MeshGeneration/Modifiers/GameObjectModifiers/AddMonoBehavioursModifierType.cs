using System;
using UnityEngine;

namespace MapboxSDK.Mapbox.Unity.MeshGeneration.Modifiers.GameObjectModifiers
{
#if UNITY_EDITOR
	using UnityEditor;
#endif

	[Serializable]
	public class AddMonoBehavioursModifierType
	{
		[SerializeField]
		string _typeString;

		Type _type;

#if UNITY_EDITOR
		[SerializeField]
		MonoScript _script;
#endif

		public Type Type
		{
			get
			{
				if (_type == null)
				{
					_type = Type.GetType(_typeString);
				}
				return _type;
			}
		}
	}
}