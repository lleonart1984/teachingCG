using System;
using System.Collections.Generic;
using System.Text;
using static GMath.Gfx;
using GMath;
using Rendering;
using static Renderer.Program;

namespace Renderer.Modeling
{
    public static class MeshShapeGenerator<T> where T : struct, IVertex<MyVertex>
    {
        public static Mesh<MyVertex> Box(int points, bool withFace=true)
        {
            var stacks = (int)ceil(sqrt(points / (float)6.0));
            var box = new Mesh<MyVertex>(new MyVertex[] { }, new int[] { });
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
                var face = Manifold<MyVertex>.Surface(stacks, stacks, (x, y) => float3(dirX.x * x + trans.x, dirX.y * x + (dirX.y == 0 ? dirY.y * y : 0) + trans.y, dirY.z * y + trans.z));
                box += face;
            }
            return box.Transform(Transforms.Translate(-.5f, -.5f, -.5f));
        }

        public static Mesh<MyVertex> Cylinder(int points, float thickness=0, float angle = 2 * pi)
        {
            var ss = (int)ceil(sqrt(points));
            var baseCylOuter = new Mesh<MyVertex>();
            var face1 = Manifold<MyVertex>.Revolution(ss, ss, x => float3(.5f * x, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0,0,.5f));
            var face2 = Manifold<MyVertex>.Revolution(ss, ss, x => float3(.5f * x, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0,0,-.5f));
            
            if (thickness != 0)
            {
                baseCylOuter = Manifold<MyVertex>.Revolution(ss, ss, x => float3(.5f + thickness, 0, x), float3(0, 0, 1), angle).Transform(Transforms.Translate(0, 0, -.5f));
                face1 = Manifold<MyVertex>.Revolution(ss, ss, x => float3(.5f + x * thickness, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0, 0, .5f));
                face2 = Manifold<MyVertex>.Revolution(ss, ss, x => float3(.5f + x * thickness, 0, 0), float3(0, 0, 1), angle).Transform(Transforms.Translate(0, 0, -.5f));
            }
            var baseCyl = Manifold<MyVertex>.Revolution(ss, ss, x => float3(.5f, 0, x), float3(0,0,1), angle).Transform(Transforms.Translate(0,0,-.5f));
            return baseCyl + face1 + face2 + baseCylOuter;
        }
    
    }
}
