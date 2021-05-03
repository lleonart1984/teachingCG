using System;
using System.Collections.Generic;
using System.Text;
using static GMath.Gfx;
using GMath;
using Rendering;
using static Renderer.Program;

namespace Renderer.Modeling
{
    public static class MeshShapeGenerator<T> where T : struct, IVertex<T>
    {
        public static Mesh<T> Box(int width, int height, int deep, bool faceXYUp = true, bool faceXYDown = true, bool faceXZUp = true, bool faceXZDown = true, bool faceYZUp = true, bool faceYZDown = true
                                                                              , bool holeXYUp = false, bool holeXYDown = false, bool holeXZUp = false, bool holeXZDown = false, bool holeYZUp = false, bool holeYZDown = false
                                                                              , float4? sepXYUp = null, float4? sepXYDown = null, float4? sepXZUp = null, float4? sepXZDown = null, float4? sepYZUp = null, float4? sepYZDown = null)
        {
            var box = new Mesh<T>(new T[] { }, new int[] { });
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
                Mesh<T> face = null;
                int stacks = 0, slices = 0;
                if (dirX.x != 0)
                {
                    stacks = width;
                    slices = (int)Math.Max(dirY.y * height, dirY.z * deep);
                    if (dirY.y != 0)
                    {
                        if (length(trans) == 0)
                        {
                            if (!faceXYDown)
                                continue;
                            if (holeXYDown)
                            {
                                face = MyManifold<T>.MiddleHoleSurface(slices, stacks, sepXYDown.Value);
                            }
                        }
                        else if (!faceXYUp)
                            continue;
                        else if (holeXYUp)
                        {
                            face = MyManifold<T>.MiddleHoleSurface(slices, stacks, sepXYUp.Value);
                        }
                    }
                    else if (dirY.z != 0)
                    {
                        if (length(trans) == 0)
                        {
                            if (!faceXZDown)
                                continue;
                            if (holeXZDown)
                            {
                                face = MyManifold<T>.MiddleHoleSurface(slices, stacks, sepXZDown.Value).ApplyTransforms(
                            Transforms.RotateX(- pi / 2));
                            }
                        }
                        else if (!faceXZUp)
                            continue;
                        else if (holeXZUp)
                        {
                            face = MyManifold<T>.MiddleHoleSurface(slices, stacks, sepXZUp.Value).ApplyTransforms(
                            Transforms.RotateX(- pi / 2));
                        }
                    }
                }
                else
                {
                    stacks = height;
                    slices = deep;
                    if (length(trans) == 0)
                    {
                        if (!faceYZDown)
                            continue;
                        if (holeYZDown)
                        {
                            face = MyManifold<T>.MiddleHoleSurface(slices, stacks, sepYZDown.Value).ApplyTransforms(
                            Transforms.RotateY(pi / 2));
                        }
                    }
                    else if (!faceYZUp)
                        continue;
                    else if (holeYZUp)
                    {
                        face = MyManifold<T>.MiddleHoleSurface(slices, stacks, sepYZUp.Value).ApplyTransforms(
                            Transforms.RotateY(pi/2));
                    }
                }
                if (face == null)
                    face = Manifold<T>.Surface(stacks, slices, (x, y) => float3(dirX.x * x + trans.x, dirX.y * x + (dirX.y == 0 ? dirY.y * y : 0) + trans.y, dirY.z * y + trans.z));
                else
                {
                    face = face.ApplyTransforms(Transforms.Translate(trans));
                }
                box += face;
            }
            return box.Transform(Transforms.Translate(-.5f, -.5f, -.5f));
        }


        public static Mesh<T> Box(int points)
        {
            return Box(points / 3, points / 3, points / 3);
        }

        public static Mesh<T> Cylinder(int points, float thickness=0, float angle = 2 * pi, bool surface = false)
        {
            var ss = (int)ceil(sqrt(points));
            var baseCylOuter = new Mesh<T>();
            var face1 = MyManifold<T>.Revolution(ss, ss, x => float3(1 * x, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0,0,.5f));
            var face2 = MyManifold<T>.Revolution(ss, ss, x => float3(1 * x, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0,0,-.5f));
            
            if (thickness != 0)
            {
                baseCylOuter = MyManifold<T>.Revolution(ss, ss, x => float3(1 + thickness, 0, x), float3(0, 0, 1), angle).Transform(Transforms.Translate(0, 0, -.5f));
                face1 = MyManifold<T>.Revolution(ss, ss, x => float3(1 + x * thickness, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0, 0, .5f));
                face2 = MyManifold<T>.Revolution(ss, ss, x => float3(1 + x * thickness, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0, 0, -.5f));
            }
            var baseCyl = MyManifold<T>.Revolution(ss, ss, x => float3(1, 0, x), float3(0,0,1), angle).Transform(Transforms.Translate(0,0,-.5f));
            if (surface)
            {
                return face1;
            }
            return baseCyl + face1 + face2 + baseCylOuter;
        }
    
    }
}
