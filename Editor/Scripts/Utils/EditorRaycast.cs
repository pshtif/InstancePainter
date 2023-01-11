/*
 *	Created by:  Peter @sHTiF Stefcek
 */
#if UNITY_EDITOR

using UnityEditor.SceneManagement;
using UnityEngine.Profiling;

namespace InstancePainter.Editor
{
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    public class EditorRaycast
    {
        static private MethodInfo _internalRaycast;

        static void CacheUnityInternalCall()
        {
            var handleUtility = typeof(Editor).Assembly.GetTypes().FirstOrDefault(t => t.Name == "HandleUtility");
            _internalRaycast =
                handleUtility.GetMethod("IntersectRayMesh", (BindingFlags.Static | BindingFlags.NonPublic));
        }

        public static bool Raycast(Ray p_ray, MeshFilter p_meshFilter, out RaycastHit p_hit)
        {
            if (_internalRaycast == null) CacheUnityInternalCall();

            return Raycast(p_ray, p_meshFilter.sharedMesh, p_meshFilter.transform.localToWorldMatrix, out p_hit);
        }

        public static bool Raycast(Ray p_ray, MeshFilter[] p_meshFilters, out RaycastHit p_hit)
        {
            p_hit = new RaycastHit();
            float minT = Mathf.Infinity;
            foreach (MeshFilter filter in p_meshFilters)
            {
                Mesh mesh = filter.sharedMesh;
                if (!mesh)
                    continue;
                
                RaycastHit localHit;

                if (Raycast(p_ray, mesh, filter.transform.localToWorldMatrix, out localHit))
                {
                    if (localHit.distance < minT)
                    {
                        p_hit = localHit;
                        minT = p_hit.distance;
                    }
                }
            }

            if (minT == Mathf.Infinity)
                return false;

            return true;
        }

        public static bool Raycast(Ray p_ray, Collider[] p_colliders, out RaycastHit p_hit)
        {
            p_hit = new RaycastHit();
            float minT = Mathf.Infinity;

            foreach (Collider collider in p_colliders)
            {
                RaycastHit localHit;
                
                if (collider.Raycast(p_ray, out localHit, Mathf.Infinity))
                {
                    if (localHit.distance < minT)
                    {
                        p_hit = localHit;
                        minT = p_hit.distance;
                    }
                }
            }

            if (minT == Mathf.Infinity)
                return false;

            return true;
        }

        private static bool Raycast(Ray p_ray, Mesh p_mesh, Matrix4x4 p_matrix, out RaycastHit p_hit)
        {
            if (_internalRaycast == null) CacheUnityInternalCall();

            var parameters = new object[] { p_ray, p_mesh, p_matrix, null };
            bool result = (bool)_internalRaycast.Invoke(null, parameters);
            p_hit = (RaycastHit)parameters[3];
            return result;
        }
        
        // public static bool RaycastWorld(Vector2 p_position, out RaycastHit p_hit, out Transform p_transform,
        //     out Mesh p_mesh)
        // {
        //     p_hit = new RaycastHit();
        //     p_transform = null;
        //     p_mesh = null;
        //
        //     GameObject picked = HandleUtility.PickGameObject(p_position, false);
        //     if (!picked)
        //         return false;
        //
        //     Ray mouseRay = HandleUtility.GUIPointToWorldRay(p_position);
        //
        //     MeshFilter[] meshFil = picked.GetComponentsInChildren<MeshFilter>();
        //     float minT = Mathf.Infinity;
        //     foreach (MeshFilter mf in meshFil)
        //     {
        //         Mesh mesh = mf.sharedMesh;
        //         if (!mesh)
        //             continue;
        //         RaycastHit localHit;
        //
        //         if (Raycast(mouseRay, mesh, mf.transform.localToWorldMatrix, out localHit))
        //         {
        //             if (localHit.distance < minT)
        //             {
        //                 p_hit = localHit;
        //                 p_transform = mf.transform;
        //                 p_mesh = mesh;
        //                 minT = p_hit.distance;
        //             }
        //         }
        //     }
        //
        //     if (minT == Mathf.Infinity)
        //     {
        //         return false;
        //     }
        //
        //     return true;
        // }
        
        // public static bool RaycastWorld(Vector2 p_position, out RaycastHit p_hit)
        // {
        //     p_hit = new RaycastHit();
        //
        //     GameObject picked = HandleUtility.PickGameObject(p_position, false);
        //     if (!picked)
        //         return false;
        //
        //     Ray mouseRay = HandleUtility.GUIPointToWorldRay(p_position);
        //
        //     MeshFilter[] meshFil = picked.GetComponentsInChildren<MeshFilter>();
        //     float minT = Mathf.Infinity;
        //     foreach (MeshFilter mf in meshFil)
        //     {
        //         Mesh mesh = mf.sharedMesh;
        //         if (!mesh)
        //             continue;
        //         RaycastHit localHit;
        //
        //         if (Raycast(mouseRay, mesh, mf.transform.localToWorldMatrix, out localHit))
        //         {
        //             if (localHit.distance < minT)
        //             {
        //                 p_hit = localHit;
        //                 minT = p_hit.distance;
        //             }
        //         }
        //     }
        //
        //     if (minT == Mathf.Infinity)
        //         return false;
        //
        //     return true;
        // }

        // Taken from Unity codebase for object raycasting in sceneview - sHTiF
        public static bool RaycastWorld_OBSOLETE(Vector2 p_position, out RaycastHit p_hit, out Transform p_transform,
            out Mesh p_mesh, GameObject[] p_ignore, GameObject[] p_filter)
        {
            p_hit = new RaycastHit();
            p_transform = null;
            p_mesh = null;

            GameObject picked = HandleUtility.PickGameObject(p_position, false, p_ignore, p_filter);

            if (!picked)
                return false;

            Ray mouseRay = HandleUtility.GUIPointToWorldRay(p_position);

            MeshFilter[] meshFil = picked.GetComponentsInChildren<MeshFilter>();
            float minT = Mathf.Infinity;
            foreach (MeshFilter mf in meshFil)
            {
                Mesh mesh = mf.sharedMesh;
                if (!mesh)
                    continue;
                RaycastHit localHit;

                if (Raycast(mouseRay, mesh, mf.transform.localToWorldMatrix, out localHit))
                {
                    if (localHit.distance < minT)
                    {
                        p_hit = localHit;
                        p_transform = mf.transform;
                        p_mesh = mesh;
                        minT = p_hit.distance;
                    }
                }
            }
            
            if (minT == Mathf.Infinity)
            {
                Collider[] colliders = picked.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    RaycastHit localHit;
                    if (col.Raycast(mouseRay, out localHit, Mathf.Infinity))
                    {
                        if (localHit.distance < minT)
                        {
                            p_hit = localHit;
                            p_transform = col.transform;
                            minT = p_hit.distance;
                        }
                    }
                }
            }

            if (minT == Mathf.Infinity)
            {
                //p_hit.point = Vector3.Project(picked.transform.position - mouseRay.origin, mouseRay.direction) + mouseRay.origin;
                return false;
            }

            return true;
        }
        
