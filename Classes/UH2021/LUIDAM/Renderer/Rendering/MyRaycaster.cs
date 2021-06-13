using GMath;
using System;
using System.Collections.Generic;
using System.Linq;
using static GMath.Gfx;
using System.Text;
using static Rendering.Raycasting;
using Renderer.CSG;

namespace Rendering
{
    public static class MyRaycaster
    {

        #region Box

        class BoxGeometry : IRaycastGeometry<float3>
        {
            PlaneGeometry XYlow;
            PlaneGeometry XYup;
            PlaneGeometry XZlow;
            PlaneGeometry XZup;
            PlaneGeometry YZlow;
            PlaneGeometry YZup;
            float3 lowerBound;
            float3 upperBound;

            public BoxGeometry(float3 lowerBound, float3 upperBound)
            {
                this.lowerBound = lowerBound;
                this.upperBound = upperBound;
                float3 X = float3(1, 0, 0), Y = float3(0, 1, 0), Z = float3(0, 0, 1);
                XYlow = new PlaneGeometry(lowerBound, Z);
                XYup = new PlaneGeometry(upperBound, Z);
                XZlow = new PlaneGeometry(lowerBound, Y);
                XZup = new PlaneGeometry(upperBound, Y);
                YZlow = new PlaneGeometry(lowerBound, X);
                YZup = new PlaneGeometry(upperBound, X);
            }

            public IEnumerable<HitInfo<float3>> Raycast(RayDescription ray)
            {
                float epsilon = 0.0001f;
                foreach (var item in XYlow.Raycast(ray)
                             .Concat(XYup.Raycast(ray)
                             .Concat(XZlow.Raycast(ray)
                             .Concat(XZup.Raycast(ray)
                             .Concat(YZlow.Raycast(ray)
                             .Concat(YZup.Raycast(ray))))))
                                 .OrderBy(x => x.T))
                {
                    if (lowerBound.x <= item.Attribute.x + epsilon && item.Attribute.x <= upperBound.x + epsilon &&
                        lowerBound.y <= item.Attribute.y + epsilon && item.Attribute.y <= upperBound.y + epsilon &&
                        lowerBound.z <= item.Attribute.z + epsilon && item.Attribute.z <= upperBound.z + epsilon)
                    {
                        yield return item;
                        break;
                    }
                }
            }
        }

        public static IRaycastGeometry<float3> Box(float3 lowerBound, float3 upperBound)
        {
            return new BoxGeometry(lowerBound, upperBound);
        }

        #endregion

        #region Quadric


        public static IRaycastGeometry<float3> Cylinder(float radius = 1, string plane = "xy", float3? lowerBound = null, float3? upperBound = null)
        {
            if (plane.Length != 2 || plane.Where(x => x != 'x' && x != 'y' && x != 'z').Any())
                throw new ArgumentException("plane must have 2 chars plane (x,y,z). Example: xy");
            float3x3 Q = new float3x3(plane.Contains('x') ? 1 : 0, 0, 0, 0, plane.Contains('y') ? 1 : 0, 0, 0, 0, plane.Contains('z') ? 1 : 0);
            var cylinder = new QuadricGeometry(Q, float3(0, 0, 0), -radius * radius);
            if (!lowerBound.HasValue && !upperBound.HasValue)
            {
                return cylinder;
            }
            return new CSGNode(cylinder, lowerBound: lowerBound, upperBound: upperBound);
        }

        public static IRaycastGeometry<float3> Pipe(float radius = 1, float thickness = 0, string plane = "xy", float3? lowerBound = null, float3? upperBound = null)
        {
            if (thickness == 0)
                return Cylinder(radius, plane);
            var cylinder1 = new CSGNode(Cylinder(radius, plane, lowerBound, upperBound));
            var cylinder2 = new CSGNode(Cylinder(radius + thickness, plane, lowerBound, upperBound));
            return cylinder1 | cylinder2;
        }


        #endregion
    }
}
