using System;
using System.Collections.Generic;
using System.Text;
using static GMath.Gfx;
using GMath;
using Rendering;
using static Renderer.Program;

namespace Renderer.Modeling
{
    public static class MeshShapeGenerator<T> where T : struct, IVertex<PositionNormal>
    {
        public static Mesh<PositionNormal> Box(int width, int height, int deep, bool faceXYUp = true, bool faceXYDown = true, bool faceXZUp = true, bool faceXZDown = true, bool faceYZUp = true, bool faceYZDown = true)
        {
            var box = new Mesh<PositionNormal>(new PositionNormal[] { }, new int[] { });
            foreach (var (dirX, dirY, trans) in new (float3 dirX, float3 dirY, float3 trans)[] 
            { 
                (float3(1, 0, 0), float3(0,1,0), float3(0, 0, 0)), 
                (float3(1, 0, 0), float3(0,1,0), float3(0, 0, 1)),
                (float3(1, 0, 0), float3(0,0,1), float3(0, 0, 0)),
                (float3(1, 0, 0), float3(0,0,1), float3(0, 1, 0)),
                (float3(0, 1, 0), float3(0,0,1), float3(0, 0, 0)),
                (float3(0, 1, 0), float3(0,0,1), float3(1, 0, 0)),
            })
            {
                int stacks = 0, slices = 0;
                if (dirX.x != 0)
                {
                    if (dirY.y != 0)
                    {
                        if (length(trans) == 0)
                        {
                            if (!faceXYDown)
                                continue;
                        }
                        else if (!faceXYUp)
                            continue;
                    }
                    else if (dirY.z != 0)
                    {
                        if (length(trans) == 0)
                        {
                            if (!faceXZDown)
                                continue;
                        }
                        else if (!faceXZUp)
                            continue;
                    }
                    stacks = width;
                    slices = (int)Math.Max(dirY.y * height, dirY.z * deep);
                }
                else
                {
                    if (length(trans) == 0)
                    {
                        if (!faceYZDown)
                            continue;
                    }
                    else if (!faceYZUp)
                        continue;
                    stacks = height;
                    slices = deep;
                }
                var face = Manifold<PositionNormal>.Surface(stacks, slices, (x, y) => float3(dirX.x * x + trans.x, dirX.y * x + (dirX.y == 0 ? dirY.y * y : 0) + trans.y, dirY.z * y + trans.z));
                box += face;
            }
            return box.Transform(Transforms.Translate(-.5f, -.5f, -.5f));
        }


        public static Mesh<PositionNormal> Box(int points)
        {
            return Box(points / 3, points / 3, points / 3);
        }

        public static Mesh<PositionNormal> Cylinder(int points, float thickness=0, float angle = 2 * pi, bool surface = false)
        {
            var ss = (int)ceil(sqrt(points));
            var baseCylOuter = new Mesh<PositionNormal>();
            var face1 = Manifold<PositionNormal>.Revolution(ss, ss, x => float3(1 * x, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0,0,.5f));
            var face2 = Manifold<PositionNormal>.Revolution(ss, ss, x => float3(1 * x, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0,0,-.5f));
            
            if (thickness != 0)
            {
                baseCylOuter = Manifold<PositionNormal>.Revolution(ss, ss, x => float3(1 + thickness, 0, x), float3(0, 0, 1), angle).Transform(Transforms.Translate(0, 0, -.5f));
                face1 = Manifold<PositionNormal>.Revolution(ss, ss, x => float3(1 + x * thickness, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0, 0, .5f));
                face2 = Manifold<PositionNormal>.Revolution(ss, ss, x => float3(1 + x * thickness, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0, 0, -.5f));
            }
            var baseCyl = Manifold<PositionNormal>.Revolution(ss, ss, x => float3(1, 0, x), float3(0,0,1), angle).Transform(Transforms.Translate(0,0,-.5f));
            if (surface)
            {
                return face1;
            }
            return baseCyl + face1 + face2 + baseCylOuter;
        }
    
    }
}
