/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using UnityEngine;

namespace InstancePainter.Editor
{
    public static class StyleUtils
    {
        private static GUIStyle _sectionStyleCenter;

        public static GUIStyle SectionStyleCenter
        {
            get
            {
                if (_sectionStyleCenter == null)
                {
                    _sectionStyleCenter = new GUIStyle();
                    _sectionStyleCenter.normal.background = TextureUtils.GetColorTexture(new Color(.1f, .1f, .1f));
                    _sectionStyleCenter.normal.textColor = Color.white;
                    _sectionStyleCenter.fontStyle = FontStyle.Bold;
                    _sectionStyleCenter.alignment = TextAnchor.MiddleCenter;
                    _sectionStyleCenter.fontSize = 14;
                }

                return _sectionStyleCenter;
            }
        }
        
        private static GUIStyle _titleStyleRight;
        
        public static GUIStyle TitleStyleRight
        {
            get
            {
                if (_titleStyleRight == null)
                {
                    _titleStyleRight = new GUIStyle();
                    _titleStyleRight.normal.background = TextureUtils.GetColorTexture(new Color(.1f, .1f, .1f));
                    _titleStyleRight.normal.textColor = Color.white;
                    _titleStyleRight.fontStyle = FontStyle.Bold;
                    _titleStyleRight.alignment = TextAnchor.MiddleRight;
                    _titleStyleRight.fontSize = 14;
                }

                return _titleStyleRight;
            }
        }
        
        private static GUIStyle _titleStyleCount;
        
        public static GUIStyle TitleStyleCount
        {
            get
            {
                if (_titleStyleCount == null)
                {
                    _titleStyleCount = new GUIStyle();
                    _titleStyleCount.normal.background = TextureUtils.GetColorTexture(new Color(.1f, .1f, .1f));
                    _titleStyleCount.normal.textColor = new Color(0.9f, 0.5f, 0);
                    _titleStyleCount.fontStyle = FontStyle.Bold;
                    _titleStyleCount.alignment = TextAnchor.MiddleLeft;
                    _titleStyleCount.fontSize = 14;
                }

                return _titleStyleCount;
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