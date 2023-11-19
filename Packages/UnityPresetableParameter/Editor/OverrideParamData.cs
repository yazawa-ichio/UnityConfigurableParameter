using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityConfigurableParameter
{
	[Serializable]
	internal class OverrideParamData
	{
		[Serializable]
		public class ParamData
		{
			public long SourceLocalId;

			public List<Entry> Params = new();

			public object Get(string name, Type type)
			{
				foreach (var entry in Params)
				{
					if (entry.Name == name)
					{
						if (type.IsValueType && entry.Value == null)
						{
							continue;
						}
						if (entry.Value != null && !type.IsAssignableFrom(entry.Value.GetType()))
						{
							continue;
						}
						return entry.Value;
					}
				}
				return default;
			}

			public void Apply(UnityEngine.Object output)
			{
				if (output == null)
				{
					return;
				}
				var so = new SerializedObject(output);
				so.Update();
				var meta = OverrideMetaData.Get(output.GetType());
				foreach (var (name, field) in meta.OverrideParams)
				{
					var prop = so.FindProperty(name);
					if (prop == null)
					{
						continue;
					}
					var value = Get(name, field.FieldType);
					if (value == null)
					{
						Params.Add(Entry.Create(name, prop.boxedValue));
					}
					else
					{
						prop.boxedValue = value;
					}
				}
				so.ApplyModifiedProperties();
			}
		}

		public string Guid;

		public List<ParamData> AssetData = new();

		public void Apply(UnityEngine.Object source, UnityEngine.Object output)
		{
			if (source == null)
			{
				return;
			}
			if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string sourceGuid, out long sourceLocalId))
			{
				return;
			}
			var param = AssetData.Find(x => x.SourceLocalId == sourceLocalId);
			if (param == null)
			{
				param = new ParamData()
				{
					SourceLocalId = sourceLocalId,
				};
				AssetData.Add(param);
			}
			param.Apply(output);
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
				set
				{
					m_Data = CreateData(value);
					m_Value = null;
				}
			}

			public static Entry Create(string name, object value)
			{
				var entry = new Entry();
				entry.m_Name = name;
				entry.m_Data = CreateData(value);
				return entry;
			}

			static object CreateData(object value)
			{
				if (value == null)
				{
					return null;
				}
				Type type = value.GetType();
				if (type == typeof(sbyte))
				{
					return new SbyteHolder() { Value = (sbyte)value };
				}
				if (type == typeof(short))
				{
					return new ShortHolder() { Value = (short)value };
				}
				if (type == typeof(int))
				{
					return new IntHolder() { Value = (int)value };
				}
				if (type == typeof(long))
				{
					return new LongHolder() { Value = (long)value };
				}
				if (type == typeof(byte))
				{
					return new ByteHolder() { Value = (byte)value };
				}
				if (type == typeof(ushort))
				{
					return new UshortHolder() { Value = (ushort)value };
				}
				if (type == typeof(uint))
				{
					return new UIntHolder() { Value = (uint)value };
				}
				if (type == typeof(ulong))
				{
					return new ULongHolder() { Value = (ulong)value };
				}
				if (type == typeof(float))
				{
					return new FloatHolder() { Value = (float)value };
				}
				if (type == typeof(double))
				{
					return new DoubleHolder() { Value = (double)value };
				}
				if (type == typeof(bool))
				{
					return new BoolHolder() { Value = (bool)value };
				}
				if (type == typeof(char))
				{
					return new CharHolder() { Value = (char)value };
				}
				if (type == typeof(string))
				{
					return new StringHolder() { Value = (string)value };
				}
				if (typeof(UnityEngine.Object).IsAssignableFrom(type))
				{
					return new UnityObjectHolder() { Value = (UnityEngine.Object)value };
				}
				return value;
			}

		}

	}
}