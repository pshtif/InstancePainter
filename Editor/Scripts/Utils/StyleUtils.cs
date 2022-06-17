/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace InstancePainter.Editor
{
    public static class StyleUtils
    {
        private static GUIStyle _titleStyle;
        
        public static GUIStyle TitleStyle
        {
            get
            {
                if (_titleStyle == null)
                {
                    _titleStyle = new GUIStyle();
                    _titleStyle.normal.background = TextureUtils.GetColorTexture(new Color(.1f, .1f, .1f));
                    _titleStyle.normal.textColor = new Color(1, 0.5f, 0);
                    _titleStyle.fontStyle = FontStyle.Bold;
                    _titleStyle.alignment = TextAnchor.MiddleCenter;
                    _titleStyle.fontSize = 14;
                }

                return _titleStyle;
            }
        }
        
        private static GUIStyle _clusterStyle;
        
        public static GUIStyle ClusterStyle
        {
            get
            {
                if (_clusterStyle == null)
                {
                    _clusterStyle = new GUIStyle();
                    _clusterStyle.normal.background = TextureUtils.GetColorTexture(new Color(.15f, .15f, .15f));
                    _clusterStyle.normal.textColor = Color.white;
                    _clusterStyle.alignment = TextAnchor.MiddleLeft;
                    _clusterStyle.padding.left = 10;
                    _clusterStyle.fontSize = 12;
                }

                return _clusterStyle;
            }
        }
        
        private static GUIStyle _clusterMeshNameStyle;
        
        public static GUIStyle ClusterMeshNameStyle
        {
            get
            {
                if (_clusterMeshNameStyle == null)
                {
                    _clusterMeshNameStyle = new GUIStyle();
                    _clusterMeshNameStyle.normal.textColor = Color.white;
                    _clusterMeshNameStyle.alignment = TextAnchor.MiddleRight;
                    _clusterMeshNameStyle.fontSize = 12;
                }

                return _clusterMeshNameStyle;
            }
        }
    }
}