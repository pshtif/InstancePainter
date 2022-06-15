using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InstancePainter
{
    public class InstanceCollection : ScriptableObject
    {
        //[HideInInspector]
        [SerializeField]
        private Matrix4x4[] _matrixData;

        public Matrix4x4[] MatrixData => _matrixData;
        
        //[HideInInspector]
        [SerializeField]
        private Vector4[] _colorData;

        public Vector4[] ColorData => _colorData;
        
        #if UNITY_EDITOR
        public void SetData(Matrix4x4[] p_matrixData, Vector4[] p_colorData)
        {
            _matrixData = p_matrixData;
            _colorData = p_colorData;
        }
        
        public static InstanceCollection CreateAssetWithPanel()
        {
            var path = EditorUtility.SaveFilePanelInProject(
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
        
        public static InstanceCollection CreateAsAssetFromPath(string p_path)
        {
            InstanceCollection collection = ScriptableObject.CreateInstance<InstanceCollection>();

            AssetDatabase.CreateAsset(collection, p_path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return collection;
        }
        #endif
    }
}