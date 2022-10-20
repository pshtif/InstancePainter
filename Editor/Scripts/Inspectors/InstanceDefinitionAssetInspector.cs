/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using InstancePainter.Runtime;
using UnityEditor;
using UnityEngine;

namespace InstancePainter.Editor
{
    [CustomEditor(typeof(InstanceDefinition))]
    public class InstanceDefinitionAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("InstanceDefinitions has been made obsolete you need to migrate to PaintDefinitions.",
                MessageType.Warning);
            if (GUILayout.Button("Migrate to Paint Definition", GUILayout.Height(32)))
            {
                PaintDefinition.MigrateFromInstanceDefinition(target as InstanceDefinition);
            }
        }
    }
}