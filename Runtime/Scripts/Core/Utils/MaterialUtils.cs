/*
 *	Created by:  Peter @sHTiF Stefcek
 */

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BinaryEgo.InstancePainter
{
    public class MaterialUtils
    {
        public static Material DefaultIndirectMaterial
        {
            get
            {
                // Kind of overkill but will find it anywhere, not really performance issue
                var path = AssetDatabase.GetAllAssetPaths()
                    .FirstOrDefault(p => p.Contains("IPDefaultIndirectPixelShadows"));
                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }
        }

        public static Material DefaultFallbackMaterial
        {
            get
            {
                // Kind of overkill but will find it anywhere, not really performance issue
                var path = AssetDatabase.GetAllAssetPaths()
                    .FirstOrDefault(p => p.Contains("IPDefaultFallbackPixelShadows"));
                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }
        }

        private static Material _explicitClusterMaterial;
        
        public static Material ExplicitClusterMaterial
        {
            get
            {
                if (_explicitClusterMaterial == null)
                {
                    _explicitClusterMaterial = new Material(Shader.Find("Hidden/Instance Painter/InstancedIndirectUtility"));
                    _explicitClusterMaterial.color = Color.green;
                }

                return _explicitClusterMaterial;
            }
        }
        
        private static Material _nonExplicitClusterMaterial;
        
        public static Material NonExplicitClusterMaterial
        {
            get
            {
                if (_nonExplicitClusterMaterial == null)
                {
                    _nonExplicitClusterMaterial = new Material(Shader.Find("Hidden/Instance Painter/InstancedIndirectUtility"));
                    _nonExplicitClusterMaterial.color = Color.red;
                }

                return _nonExplicitClusterMaterial;
            }
        }
    }
}
#endif