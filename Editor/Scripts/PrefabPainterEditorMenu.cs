/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEditor;

namespace PrefabPainter.Editor
{
    public class PrefabPainterEditorMenu
    {
        [MenuItem("Tools/Prefab Painter/Enabled")]
        private static void ToggleEnabled()
        {
            PrefabPainterEditorCore.Config.enabled = !PrefabPainterEditorCore.Config.enabled;
        }

        [MenuItem("Tools/Prefab Painter/Enabled", true)]
        private static bool ToggleActionValidate()
        {
            Menu.SetChecked("Tools/Prefab Painter/Enabled", PrefabPainterEditorCore.Config.enabled);
            return true;
        }
    }
}