using System;
using System.IO;
using UnityEngine;

namespace QuartzDistribution
{
    public static class QuartzDataLoader
    {
        private const string Folder = "QuartzMap";

        public static ResourceTypeCollection LoadResourceTypes()
        {
            return Load<ResourceTypeCollection>("QuartzResourceTypes.json");
        }

        public static MapConfigData LoadMap(string mapId)
        {
            string file = mapId == "altay" ? "QuartzMapMarkers_Altay.json" : "QuartzMapMarkers_National.json";
            return Load<MapConfigData>(file);
        }

        private static T Load<T>(string fileName) where T : class
        {
            string path = Path.Combine(Application.streamingAssetsPath, Folder, fileName);
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogError("[QuartzMap] 找不到数据文件: " + path);
                    return null;
                }

                return JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogError("[QuartzMap] 读取数据失败: " + exception.Message);
                return null;
            }
        }
    }
}
