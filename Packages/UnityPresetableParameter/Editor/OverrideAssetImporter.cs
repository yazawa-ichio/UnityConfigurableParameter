using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityConfigurableParameter
{

	[ScriptedImporter(1, ".overrideasset")]
	public partial class OverrideAssetImporter : ScriptedImporter
	{
		[MenuItem("Assets/Create/OverrideAsset")]
		public static void Create()
		{
			var obj = Selection.objects.OfType<ScriptableObject>().FirstOrDefault();
			if (obj == null)
			{
				return;
			}
			var path = Path.ChangeExtension(AssetDatabase.GetAssetPath(obj), ".overrideasset");
			var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
			var data = new OverrideParamData() { Guid = guid };
			var buf = System.Text.Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(data, true));
			File.WriteAllBytes(path, buf);
		}

		public override void OnImportAsset(AssetImportContext ctx)
		{
			var data = new OverrideParamData();
			EditorJsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(ctx.assetPath), data);

			ctx.DependsOnSourceAsset(new GUID(data.Guid));
			var path = AssetDatabase.GUIDToAssetPath(data.Guid);
			ctx.DependsOnSourceAsset(path);

			var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
			if (asset == null)
			{
				return;
			}
			Dictionary<Object, Object> replaceMap = new();
			var main = replaceMap[asset] = Instantiate(asset);

			foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
			{
				var replace = Instantiate(obj);
				replace.name = obj.name;
				replaceMap[obj] = replace;
			}

			foreach (var obj in replaceMap)
			{
				AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj.Key, out string guid, out long localId);
				ctx.AddObjectToAsset(guid + ":" + localId, obj.Value);
			}
			ctx.SetMainObject(main);

			foreach (var kvp in replaceMap)
			{
				data.Apply(kvp.Key, kvp.Value);
			}

		}
	}
}