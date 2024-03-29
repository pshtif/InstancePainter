/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using UnityEditor;

namespace InstancePainter.Editor
{
    public class IPEditorMenu
    {
        [MenuItem("Tools/Instance Painter/Enabled")]
        private static void ToggleEnabled()
        {
            IPEditorCore.Instance.Config.enabled = !IPEditorCore.Instance.Config.enabled;
            EditorUtility.SetDirty(IPEditorCore.Instance.Config);
        }

        [MenuItem("Tools/Instance Painter/Enabled", true)]
        private static bool ToggleActionValidate()
        {
            Menu.SetChecked("Tools/Instance Painter/Enabled", IPEditorCore.Instance.Config.enabled);
            return true;
        }
        
        [MenuItem("Tools/Instance Painter/Settings")]
        private static void ShowSettings()
        {
            InstancePainterWindow.InitEditorWindow();
        }
    }
}
#endif