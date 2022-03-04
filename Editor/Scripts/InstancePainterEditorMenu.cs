/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEditor;

namespace InstancePainter.Editor
{
    public class InstancePainterEditorMenu
    {
        [MenuItem("Tools/Instance Painter/Enabled")]
        private static void ToggleEnabled()
        {
            InstancePainterEditorCore.Config.enabled = !InstancePainterEditorCore.Config.enabled;
        }

        [MenuItem("Tools/Instance Painter/Enabled", true)]
        private static bool ToggleActionValidate()
        {
            Menu.SetChecked("Tools/Instance Painter/Enabled", InstancePainterEditorCore.Config.enabled);
            return true;
        }
    }
}