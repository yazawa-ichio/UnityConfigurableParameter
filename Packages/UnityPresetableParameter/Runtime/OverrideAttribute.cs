using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityConfigurableParameter
{
	[AttributeUsage(AttributeTargets.Field)]
	public class NestOverrideAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Field)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class OverrideDispName : Attribute
	{
		public string Name { get; private set; }

		public OverrideDispName(string name)
		{
			Name = name;
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class OverrideAttribute : Attribute
	{
		public string SerializeName { get; set; }

		internal class Entry
		{
			public bool IsOverride;
			public FieldInfo FieldInfo;
			public string SerializeName;
			public GUIContent DisplayName;

			public string Name => SerializeName ?? FieldInfo.Name;
		}

		static Dictionary<Type, Entry[]> m_Entries = new();

		internal static Entry[] Get(Type type)
		{
			if (!m_Entries.TryGetValue(type, out var ret))
			{
				m_Entries[type] = ret = GetEntries(type).ToArray();
			}
			return ret;
		}

		static IEnumerable<Entry> GetEntries(Type type)
		{
			if (type == null)
			{
				yield break;
			}

			if (type == typeof(ScriptableObject))
			{
				yield break;
			}

			foreach (var entry in GetEntries(type.BaseType))
			{
				yield return entry;
			}

			foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (field.IsDefined(typeof(OverrideAttribute), false))
				{
					yield return new Entry
					{
						FieldInfo = field,
						SerializeName = field.GetCustomAttribute<OverrideAttribute>()?.SerializeName,
						DisplayName = new GUIContent(field.GetCustomAttribute<OverrideDispName>()?.Name ?? field.Name),
					};
				}
				if (typeof(IOverride).IsAssignableFrom(field.FieldType) && field.IsDefined(typeof(NestOverrideAttribute), false))
				{
					yield return new Entry
					{
						IsOverride = true,
						FieldInfo = field,
					};
				}
			}
		}

	}

}