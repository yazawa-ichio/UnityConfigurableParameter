using System;
using UnityEngine;

namespace UnityConfigurableParameter
{
	[Serializable]
	public sealed class ConfigurableParameter<T> : ISerializationCallbackReceiver where T : new()
	{
		[SerializeField]
		ConfigAsset m_Config;
		[SerializeField]
		T m_Value;
		[SerializeField]
		string[] m_Override;

		public T Resolve()
		{
			if (m_Config == null)
			{
				return m_Value;
			}
			var value = new T();
			m_Config.Resolve(this, ref value);
			SetParams(ref value, m_Value, m_Override);
			return value;
		}

		internal void SetParams(ref T value, T preset, string[] names)
		{
			foreach (var name in names)
			{
				if (ReflectionCache<T>.TryGetField(name, out var field))
				{
					var presetValue = field.GetValue(preset);
					field.SetValue(value, presetValue);
				}
			}
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			if (m_Config != null && m_Config.Value is not T)
			{
				Debug.Assert(false, "Config is not match type");
			}
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			if (m_Config != null && m_Config.Value is not T)
			{
				Debug.Assert(false, "Config is not match type");
			}
		}
	}
}