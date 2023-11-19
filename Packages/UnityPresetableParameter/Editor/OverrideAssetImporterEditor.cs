using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityConfigurableParameter
{

	[CustomEditor(typeof(OverrideAssetImporter))]
	public class OverrideAssetImporterEditor : ScriptedImporterEditor
	{

		class AssetPropertyData
		{
			public Object Source;
			public SerializedObject SerializedObject;
			public Dictionary<string, SerializedProperty> Property = new();
		}

		[NonSerialized]
		string m_Path;
		[NonSerialized]
		OverrideParamData m_Data;
		[NonSerialized]
		Dictionary<long, AssetPropertyData> m_AssetDatas = new();

		public override void OnInspectorGUI()
		{
			var importer = (target as OverrideAssetImporter);
			if (m_Path != importer.assetPath)
			{
				m_Path = importer.assetPath;
				Init(importer);
			}

			bool first = true;
			foreach (var data in m_AssetDatas.Values)
			{
				if (!first)
				{
					EditorGUILayout.Space();
				}
				first = false;
				using (new EditorGUI.DisabledGroupScope(true))
				{
					EditorGUILayout.ObjectField(data.Source, typeof(Object), false);
				}
				using (new EditorGUI.IndentLevelScope())
				{
					Stack<string> nestPath = new();
					foreach (var property in data.Property.Values)
					{
						var path = property.propertyPath;
						path = CutChild(path, null);
						while (nestPath.Count > 0 && path != nestPath.Peek())
						{
							nestPath.Pop();
							EditorGUI.indentLevel--;
						}
						while (!string.IsNullOrEmpty(path))
						{
							if (nestPath.Count > 0 && nestPath.Peek() == path)
							{
								break;
							}
							EditorGUILayout.LabelField(data.SerializedObject.FindProperty(path).displayName + ".");
							EditorGUI.indentLevel++;
							path = CutChild(path, nestPath);
						}
						EditorGUILayout.PropertyField(property, true);

						static string CutChild(string path, Stack<string> data)
						{
							data?.Push(path);
							var index = path.LastIndexOf(".");
							if (index < 0)
							{
								return "";
							}
							data?.Push(path.Substring(index + 1));
							return path.Substring(0, index);
						}
					}
					while (nestPath.Count > 0)
					{
						nestPath.Pop();
						EditorGUI.indentLevel--;
					}
				}
			}

			ApplyRevertGUI();
		}


		protected override bool OnApplyRevertGUI()
		{
			var enabled = GUI.enabled;
			try
			{
				GUI.enabled = true;
				if (GUILayout.Button("Apply", GUILayout.ExpandWidth(false)))
				{
					Save();
				}
				return false;
			}
			finally
			{
				GUI.enabled = enabled;
			}
		}

		void Save()
		{
			foreach (var kvp in m_AssetDatas)
			{
				var data = m_Data.AssetData.Find(x => x.SourceLocalId == kvp.Key);
				data.Params.Clear();
				var meta = OverrideMetaData.Get(kvp.Value.Source.GetType());
				foreach (var (name, field) in meta.OverrideParams)
				{
					if (kvp.Value.Property.TryGetValue(name, out var property))
					{
						data.Params.Add(OverrideParamData.Entry.Create(name, property.boxedValue));
					}
				}
			}
			{
				var data = EditorJsonUtility.ToJson(m_Data, true);
				File.WriteAllText(m_Path, data);
				var importer = (target as OverrideAssetImporter);
				AssetDatabase.ImportAsset(importer.assetPath);
			}
		}

		void Init(OverrideAssetImporter importer)
		{
			var text = File.ReadAllText(importer.assetPath);
			var data = new OverrideParamData();
			EditorJsonUtility.FromJsonOverwrite(text, data);
			m_Data = data;

			var path = AssetDatabase.GUIDToAssetPath(data.Guid);
			m_AssetDatas.Clear();
			foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
			{
				if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long localId))
				{
					continue;
				}
				var assetPropertyData = new AssetPropertyData();
				m_AssetDatas[localId] = assetPropertyData;
				assetPropertyData.Source = obj;
				assetPropertyData.SerializedObject = new SerializedObject(obj);

				var assetData = data.AssetData.Find(x => x.SourceLocalId == localId);
				if (assetData == null)
				{
					data.AssetData.Add(assetData = new OverrideParamData.ParamData()
					{
						SourceLocalId = localId,
					});
				}
				var so = new SerializedObject(obj);
				var meta = OverrideMetaData.Get(obj.GetType());
				foreach (var (name, field) in meta.OverrideParams)
				{
					var property = so.FindProperty(name);
					if (property == null)
					{
						continue;
					}
					assetPropertyData.Property[name] = property;
					var value = assetData.Get(name, field.FieldType);
					if (value == null)
					{
						assetData.Params.Add(OverrideParamData.Entry.Create(field.Name, property.boxedValue));
					}
					else
					{
						property.boxedValue = value;
					}
				}
			}
		}

	}
}