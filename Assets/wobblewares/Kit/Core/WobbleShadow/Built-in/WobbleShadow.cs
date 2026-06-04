namespace Wobblewares.Kit
{
    using UnityEngine;

    public class WobbleShadow : MonoBehaviour
    {
        #region Inspector Variables
        public Transform target;
        public float verticalOffset = 0.1f;
        public float shadowScale = 1.0f;
        #endregion
        
        #region Private
        
        private Projector projector = null;
        private MeshRenderer _meshRenderer = null;

        private void Awake()
        {
            CacheReferences();
            UpdateShadowPosition();
        }
        
        private void Update()
        {
            UpdateShadowPosition();
        }

        private void OnDrawGizmos()
        {
            CacheReferences();
            UpdateShadowPosition();
        }
        
        private void UpdateShadowPosition()
        {
            var localBounds = _meshRenderer.localBounds;
            var worldBounds = _meshRenderer.bounds;
            
            transform.position = new Vector3(worldBounds.center.x, (worldBounds.center.y - worldBounds.size.y / 2.0f) - verticalOffset, worldBounds.center.z);
            
            // This gets you the right orthographic size and aspect ratio.
            // I think this should not be altered by translations, only scales.
            // But it does need to be affected by rotations that change which axes are parallel to the floor.
            var size = Vector3.Scale(localBounds.size, target.localScale * shadowScale);
            
            Axis upAxis = GetClosestAxis(target.transform, Vector3.up);
            Axis widthAxis;
            Axis depthAxis;
                
            if (upAxis == Axis.Forward)
            {
                widthAxis = Axis.Right;
                depthAxis = Axis.Up;
            } 
            else if (upAxis == Axis.Right)
            {
                widthAxis = Axis.Forward;
                depthAxis = Axis.Up;
            } 
            else
            {
                widthAxis = Axis.Right;
                depthAxis = Axis.Forward;
            }
            
            float width = GetComponentFromAxis(size, widthAxis);
            float depth = GetComponentFromAxis(size, depthAxis);
            projector.orthographicSize = width * 0.5f;
            projector.aspectRatio = depth / width;
            transform.rotation = Quaternion.LookRotation(Vector3.down, AxisToWorldDirection(target.transform, widthAxis));
        }
        
        private void CacheReferences()
        {
            if (target == null)
                target = transform.parent;
            
            _meshRenderer = target.GetComponent<MeshRenderer>();

            // Ensure projector
            projector = GetComponent<Projector>();
            if (projector == null)
                Debug.LogError($"A {nameof(Projector)} component is required for {nameof(WobbleShadow)}");
        }
        
        #endregion

        /// <summary>
        /// Returns the axis of first transform that most closely matches the specified axis of second transform.
        /// </summary>
        private Axis GetClosestAxis(Transform t, Vector3 direction)
        {
            float maxDot = float.MinValue;
            Axis maxAxis = Axis.Forward;
            var axes = new[] { Axis.Right, Axis.Forward, Axis.Up };
            foreach (var axis in axes)
            {
                float dot = Mathf.Abs(Vector3.Dot(AxisToWorldDirection(t, axis), direction));
                if (dot > maxDot)
                {
                    maxDot = dot;
                    maxAxis = axis;
                }
            }
            return maxAxis;
        }


        private Vector3 AxisToWorldDirection(Transform t, Axis axis)
        {
            switch (axis)
            {
                case Axis.Right:
                    return t.right;
                case Axis.Up:
                    return t.up;
                case Axis.Forward:
                    return t.forward;
            }

            return Vector3.zero;
        }

        private float GetComponentFromAxis(Vector3 vector, Axis axis)
        {
            switch (axis)
            {
                case Axis.Right:
                    return vector.x;
                case Axis.Up:
                    return vector.y;
                case Axis.Forward:
                    return vector.z;
            }

            return 0.0f;
        }

        private enum Axis
        {
            Right,
            Up,
            Forward
        }
    }
}