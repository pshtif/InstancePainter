/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    public abstract class ToolBase
    {
        private static Mesh _mouseHitMesh;
        private static RaycastHit _mouseRaycastHit;
        private static Transform _mouseHitTransform;

        public IPEditorCore Core => IPEditorCore.Instance;
        
        public abstract void DrawSceneGUI(SceneView p_sceneView);
        
        public virtual void Selected()
        {
            IPRuntimeEditorCore.renderingAsUtil = false;   
        }
        
        public virtual void Unselected()
        {
            IPRuntimeEditorCore.renderingAsUtil = false;   
        }
        
        public void Handle()
        {
            Tools.current = Tool.None;
            
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (Event.current.isMouse)
            {
                if (HandleMouseHit())
                {
                    SceneView.RepaintAll();
                }
            }
            
            if (_mouseHitTransform == null || (_mouseHitTransform?.GetComponent<MeshFilter>() == null && _mouseHitTransform?.GetComponent<Collider>() == null))
                return;
            
            HandleMouseHitInternal(_mouseRaycastHit);
            
            // if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint) 
            //      Event.current.Use();
            EditorUtility.SetDirty(Core.Renderer);
        }

        bool HandleMouseHit()
        {
            RaycastHit hit;
            
            // var include = LayerUtils.GetAllGameObjectsInLayerMask(Core.Config.includeLayerMask);
            // if (include.Length == 0)
            //     return false;
            
            //var exclude = LayerUtils.GetAllGameObjectsInLayers(Core.Config.excludeLayerMask);

            // if (EditorRaycast.RaycastWorld(Event.current.mousePosition, out hit, out _mouseHitTransform,
            //         out _mouseHitMesh, exclude.Length == 0 ? null : exclude, include.Length == 0 ? null : include))
            if (EditorRaycast.RaycastWorld(Event.current.mousePosition, out hit, out _mouseHitTransform,
                    out _mouseHitMesh, Core.Config.includeLayerMask))
            {
                _mouseRaycastHit = hit;
                return true;
            }

            return false;
        }
        
        protected abstract void HandleMouseHitInternal(RaycastHit p_hit);

        public abstract void DrawInspectorGUI();
    }
}
#endif