using System;
using UnityEngine;

namespace UnityConfigurableParameter
{
	public interface IOverride
	{
		ScriptableObject Source { get; }
		void Resolve(OverrideParam param);
	}

	[Serializable]
	public class Override<T> : IOverride where T : ScriptableObject
	{
		[SerializeField]
		T m_Value;
		[SerializeField]
		OverrideParam m_Param;

		[NonSerialized]
		T m_ResolvedValue;

		ScriptableObject IOverride.Source => m_Value;

		void IOverride.Resolve(OverrideParam param)
		{
			Resolve(param);
		}

		public T Resolve()
		{
			return Resolve(m_Param);
		}

		public T Resolve(OverrideParam param)
		{
			if (m_ResolvedValue != null)
			{
				return m_ResolvedValue;
			}
			if (m_Value == null)
			{
				return null;
			}
			m_ResolvedValue = ScriptableObject.Instantiate(m_Value);
			foreach (var entry in OverrideAttribute.Get(m_Value.GetType()))
			{
				if (entry.IsOverride)
				{
					var overrideValue = entry.FieldInfo.GetValue(m_ResolvedValue) as IOverride;
					overrideValue?.Resolve(param);
				}
				else
				{
					var name = entry.Name;
					var obj = param.Get(name, entry.FieldInfo.FieldType);
					if (obj != null)
					{
						entry.FieldInfo.SetValue(m_ResolvedValue, obj);
					}
				}
			}
			return m_ResolvedValue;
		}

		public static implicit operator T(Override<T> self)
		{
			return self.Resolve();
		}

	}

}