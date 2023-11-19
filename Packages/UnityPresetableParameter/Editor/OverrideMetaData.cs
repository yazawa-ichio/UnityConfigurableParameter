using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityConfigurableParameter
{
	internal class OverrideMetaData
	{
		static Dictionary<Type, OverrideMetaData> s_Cache = new();

		public static OverrideMetaData Get(Type type)
		{
			if (!s_Cache.TryGetValue(type, out var cache))
			{
				cache = new OverrideMetaData();
				s_Cache[type] = cache;
				cache.Register(type);
			}
			return cache;
		}

		class SerializeTarget
		{
			public string Name;
			public Type Type;
			public OverrideParamAttribute Attribute;
			public FieldInfo Field;
			public OverrideMetaData Nest;

			public IEnumerable<(string, FieldInfo)> GetOverrideParam()
			{
				if (Nest != null)
				{
					foreach (var ret in Nest.OverrideParams)
					{
						yield return (Name + "." + ret.Key, ret.Value);
					}

				}
				if (Attribute != null)
				{
					yield return (Name, Field);
				}
			}

			public IEnumerable<(string, FieldInfo)> GetRefObjectParam()
			{
				if (Nest != null)
				{
					foreach (var ret in Nest.OverrideParams)
					{
						yield return (Name + "." + ret.Key, ret.Value);
					}
				}
				if (typeof(UnityEngine.Object).IsAssignableFrom(Field.FieldType))
				{
					yield return (Name, Field);
				}
			}

		}

		List<SerializeTarget> m_Targets = new();

		public Dictionary<string, FieldInfo> OverrideParams = new();

		public Dictionary<string, FieldInfo> UnityObjectRefs = new();

		private OverrideMetaData() { }

		void Register(Type type)
		{
			foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (f.IsPublic || f.IsDefined(typeof(SerializeField), true))
				{
					AddField(f);
				}
			}
			foreach (var target in m_Targets)
			{
				foreach (var (name, field) in target.GetOverrideParam())
				{
					OverrideParams[name] = field;
				}
			}
			foreach (var target in m_Targets)
			{
				foreach (var (name, field) in target.GetRefObjectParam())
				{
					UnityObjectRefs[name] = field;
				}
			}
		}

		void AddField(FieldInfo info)
		{
			SerializeTarget target = new()
			{
				Name = info.Name,
				Type = info.FieldType,
				Attribute = info.GetCustomAttribute<OverrideParamAttribute>(true),
				Field = info,
			};
			m_Targets.Add(target);
			if (target.Attribute != null)
			{
				return;
			}
			if (info.FieldType != typeof(UnityEngine.Object) && info.FieldType.IsDefined(typeof(SerializableAttribute), true))
			{
				target.Nest = Get(info.FieldType);
			}
		}

	}

}