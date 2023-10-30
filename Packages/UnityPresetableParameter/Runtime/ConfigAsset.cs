using System.Collections.Generic;
using UnityEngine;

namespace UnityConfigurableParameter
{
	internal sealed class ConfigAsset : ScriptableObject
	{
		public ConfigAsset Parent;
		public string[] Override;

		[SerializeReference]
		public object Value;

		static Stack<ConfigAsset> s_PresetStask = new();

		internal void Resolve<T>(ConfigurableParameter<T> parameter, ref T value) where T : new()
		{
			s_PresetStask.Clear();

			var cur = this;
			while (cur != null)
			{
				s_PresetStask.Push(cur);
				cur = cur.Parent;
			}
			while (s_PresetStask.Count > 0)
			{
				var preset = s_PresetStask.Pop();
				if (preset.Parent == null)
				{
					parameter.SetParams(ref value, (T)preset.Value, ReflectionCache<T>.GetKeys());

				}
				else
				{
					parameter.SetParams(ref value, (T)preset.Value, preset.Override);
				}
			}
		}

	}
}