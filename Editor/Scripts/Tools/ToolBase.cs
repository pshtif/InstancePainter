/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    public abstract class ToolBase
    {
        private static Mesh _mouseHitMesh;
        private static RaycastHit _mouseRaycastHit;
        private static Transform _mouseHitTransform;
        
        public InstancePainterEditorConfig Config => InstancePainterEditorCore.Config;
        
        public abstract void DrawSceneGUI(SceneView p_sceneView);

        public void Handle()
        {
            Tools.current = Tool.None;
            
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (Event.current.isMouse)
            {
                HandleMouseHit();
            }
            
            if (_mouseHitTransform?.GetComponent<MeshFilter>() == null)
                return;
            
            HandleInternal(_mouseRaycastHit);
            
            // if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint) 
            //     Event.current.Use();
        }

        void HandleMouseHit()
        {
            RaycastHit hit;

            var include = LayerUtils.GetAllMeshObjectsInLayers(Config.includeLayers.ToArray());
            var exclude = LayerUtils.GetAllMeshObjectsInLayers(Config.excludeLayers.ToArray());
            
            if (EditorRaycast.RaycastWorld(Event.current.mousePosition, out hit, out _mouseHitTransform,
                out _mouseHitMesh, exclude.Length == 0 ? null : exclude, include.Length == 0 ? null : include))
            {
                _mouseRaycastHit = hit;
            }
        }
        
        protected abstract void HandleInternal(RaycastHit p_hit);

        public abstract void DrawInspectorGUI();
    }
}