/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InstancePainter.Editor
{
    public class LayerUtils
    {
        public static bool IsGameObjectInLayerMask(GameObject p_gameObject, LayerMask p_layerMask)
        {
            return (p_layerMask.value & (1 << p_gameObject.layer)) != 0;
        }
        
        public static GameObject[] GetAllGameObjectsInLayerMask(LayerMask p_layer)
        {
            var filters = StageUtility.GetCurrentStageHandle().FindComponentsOfType<Transform>();
            var result = new List<GameObject>();
            
            foreach (var filter in filters)
            {
                //if (1 << filter.gameObject.layer == p_layer)
                if (IsGameObjectInLayerMask(filter.gameObject, p_layer))
                {
                    result.Add(filter.gameObject);
                }
            }

            return result.ToArray();
        }
    }
}
#endif