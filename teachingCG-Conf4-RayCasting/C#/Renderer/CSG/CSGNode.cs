using GMath;
using Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static GMath.Gfx;
using static Rendering.Raycasting;

namespace Renderer.CSG
{
    public class CSGNode : IRaycastGeometry<float3>
    {
        public enum CSGOperation { None, Union, Intersection, Difference }

        public IRaycastGeometry<float3> Geometry { get; private set; }

        public float4x4 Transform { get; }

        public float3? LowerBound { get; }
        
        public float3? UpperBound { get; }
        
        public CSGOperation Operation { get; }

        public CSGNode LeftChild { get; set; } 
        
        public CSGNode RightChild { get; set; } 

        // Operation Constructor
        private CSGNode(CSGOperation operation)
        {
            Operation = operation;
        }

        // Leaf Constructor
        public CSGNode(float3x3 Q, float3 P, float R) : this(CSGOperation.None)
        {
            Geometry = Raycasting.Quadric(Q,P,R);
        }

        // Leaf Contructor
        public CSGNode(IRaycastGeometry<float3> geometry, float4x4? transform = null, float3? lowerBound=null, float3? upperBound=null)
        {
            Geometry = geometry;
            Transform = transform != null ? transform.Value : Transforms.Identity;
            LowerBound = lowerBound;
            UpperBound = upperBound;
            ApplyBounds();
        }

        private void ApplyBounds()
        {
            if (LowerBound.HasValue || UpperBound.HasValue)
            {
                var lower = LowerBound ?? float3(-100000, -100000, -100000);
                var upper = UpperBound ?? float3(100000, 100000, 100000);
                var lower4x1 = mul(Transform, float4x1(lower.x, lower.y, lower.z, 1));
                var upper4x1 = mul(Transform, float4x1(upper.x, upper.y, upper.z, 1));
                lower = float3(lower4x1._m00, lower4x1._m10, lower4x1._m20);
                upper = float3(upper4x1._m00, upper4x1._m10, upper4x1._m20);
                Geometry = new CSGNode(Geometry) & new CSGNode(Box(lower,upper));
            }
        }

        public static CSGNode operator | (CSGNode a, CSGNode b) => new CSGNode(CSGOperation.Union)
        {
            LeftChild = a,
            RightChild = b
        };

        public static CSGNode operator & (CSGNode a, CSGNode b) => new CSGNode(CSGOperation.Intersection)
        {
            LeftChild = a,
            RightChild = b
        };

        public static CSGNode operator / (CSGNode a, CSGNode b) => new CSGNode(CSGOperation.Difference)
        {
            LeftChild = a,
            RightChild = b
        };

        public IEnumerable<HitInfo<float3>> Raycast(RayDescription ray)
        {
            var hitsLeft = LeftChild?.Raycast(ray);
            var hitsRight = RightChild?.Raycast(ray);
            switch (Operation)
            {
                case CSGOperation.None:
                    return LeafRaycast(ray);
                case CSGOperation.Union:
                    return Union(hitsLeft, hitsRight);
                case CSGOperation.Intersection:
                    return Intersection(hitsLeft, hitsRight);
                case CSGOperation.Difference:
                    return Difference(hitsLeft, hitsRight);
                default:
                    throw new NotImplementedException($"Operation {Operation} is not implemented");
            }
        }

        private IEnumerable<HitInfo<float3>> LeafRaycast(RayDescription ray)
        {
            var FromGeometryToWorld = Transform;
            var FromWorldToGeometry = inverse(FromGeometryToWorld);
            var LocalRay = ray.Transform(FromWorldToGeometry);
            return Geometry.Raycast(LocalRay);
        }

        private IEnumerable<HitInfo<float3>> Difference(IEnumerable<HitInfo<float3>> hitsLeft, IEnumerable<HitInfo<float3>> hitsRight)
        {
            static bool differenceSelector(bool insideLeft, bool insideRight, bool isLeft)
            {
                return (!insideLeft && !insideRight &&  isLeft)
                    || ( insideLeft && !insideRight && !isLeft)
                    || ( insideLeft && !insideRight &&  isLeft)
                    || ( insideLeft &&  insideRight && !isLeft);
            };
            return RayOperation(differenceSelector, hitsLeft, hitsRight);
        }

        private IEnumerable<HitInfo<float3>> Intersection(IEnumerable<HitInfo<float3>> hitsLeft, IEnumerable<HitInfo<float3>> hitsRight)
        {
            static bool intersectionSelector(bool insideLeft, bool insideRight, bool isLeft)
            {
                return !((!insideLeft && !insideRight && !isLeft)
                      || (!insideLeft && !insideRight &&  isLeft)
                      || (!insideLeft &&  insideRight && !isLeft)
                      || ( insideLeft && !insideRight &&  isLeft));
            };
            return RayOperation(intersectionSelector, hitsLeft, hitsRight);
        }

        private IEnumerable<HitInfo<float3>> Union(IEnumerable<HitInfo<float3>> hitsLeft, IEnumerable<HitInfo<float3>> hitsRight)
        {
            static bool unionSelector(bool insideLeft, bool insideRight, bool isLeft)
            {
                return (!insideLeft && !insideRight && !isLeft)
                    || (!insideLeft && !insideRight && isLeft)
                    || (!insideLeft && insideRight && !isLeft)
                    || (insideLeft && !insideRight && isLeft);
            };
            return RayOperation(unionSelector, hitsLeft, hitsRight);
        }

        private IEnumerable<HitInfo<float3>> RayOperation(Func<bool, bool, bool, bool> selector, IEnumerable<HitInfo<float3>> hitsLeft, IEnumerable<HitInfo<float3>> hitsRight)
        {
            bool insideLeft = false, insideRight = false;

            // returns the hits ordered by T
            foreach (var (isLeft, hit) in hitsLeft.Select(x => (true, x)).Concat(hitsRight.Select(x => (false, x))).OrderBy(x => x.x.T).ThenBy(x => x.Item1))
            {
                if (selector(insideLeft, insideRight, isLeft))
                    yield return hit;

                // TODO Verify tangent intersections
                if (isLeft)
                    insideLeft = !insideLeft;
                else
                    insideRight = !insideRight;
            }
        }
    }
}
