/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace InstancePainter
{
    public class InstanceDataAsset : ScriptableObject, IData
    {
        public InstanceData collection;

        public InstanceDataAsset()
        {
            collection = new InstanceData();
        }

        public void Invalidate(bool p_fallback)
        {
            collection.Invalidate(p_fallback);
        }

        public void Dispose()
        {
            collection.Dispose();
        }
        
#if UNITY_EDITOR
        public static InstanceDataAsset CreateAssetWithPanel()
        {
            var path = UnityEditor.EditorUtility.SaveFilePanelInProject(
                "Create Instance Collection",
                "InstanceCollection",
                "asset",
                "Enter name for new Instance Collection.");
            
            if (path.Length != 0)
            {
                return CreateAsAssetFromPath(path);
            }
            
            return null;
        }
        
        public static InstanceDataAsset CreateAsAssetFromPath(string p_path)
        {
            InstanceDataAsset asset = ScriptableObject.CreateInstance<InstanceDataAsset>();

            UnityEditor.AssetDatabase.CreateAsset(asset, p_path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            return asset;
        }
#endif
    }
}