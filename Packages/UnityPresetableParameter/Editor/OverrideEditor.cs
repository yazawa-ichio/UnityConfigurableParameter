using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityConfigurableParameter
{
	[CustomPropertyDrawer(typeof(Override<>), true)]
	public class OverrideEditor : PropertyDrawer
	{
		List<OverrideAttribute.Entry> m_Entries = new();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = EditorGUIUtility.singleLineHeight;

			var value = property.FindPropertyRelative("m_Value");

			var foldoutRect = position;
			foldoutRect.width = EditorGUIUtility.labelWidth;
			using (new EditorGUI.IndentLevelScope(-1))
			{
				property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
			}
			var objectRect = position;
			objectRect.x += foldoutRect.width + 2;
			objectRect.width = position.width - foldoutRect.width - 2;
			EditorGUI.ObjectField(objectRect, value, GUIContent.none);

			if (!property.isExpanded)
			{
				return;
			}

			using (new EditorGUI.IndentLevelScope(1))
			{
				var param = property.FindPropertyRelative("m_Param").FindPropertyRelative("m_Params");
				m_Entries.Clear();
				SetEntryList(value.objectReferenceValue, m_Entries);
				if (CheckChanged(param, m_Entries))
				{
					RefreshList(param, m_Entries);
				}
				position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

				for (int i = 0; i < param.arraySize; i++)
				{
					var v = param.GetArrayElementAtIndex(i).FindPropertyRelative("m_Data");
					if (v.managedReferenceValue is OverrideParam.Entry.IValueHolder)
					{
						v = v.FindPropertyRelative("Value");
					}
					EditorGUI.PropertyField(position, v, m_Entries[i].DisplayName, true);
					position.y += EditorGUI.GetPropertyHeight(v);
				}

			}

		}

		void SetEntryList(object value, List<OverrideAttribute.Entry> entries)
		{
			if (value == null)
			{
				return;
			}
			foreach (var entry in OverrideAttribute.Get(value.GetType()))
			{
				if (entry.IsOverride)
				{
					var obj = entry.FieldInfo.GetValue(value) as IOverride;
					if (obj != null)
					{
						SetEntryList(obj.Source, entries);
					}
				}
				else if (!entries.Any(x => x.Name == entry.Name))
				{
					entries.Add(entry);
				}
			}
		}

		bool CheckChanged(SerializedProperty param, List<OverrideAttribute.Entry> entries)
		{
			if (param.arraySize != entries.Count)
			{
				return true;
			}
			for (int i = 0; i < param.arraySize; i++)
			{
				var name = param.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue;
				if (entries[i].FieldInfo.Name != name)
				{
					return true;
				}
			}
			return false;
		}

		void RefreshList(SerializedProperty param, List<OverrideAttribute.Entry> entries)
		{
			Dictionary<string, object> cache = new();
			for (int i = 0; i < param.arraySize; i++)
			{
				var p = param.GetArrayElementAtIndex(i);
				var n = p.FindPropertyRelative("m_Name");
				var v = p.FindPropertyRelative("m_Data");
				cache[n.stringValue] = v.managedReferenceValue;
			}
			param.ClearArray();
			foreach (var entry in entries)
			{
				param.InsertArrayElementAtIndex(param.arraySize);
				var e = param.GetArrayElementAtIndex(param.arraySize - 1);
				e.FindPropertyRelative("m_Name").stringValue = entry.FieldInfo.Name;
				var v = e.FindPropertyRelative("m_Data");
				if (cache.TryGetValue(entry.FieldInfo.Name, out var ret))
				{
					v.managedReferenceValue = ret;
				}
				else
				{
					v.managedReferenceValue = OverrideParam.Entry.CreateData(entry.FieldInfo.FieldType);
				}
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
			{
				return EditorGUIUtility.singleLineHeight;
			}
			var value = property.FindPropertyRelative("m_Value");

			float size = 0;
			var obj = value.objectReferenceValue;
			if (obj != null)
			{
				var param = property.FindPropertyRelative("m_Param").FindPropertyRelative("m_Params");
				for (int i = 0; i < param.arraySize; i++)
				{
					var p = param.GetArrayElementAtIndex(i);
					var v = p.FindPropertyRelative("m_Data");
					if (v.managedReferenceValue is OverrideParam.Entry.IValueHolder)
					{
						v = v.FindPropertyRelative("Value");
					}
					size += EditorGUI.GetPropertyHeight(v);
				}
			}

			return EditorGUIUtility.singleLineHeight * 1 + size;
		}
	}

}