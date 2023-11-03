using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityConfigurableParameter
{
	[Serializable]
	public class OverrideParam
	{
		[SerializeField]
		List<Entry> m_Params = new();

		public object Get(string name, Type type)
		{
			foreach (var entry in m_Params)
			{
				if (entry.Name == name)
				{
					return entry.Value;
				}
			}
			return default;
		}


		[Serializable]
		internal struct Entry
		{
			internal interface IValueHolder
			{
				object Value { get; }
			}

			class ValueHolder<T> : IValueHolder
			{
				public T Value;
				object IValueHolder.Value { get { return Value; } }
			}

			class SbyteHolder : ValueHolder<sbyte> { }
			class ShortHolder : ValueHolder<short> { }
			class IntHolder : ValueHolder<int> { }
			class LongHolder : ValueHolder<long> { }

			class ByteHolder : ValueHolder<byte> { }
			class UshortHolder : ValueHolder<ushort> { }
			class UIntHolder : ValueHolder<uint> { }
			class ULongHolder : ValueHolder<ulong> { }

			class FloatHolder : ValueHolder<float> { }
			class DoubleHolder : ValueHolder<double> { }
			class BoolHolder : ValueHolder<bool> { }
			class CharHolder : ValueHolder<char> { }
			class StringHolder : ValueHolder<string> { }
			class UnityObjectHolder : ValueHolder<UnityEngine.Object> { }

			[SerializeField]
			string m_Name;

			public string Name => m_Name;

			[SerializeReference]
			private object m_Data;

			private object m_Value;

			public object Value
			{
				get
				{
					if (m_Value == null && m_Data != null)
					{
						if (m_Data is IValueHolder holder)
						{
							m_Value = holder.Value;
						}
						else
						{
							m_Value = m_Data;
						}
					}
					return m_Value;
				}
			}

			public static object CreateData(Type type)
			{
				if (type == typeof(sbyte))
				{
					return new SbyteHolder();
				}
				if (type == typeof(short))
				{
					return new ShortHolder();
				}
				if (type == typeof(int))
				{
					return new IntHolder();
				}
				if (type == typeof(long))
				{
					return new LongHolder();
				}
				if (type == typeof(byte))
				{
					return new ByteHolder();
				}
				if (type == typeof(ushort))
				{
					return new UshortHolder();
				}
				if (type == typeof(uint))
				{
					return new UIntHolder();
				}
				if (type == typeof(ulong))
				{
					return new ULongHolder();
				}
				if (type == typeof(float))
				{
					return new FloatHolder();
				}
				if (type == typeof(double))
				{
					return new DoubleHolder();
				}
				if (type == typeof(bool))
				{
					return new BoolHolder();
				}
				if (type == typeof(char))
				{
					return new CharHolder();
				}
				if (type == typeof(string))
				{
					return new StringHolder();
				}
				if (typeof(UnityEngine.Object).IsAssignableFrom(type))
				{
					return new UnityObjectHolder();
				}
				return Activator.CreateInstance(type);
			}

		}

	}

}