         public static bool RaycastWorld(Vector2 p_position, out RaycastHit p_hit, out Transform p_transform,
            out Mesh p_mesh, GameObject[] p_ignore, GameObject[] p_filter)
        {
            p_hit = new RaycastHit();
            p_transform = null;
            p_mesh = null;

            MeshFilter[] meshes;
            if (p_filter != null)
            {
                meshes = new MeshFilter[0];
                foreach (var gameObject in p_filter)
                {
                    meshes = meshes.Concat(gameObject.GetComponentsInChildren<MeshFilter>()).ToArray();
                }

                meshes = p_filter[0].GetComponentsInChildren<MeshFilter>();
            }
            else
            {
                meshes = StageUtility.GetCurrentStageHandle().FindComponentsOfType<MeshFilter>();
            }

            Ray mouseRay = HandleUtility.GUIPointToWorldRay(p_position);

            float minT = Mathf.Infinity;
            foreach (MeshFilter mf in meshes)
            {
                Mesh mesh = mf.sharedMesh;
                if (!mesh)
                    continue;
                RaycastHit localHit;

                if (Raycast(mouseRay, mesh, mf.transform.localToWorldMatrix, out localHit))
                {
                    if (localHit.distance < minT)
                    {
                        p_hit = localHit;
                        p_transform = mf.transform;
                        p_mesh = mesh;
                        minT = p_hit.distance;
                    }
                }
            }
            
            SkinnedMeshRenderer[] skinnedMeshes = StageUtility.GetCurrentStageHandle().FindComponentsOfType<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer sm in skinnedMeshes)
            {
                Mesh mesh = sm.sharedMesh;
                if (!mesh)
                    continue;
                RaycastHit localHit;

                if (Raycast(mouseRay, mesh, sm.transform.localToWorldMatrix, out localHit))
                {
                    if (localHit.distance < minT)
                    {
                        p_hit = localHit;
                        p_transform = sm.transform;
                        p_mesh = mesh;
                        minT = p_hit.distance;
                    }
                }
            }
            
            if (minT == Mathf.Infinity)
            {
                Collider[] colliders = StageUtility.GetCurrentStageHandle().FindComponentsOfType<Collider>();
                foreach (Collider col in colliders)
                {
                    RaycastHit localHit;
                    if (col.Raycast(mouseRay, out localHit, Mathf.Infinity))
                    {
                        if (localHit.distance < minT)
                        {
                            p_hit = localHit;
                            p_transform = col.transform;
                            minT = p_hit.distance;
                        }
                    }
                }
            }

            if (minT == Mathf.Infinity)
            {
                //p_hit.point = Vector3.Project(picked.transform.position - mouseRay.origin, mouseRay.direction) + mouseRay.origin;
                return false;
            }

            return true;
        }
    }
}
#endif