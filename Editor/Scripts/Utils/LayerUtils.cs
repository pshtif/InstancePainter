/*
 *	Created by:  Peter @sHTiF Stefcek
 */

using System.Collections.Generic;
using UnityEngine;

namespace PrefabPainter.Editor
{
    public class LayerUtils
    {
        
        
        public static GameObject[] GetAllMeshObjectsInLayer(LayerMask p_layer)
        {
            var filters = GameObject.FindObjectsOfType<MeshFilter>();
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
        
        public static GameObject[] GetAllMeshObjectsInLayers(LayerMask[] p_layers)
        {
            var filters = GameObject.FindObjectsOfType<MeshFilter>();
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
        
        public static MeshFilter[] GetAllMeshFiltersInLayers(LayerMask[] p_layers)
        {
            var filters = GameObject.FindObjectsOfType<MeshFilter>();
            var result = new List<MeshFilter>();
            
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