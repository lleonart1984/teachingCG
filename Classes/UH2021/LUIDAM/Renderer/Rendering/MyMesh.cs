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

        public static Mesh<V> Expand<V>(this Mesh<V> mesh) where V : struct, INormalVertex<V>
        {
            switch (mesh.Topology)
            {
                case Topology.Points:
                    return mesh;
                case Topology.Lines:
                    throw new NotImplementedException();
                case Topology.Triangles:

                    List<V> vertexes = new List<V>();
                    List<int> indexes = new List<int>();

                    static float3 LinearSystem3x3 (float3 row1, float3 row2, float3 row3, float3 r)
                    {
                        // Solving linear system using Cramer's rule
                        var D = determinant(float3x3(row1, row2, row3));

                        var Dx = determinant(float3x3(float3(r.x, row1.y, row1.z),
                                                      float3(r.y, row2.y, row2.z),
                                                      float3(r.z, row3.y, row3.z)));

                        var Dy = determinant(float3x3(float3(row1.x, r.x, row1.z),
                                                      float3(row2.x,      r.y, row2.z),
                                                      float3(row3.x, r.z , row3.z)));

                        var Dz = determinant(float3x3(float3(row1.x, row1.y, r.x),
                                                      float3(row2.x, row2.y,           r.y),
                                                      float3(row3.x, row3.y, r.z)));
                        return float3(Dx / D, Dy / D, Dz / D);
                    }

                    static float2 LinearSystem2x2(float2 row1, float2 row2, float2 r)
                    {
                        // Solving linear system using Cramer's rule
                        var D = determinant(float2x2(row1, row2));

                        var Dx = determinant(float2x2(float2(r.x, row1.y),
                                                      float2(r.y, row2.y)));

                        var Dy = determinant(float2x2(float2(row1.x, r.x),
                                                      float2(row2.x, r.y)));

                        return float2(Dx / D, Dy / D);
                    }

                    static float3 Bisect(float3 p0, float3 p1, float3 p2)
                    {
                        //var a = normalize(p1 - p0);
                        //var b = normalize(p2 - p0);
                        //var cosAngle = cos(acos(dot(a, b)) / 2);
                        //var normal = cross(a, b);

                        //var bisect = LinearSystem3x3(a, b, normal, float3(cosAngle, cosAngle, 0));

                        var a = normalize(p1 - p0);
                        var b = normalize(p2 - p0);
                        var bisect = b + a;

                        return normalize(bisect);
                    }

                    for (int i = 0; i < mesh.Indices.Length / 3; i++)
                    {
                        var v1 = mesh.Vertices[mesh.Indices[i * 3 + 0]];
                        var v2 = mesh.Vertices[mesh.Indices[i * 3 + 1]];
                        var v3 = mesh.Vertices[mesh.Indices[i * 3 + 2]];
                        
                        var p1 = v1.Position;
                        var p2 = v2.Position;
                        var p3 = v3.Position;

                        var bisect1 = Bisect(p1, p2, p3);
                        var bisect2 = Bisect(p2, p1, p3);

                        var rest = p2 - p1;
                        var inter =  LinearSystem2x2(float2(bisect1.x, -bisect2.x), 
                                                     float2(bisect1.y, -bisect2.y), float2(rest.x, rest.y));

                        float3? intersection = null;

                        if (float.IsFinite(inter.x) && float.IsFinite(inter.y) && 
                            inter.x >= 0 && inter.y >= 0 && 
                            abs(inter.x * bisect1.z - inter.y * bisect2.z - rest.z) <= 0.00001f) // Intersection
                        {
                            intersection = p1 + bisect1 * inter.x;
                        }
                        else
                        {
                            inter = LinearSystem2x2(float2(bisect1.z, -bisect2.z), 
                                                    float2(bisect1.y, -bisect2.y), float2(rest.z, rest.y));

                            if (float.IsFinite(inter.x) && float.IsFinite(inter.y) &&
                                inter.x >= 0 && inter.y >= 0 &&
                                abs(inter.x * bisect1.x - inter.y * bisect2.x - rest.x) <= 0.001f)
                            {
                                intersection = p1 + bisect1 * inter.x;
                            }
                            else
                            {
                                inter = LinearSystem2x2(float2(bisect1.z, -bisect2.z),
                                                             float2(bisect1.x, -bisect2.x), float2(rest.z, rest.x));
                                if (float.IsFinite(inter.x) && float.IsFinite(inter.y) &&
                                    inter.x >= 0 && inter.y >= 0 &&
                                    abs(inter.x * bisect1.y - inter.y * bisect2.y - rest.y) <= 0.001f)
                                {
                                    intersection = p1 + bisect1 * inter.x;
                                }
                            }
                        }

                        if (!intersection.HasValue)
                        {
                            if (length(p1 - p2) <= 0.00001f) // p1 == p2 => Middle Point
                            {
                                intersection = (p3 + p2) / 2;
                            }
                            else if (length(p3 - p2) <= 0.00001f) // p3 == p2 => Middle Point
                            {
                                intersection = (p1 + p2) / 2;
                            }
                            else if (length(p3 - p1) <= 0.00001f) // p3 == p1 => Middle Point
                            {
                                intersection = (p2 + p1) / 2;
                            }
                            else
                                throw new InvalidOperationException("Interection failed");
                        }

                        var barycenter = intersection.Value;

                        var v4 = new V { Position = barycenter, Normal = normalize(v1.Normal + v2.Normal + v3.Normal)};

                        indexes.AddRange(new int[]
                        {
                            vertexes.Count + 0,
                            vertexes.Count + 3,
                            vertexes.Count + 1,

                            vertexes.Count + 0,
                            vertexes.Count + 3,
                            vertexes.Count + 2,

                            vertexes.Count + 1,
                            vertexes.Count + 3,
                            vertexes.Count + 2,
                        });

                        vertexes.AddRange(new V[]
                        {
                            v1,
                            v2,
                            v3,
                            v4,
                        });

                    }
                    mesh = new Mesh<V>(vertexes.ToArray(), indexes.ToArray());
                    return mesh;
                default:
                    throw new NotImplementedException();
            }
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
        /// Applies the given normal to all vertex in Mesh
        /// </summary>
        /// <param name="normal"></param>
        public static void SetNormal<V>(this Mesh<V> mesh, float3 normal) where V : struct, IVertex<V>
        {
            mesh.NormalVertex = new float3[] { normal };
            mesh.NormalSeparators = new int[] { mesh.Vertices.Length };
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
            if (vertexIndex < mesh.MaterialsSeparators[0])
            {
                return mesh.Materials[0];
            }
            for (int i = 1; i < mesh.MaterialsSeparators.Length; i++)
            {
                if (mesh.MaterialsSeparators[i-1] <= vertexIndex && vertexIndex < mesh.MaterialsSeparators[i])
                    return mesh.Materials[i];
            }
            throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Get the vertex normal corresponding to the vertex in <paramref name="vertexIndex"/>
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="mesh"></param>
        /// <param name="vertexIndex"></param>
        /// <returns></returns>
        public static float3 GetVertexNormal<V>(this Mesh<V> mesh, int vertexIndex) where V : struct, IVertex<V>
        {
            if (vertexIndex < mesh.NormalSeparators[0])
            {
                return mesh.NormalVertex[0];
            }
            for (int i = 1; i < mesh.NormalSeparators.Length; i++)
            {
                if (mesh.NormalSeparators[i - 1] <= vertexIndex && vertexIndex < mesh.NormalSeparators[i])
                    return mesh.NormalVertex[i];
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
                var newMesh = new Mesh<V>(vertex, indexes, mesh.Materials[i], default);

                var normal = new List<float3>();
                var normalIndexes = new List<int>();
                for (int j = initVertexIndex; j < initVertexIndex + vertexAmount; j++)
                {
                    var currNormal = mesh.GetVertexNormal(j);
                    if (normal.Any())
                    {
                        if (any(normal[normal.Count - 1] != currNormal))
                        {
                            normalIndexes.Append(j - initVertexIndex);
                            normal.Append(currNormal);
                        }
                    }
                    else
                    {
                        normal.Add(currNormal);
                    }
                }
                normalIndexes.Add(vertexAmount);
                newMesh.NormalVertex = normal.ToArray();
                newMesh.NormalSeparators = normalIndexes.ToArray();

                meshMaterials.Add(newMesh);
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
