using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityConfigurableParameter
{
	[CustomPropertyDrawer(typeof(ConfigurableParameter<>), true)]
	class ConfigurableParameterEditor : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
			{
				return EditorGUIUtility.singleLineHeight;
			}
			var value = property.FindPropertyRelative("m_Value");
			return EditorGUI.GetPropertyHeight(value, label) + EditorGUIUtility.singleLineHeight;
		}

		static List<string> s_Override = new();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = EditorGUIUtility.singleLineHeight;
			using (new EditorGUI.IndentLevelScope(-1))
			{
				property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
			}

			if (!property.isExpanded)
			{
				return;
			}

			using (new EditorGUI.IndentLevelScope(1))
			{
				position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

				var configProp = property.FindPropertyRelative("m_Config");
				var valueProp = property.FindPropertyRelative("m_Value");
				var overrideProp = property.FindPropertyRelative("m_Override");

				s_Override.Clear();
				for (var i = 0; i < overrideProp.arraySize; i++)
				{
					s_Override.Add(overrideProp.GetArrayElementAtIndex(i).stringValue);
				}

				var presetRect = new Rect(position.x, position.y, position.width - 80, EditorGUIUtility.singleLineHeight);

				using (new EditorGUI.DisabledGroupScope(true))
				{
					EditorGUI.ObjectField(presetRect, configProp);
				}

				if (GUI.Button(new Rect(presetRect.xMax + 1, presetRect.y, 38, presetRect.height), "SET"))
				{
					var type = fieldInfo.FieldType.GetGenericArguments()[0];
					ConfigAssetList.Select(type, (x) =>
					{
						configProp.serializedObject.Update();
						configProp.objectReferenceValue = x;
						configProp.serializedObject.ApplyModifiedProperties();
					});
				}

				if (GUI.Button(new Rect(presetRect.xMax + 1 + 40, presetRect.y, 38, presetRect.height), "NEW"))
				{
					Create(configProp);
				}

				position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				var valueRect = new Rect(position.x, position.y, position.width - 38, 0);
				var first = true;
				var depth = valueProp.depth;
				while (valueProp.NextVisible(first))
				{
					if (depth == valueProp.depth)
					{
						return;
					}
					first = false;
					bool enable = s_Override.Contains(valueProp.name);

					var height = EditorGUI.GetPropertyHeight(valueProp, false);
					valueRect.height = height;

					using (new EditorGUI.DisabledScope(!enable))
					{
						if (enable || configProp.objectReferenceValue == null)
						{
							EditorGUI.PropertyField(valueRect, valueProp, true);
						}
						else
						{
							var asset = configProp.objectReferenceValue as ConfigAsset;
							while (asset.Parent != null)
							{
								if (asset.Override.Contains(valueProp.name))
								{
									break;
								}
								asset = asset.Parent;
							}
							var so = new SerializedObject(asset);
							var prop = so.FindProperty("Value").FindPropertyRelative(valueProp.name);
							EditorGUI.PropertyField(valueRect, prop, true);
						}
					}
					var toggle = GUI.Toggle(new Rect(valueRect.xMax + 2, valueRect.y, 34, EditorGUIUtility.singleLineHeight), enable, "OR");
					if (toggle != enable)
					{
						enable = toggle;
						if (enable)
						{
							overrideProp.InsertArrayElementAtIndex(overrideProp.arraySize);
							overrideProp.GetArrayElementAtIndex(overrideProp.arraySize - 1).stringValue = valueProp.name;
						}
						else
						{
							overrideProp.DeleteArrayElementAtIndex(s_Override.IndexOf(valueProp.name));
						}
					}
					valueRect.y += height + EditorGUIUtility.standardVerticalSpacing;
				}
			}

			void Create(SerializedProperty preset)
			{
				var path = EditorUtility.SaveFilePanelInProject("Create Config", "New Config", "asset", "", "Assets");
				if (string.IsNullOrEmpty(path))
				{
					return;
				}
				var type = fieldInfo.FieldType.GetGenericArguments()[0];
				var asset = ScriptableObject.CreateInstance<ConfigAsset>();
				asset.Value = System.Activator.CreateInstance(type);
				AssetDatabase.CreateAsset(asset, path);
				var impoter = AssetImporter.GetAtPath(path);
				impoter.userData = type.FullName;
				impoter.SaveAndReimport();
				preset.objectReferenceValue = asset;
			}
		}

	}

}
