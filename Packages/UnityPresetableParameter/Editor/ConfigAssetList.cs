using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;

namespace UnityConfigurableParameter
{

	public static class ConfigAssetList
	{

		public static string[] GetList(Type type)
		{
			List<string> list = new();
			var name = type.FullName;
			foreach (var guid in AssetDatabase.FindAssets($"t:UnityConfigurableParameter.ConfigAsset"))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var importer = AssetImporter.GetAtPath(path);
				if (string.IsNullOrEmpty(importer.userData))
				{
					var asset = AssetDatabase.LoadAssetAtPath<ConfigAsset>(path);
					if (asset.Value == null)
					{
						continue;
					}
					importer.userData = asset.Value.GetType().FullName;
					importer.SaveAndReimport();
				}
				if (importer.userData == name)
				{
					list.Add(path);
				}
			}
			return list.ToArray();
		}

		internal static void Select(Type type, Action<ConfigAsset> value)
		{
			var provider = new SearchProvider("presetable", (ctx, list, provider) =>
			{
				foreach (var path in GetList(type))
				{
					var item = provider.CreateItem(path);
					list.Add(provider.CreateItem(path));
				}
				return null;
			});
			provider.toObject = (item, type) =>
			{
				return AssetDatabase.LoadAssetAtPath<ConfigAsset>(item.id);
			};
			var ctx = SearchService.CreateContext(provider);
			SearchService.ShowPicker(ctx, (item, ok) =>
			{
				var obj = item.ToObject<ConfigAsset>();
				value(obj);
			});
		}
	}
}