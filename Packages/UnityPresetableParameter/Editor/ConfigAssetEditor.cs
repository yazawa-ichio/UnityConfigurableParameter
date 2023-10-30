using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityConfigurableParameter
{
	[CustomEditor(typeof(ConfigAsset), true)]
	class ConfigAssetEditor : Editor
	{
		static List<string> s_Override = new();

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			var parentProp = serializedObject.FindProperty(nameof(ConfigAsset.Parent));
			var valueProp = serializedObject.FindProperty(nameof(ConfigAsset.Value));
			var overrideProp = serializedObject.FindProperty(nameof(ConfigAsset.Override));

			s_Override.Clear();
			for (var i = 0; i < overrideProp.arraySize; i++)
			{
				s_Override.Add(overrideProp.GetArrayElementAtIndex(i).stringValue);
			}
			using (new GUILayout.HorizontalScope())
			{
				using (new EditorGUI.DisabledGroupScope(true))
				{
					EditorGUILayout.PropertyField(parentProp);
				}
				if (GUILayout.Button("SET", GUILayout.Width(40)))
				{
					var type = valueProp.managedReferenceValue.GetType();
					ConfigAssetList.Select(type, (x) =>
					{
						parentProp.serializedObject.Update();
						parentProp.objectReferenceValue = x;
						parentProp.serializedObject.ApplyModifiedProperties();
					});
				}
				if (GUILayout.Button("NEW", GUILayout.Width(40)))
				{
					var type = valueProp.managedReferenceValue.GetType();
					Create(parentProp, type);
				}
			}

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

				using (new GUILayout.HorizontalScope())
				{
					using (new EditorGUI.DisabledScope(!enable && parentProp.objectReferenceValue != null))
					{
						if (enable || parentProp.objectReferenceValue == null)
						{
							EditorGUILayout.PropertyField(valueProp, true);
						}
						else
						{
							var asset = parentProp.objectReferenceValue as ConfigAsset;
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
							EditorGUILayout.PropertyField(prop, true);
						}
					}
					var toggle = GUILayout.Toggle(enable, "OR", GUILayout.Width(34));
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
				}
			}

			serializedObject.ApplyModifiedProperties();
		}


		void Create(SerializedProperty preset, Type type)
		{
			var path = EditorUtility.SaveFilePanelInProject("Create Config", "New Config", "asset", "", "Assets");
			if (string.IsNullOrEmpty(path))
			{
				return;
			}
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
