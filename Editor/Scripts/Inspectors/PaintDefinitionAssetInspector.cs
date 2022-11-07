/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEditor;

namespace BinaryEgo.InstancePainter.Editor
{
    [CustomEditor(typeof(PaintDefinition))]
    public class PaintDefinitionAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}