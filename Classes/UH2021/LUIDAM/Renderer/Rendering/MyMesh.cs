using GMath;
using static GMath.Gfx;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Renderer;

namespace Rendering
{
    public static class MyMeshTools
    {

        public static Mesh<V> ApplyTransforms<V>(this Mesh<V> mesh, params float4x4[] transforms) where V : struct, IVertex<V>
        {
            var id = Transforms.Identity;
            foreach (var item in transforms)
            {
                id = mul(id, item);
            }
            return mesh.Transform(id);
        }

        /// <summary>
        /// Returs a model with the points between (0,0,0) <= (x,y,z) <= (wwidth, height, deep)
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="deep"></param>
        /// <returns></returns>
        public static Mesh<V> FitIn<V>(this Mesh<V> mesh, float width, float height, float deep) where V : struct, INormalVertex<V>
        {
            var s = MyTransforms.FitIn(mesh.BoundBox.oppositeCorner, mesh.BoundBox.topCorner, width, height, deep);
            return mesh.Transform(s);
        }

        /// <summary>
        /// Applies the given material to all vertex in Mesh
        /// </summary>
        /// <param name="material"></param>
        public static void SetMaterial<V>(this Mesh<V> mesh, IMaterial material) where V : struct, IVertex<V>
        {
            mesh.Materials = new IMaterial[] { material };
            mesh.MaterialsSeparators = new int[] { mesh.Vertices.Length };
        }

        /// <summary>
        /// Get the material corresponding to the vertex in <paramref name="vertexIndex"/>
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="mesh"></param>
        /// <param name="vertexIndex"></param>
        /// <returns></returns>
        public static IMaterial GetVertexMaterial<V>(this Mesh<V> mesh, int vertexIndex) where V : struct, IVertex<V>
        {
            for (int i = 0; i < mesh.MaterialsSeparators.Length; i++)
            {
                if (mesh.MaterialsSeparators[i] <= vertexIndex)
                    return mesh.Materials[i];
            }
            throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Extract submeshes from <paramref name="mesh"/> that has the same materials. 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static IEnumerable<Mesh<V>> MaterialDecompose<V>(this Mesh<V> mesh) where V : struct, IVertex<V>
        {
            List<Mesh<V>> meshMaterials = new List<Mesh<V>>();
            int initVertexIndex = 0;
            int initIndex = 0;
            int topologyIndexPerVertex = mesh.Topology == Topology.Triangles ? 3 : mesh.Topology == Topology.Lines ? 2 : mesh.Topology == Topology.Points ? 1 : throw new NotImplementedException(); 
            for (int i = 0; i < mesh.MaterialsSeparators.Length; i++)
            {
                var lastMaterialIndex = mesh.MaterialsSeparators[i];
                var indexAmount = mesh.Indices.Where(x => initVertexIndex <= x && x < lastMaterialIndex).Count();
                var vertexAmount = lastMaterialIndex - initVertexIndex;
                var indexes = new int[indexAmount];
                var vertex = new V[vertexAmount];

                Array.Copy(mesh.Indices, initIndex, indexes, 0, indexes.Length);
                Array.Copy(mesh.Vertices, initVertexIndex, vertex, 0, vertex.Length);
                indexes = indexes.Select(x => x - initVertexIndex).ToArray();
                meshMaterials.Add(new Mesh<V>(vertex, indexes, mesh.Materials[i]));
                initVertexIndex += vertexAmount;
                initIndex += indexAmount;
            }
            return meshMaterials;
        }
    }

