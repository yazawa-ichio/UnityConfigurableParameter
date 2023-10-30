using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityConfigurableParameter
{
	static class ReflectionCache<T>
	{
		static readonly Dictionary<string, System.Reflection.FieldInfo> Fields = new();
		static readonly string[] s_Keys;

		static ReflectionCache()
		{
			var type = typeof(T);
			foreach (var field in type.GetFields())
			{
				if (field.IsPublic)
				{
					Fields[field.Name] = field;
				}
				else if (field.IsDefined(typeof(SerializeField), true))
				{
					Fields[field.Name] = field;
				}
			}
			s_Keys = Fields.Keys.ToArray();
		}

		public static string[] GetKeys()
		{
			return s_Keys;
		}

		public static bool TryGetField(string name, out System.Reflection.FieldInfo field)
		{
			return Fields.TryGetValue(name, out field);
		}

	}
}