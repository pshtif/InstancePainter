/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InstancePainter.Editor
{
    public class LayerUtils
    {
        
        
        public static GameObject[] GetAllGameObjectsInLayer(LayerMask p_layer)
        {
            var filters = StageUtility.GetCurrentStageHandle().FindComponentsOfType<Transform>();
            var result = new List<GameObject>();
            
            foreach (var filter in filters)
            {
                if (1 << filter.gameObject.layer == p_layer)
                {
                    result.Add(filter.gameObject);
                }
            }

            return result.ToArray();
        }
        
        public static GameObject[] GetAllGameObjectsInLayers(LayerMask[] p_layers)
        {
            var filters = StageUtility.GetCurrentStageHandle().FindComponentsOfType<Transform>();
            var result = new List<GameObject>();
            
            foreach (var filter in filters)
            {
                foreach (var layer in p_layers)
                {
                    if (1 << filter.gameObject.layer == layer)
                    {
                        result.Add(filter.gameObject);
                        break;
                    }
                }
            }

            return result.ToArray();
        }
        
        public static T[] GetAllComponentsInLayers<T>(LayerMask[] p_layers) where T : Component
        {
            var filters = StageUtility.GetCurrentStageHandle().FindComponentsOfType<T>();
            var result = new List<T>();
            
            foreach (var filter in filters)
            {
                foreach (var layer in p_layers)
                {
                    if (1 << filter.gameObject.layer == layer)
                    {
                        result.Add(filter);
                        break;
                    }
                }
            }

            return result.ToArray();
        }
    }
}