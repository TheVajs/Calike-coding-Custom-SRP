using System;
using System.Collections.Generic;
using UnityEngine;

namespace Procedureal_grass
{
    public class ColliderInstances : MonoBehaviour
    {
        public List<Vector3> insidePositions = new List<Vector3>();
        public List<Collider> colliders = new List<Collider>();
        
        [Tooltip("A mesh to extrude the pyramids from")]
        [SerializeField] private Mesh sourceMesh;
        [SerializeField] private Transform t;

        private bool init;
        
        private void Start()
        {
            var positions = sourceMesh.vertices;
            for (var i = 0; i < positions.Length; i++)
            {
                var position = t.localToWorldMatrix.MultiplyPoint3x4(positions[i]);
                foreach (var coll in colliders)
                {
                    var closest = coll.ClosestPoint(position);
                    print(closest + " " + position);
                    if(closest == position)
                        insidePositions.Add(positions[i]);
                }
            }
        }

        private void OnTriggerStay (Collider other)
        {
            if (other.CompareTag("Ground")) return;
            if (Vector3.Magnitude(other.bounds.size) < 1f) return;
            if (!colliders.Contains(other))
            {
                colliders.Add(other);
            }
        }
 
        private void OnTriggerExit (Collider other) {
            colliders.Remove(other);
        }
    }
}