    public class MyManifold<V> where V : struct, IVertex<V>
    {
        /// <summary>
        /// Builds a surface in xy plane with a hole
        /// Boundries are (0,0,0) and (1,1,0)
        /// </summary>
        /// <param name="slices"></param>
        /// <param name="stacks"></param>
        /// <param name="separation">starting left in clockwise, separation of the hole from the surface borders</param>
        /// <returns></returns>
        public static Mesh<V> MiddleHoleSurface(int slices, int stacks, float4 separation)
        {
            static float3 meshGenerator(float x, float y, float z, float3 center, float radius, float3 centerDir)
            {
                var p = float3(x, y, z);
                var d = p - center;
                var lengthD = length(d);
                if (lengthD <= .00001f)
                {
                    d = p + .001f * normalize(centerDir) - center;
                }
                if (lengthD <= radius) // Inside sphere
                {
                    // a,b,c Components of the parametric substitution of ray(center,d) in the sphere equation
                    var sphereA = pow(d.x, 2) + pow(d.y, 2) + pow(d.z, 2);
                    var alpha = radius * sqrt(1 / sphereA);
                    var intersection = center + alpha * d;
                    p = intersection;
                }
                return p;
            }

            var radius = 1f;
            var center = float3(1, 0, 0);
            var radiusFixScale = .8f; // To fix near center points issue 
            var holeXScale = 1 - separation.x - separation.z;
            var holeYScale = 1 - separation.y - separation.w;


            var model = Manifold<V>.Surface(slices / 2, stacks / 2,
                (x, y) => meshGenerator(2 * x, -1 + y * radiusFixScale, 0, center, radius, float3(0, -1, 0)))
                .Transform(Transforms.Translate((1 - radiusFixScale) * float3(0, 1, 0)));

            model += Manifold<V>.Surface(slices / 2, stacks / 2,
                (x, y) => meshGenerator(2 * x, 1 - y * radiusFixScale, 0, center, radius, float3(0, 1, 0)))
                .Transform(Transforms.Translate((1 - radiusFixScale) * float3(0, -1, 0)));

            model = model.ApplyTransforms(MyTransforms.FitIn(model.BoundBox.oppositeCorner, model.BoundBox.topCorner, 1, 1, 1));
            model = model.ApplyTransforms(Transforms.Scale(1 / (model.BoundBox.topCorner.x == 0 ? 1 : model.BoundBox.topCorner.x),
                                                           1 / (model.BoundBox.topCorner.y == 0 ? 1 : model.BoundBox.topCorner.y),
                                                           1 / (model.BoundBox.topCorner.z == 0 ? 1 : model.BoundBox.topCorner.z)));
            model = model.ApplyTransforms(MyTransforms.FitIn(model.BoundBox.oppositeCorner, model.BoundBox.topCorner, 1 - separation.x - separation.z, 1 - separation.y - separation.w, 1));
            model = model.ApplyTransforms(Transforms.Translate(separation.x, separation.w, 0));
            var l = model.BoundBox.oppositeCorner;
            var h = model.BoundBox.topCorner;

            var upSurface = Manifold<V>.Surface((int)ceil(slices * holeXScale), (int)ceil(stacks * separation.y),
                (x, y) => float3(l.x + x * (1 - separation.x - separation.z), h.y + y * separation.y, 0));

            var downSurface = Manifold<V>.Surface((int)ceil(slices * holeXScale), (int)ceil(stacks * separation.w),
                (x, y) => float3(l.x + x * (1 - separation.x - separation.z), y * separation.w, 0));

            var leftSurface = Manifold<V>.Surface((int)ceil(slices * separation.x), (int)ceil(stacks * holeYScale),
                (x, y) => float3(x * separation.x, l.y + y * (1 - separation.w - separation.y), 0));

            var rightSurface = Manifold<V>.Surface((int)ceil(slices * separation.z), (int)ceil(stacks * holeYScale),
                (x, y) => float3(h.x + x * separation.z, l.y + y * (1 - separation.w - separation.y), 0));

            var upLeftSurface = Manifold<V>.Surface((int)ceil(slices * separation.x), (int)ceil(stacks * separation.y),
                (x, y) => float3(x * separation.x, h.y + y * separation.y, 0));

            var upRightSurface = Manifold<V>.Surface((int)ceil(slices * separation.z), (int)ceil(stacks * separation.y),
                (x, y) => float3(h.x + x * separation.z, h.y + y * separation.y, 0));

            var downLeftSurface = Manifold<V>.Surface((int)ceil(slices * separation.x), (int)ceil(stacks * separation.w),
                (x, y) => float3(x * separation.x, y * separation.w, 0));

            var downRightSurface = Manifold<V>.Surface((int)ceil(slices * separation.z), (int)ceil(stacks * separation.w),
                (x, y) => float3(h.x + x * separation.z, y * separation.w, 0));

            var surface = upSurface + downSurface + leftSurface + rightSurface + upLeftSurface + upRightSurface + downLeftSurface + downRightSurface;
            return model + surface;
        }

        public static Mesh<V> Revolution(int slices, int stacks, Func<float, float3> g, float3 axis, float angle = 2 * pi)
        {
            return Manifold<V>.Generative(slices, stacks, g, (v, t) => mul(float4(v, 1), Transforms.Rotate(t * angle, axis)).xyz);
        }

    }
}
