using GMath;
using static GMath.Gfx;
using Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Renderer
{
    #region Vertex

    public struct MyPositionNormalCoordinate : INormalVertex<MyPositionNormalCoordinate>, ICoordinatesVertex<MyPositionNormalCoordinate>, ITransformable<MyPositionNormalCoordinate>, IColorable
    {
        public float3 Position { get; set; }

        public float3 Normal { get; set; }

        public float2 Coordinates { get; set; }

        public float3 Color { get; set; }

        public MyPositionNormalCoordinate Add(MyPositionNormalCoordinate other)
        {
            return new MyPositionNormalCoordinate
            {
                Position = this.Position + other.Position,
                Normal = this.Normal + other.Normal,
                Coordinates = this.Coordinates + other.Coordinates,
                Color = Color
            };
        }

        public MyPositionNormalCoordinate Mul(float s)
        {
            return new MyPositionNormalCoordinate
            {
                Position = this.Position * s,
                Normal = this.Normal * s,
                Coordinates = this.Coordinates * s,
                Color = Color
            };
        }

        public MyPositionNormalCoordinate Transform(float4x4 matrix)
        {
            float4 p = float4(Position, 1);
            p = mul(p, matrix);

            float4 n = float4(Normal, 0);
            n = mul(n, matrix);

            return new MyPositionNormalCoordinate
            {
                Position = p.xyz / p.w,
                Normal = n.xyz,
                Coordinates = Coordinates,
                Color = Color
            };
        }
    }

    #endregion

    #region Materials

    public struct MyMaterial<T> : IMaterial where T : struct, INormalVertex<T>, ICoordinatesVertex<T> 
    {
        public Texture2D Diffuse;

        public float3 Specular;

        public float SpecularPower;

        public float Glossyness;

        public Sampler TextureSampler;

        public float3 EvalBRDF(T surfel, float3 wout, float3 win)
        {
            if (Diffuse == null) // TODO Remove ONLY FOR TESTING
            {
                Diffuse = new Texture2D(2, 2);
                Diffuse.Write(0, 0, float4(1, 0, 0, 1));
                Diffuse.Write(0, 1, float4(0, 1, 0, 1));
                Diffuse.Write(1, 0, float4(0, 0, 1, 1));
                Diffuse.Write(1, 1, float4(0, 1, 1, 1));
                TextureSampler = new Sampler { Wrap = WrapMode.Repeat };
                //var c = GuitarDrawer<MyPositionNormalCoordinate>.LoadMaterialFromFile("guitar_texture.material", 32, 0.9f);
                //Diffuse = c.Diffuse;
                //Glossyness = c.Glossyness;
                //TextureSampler = c.TextureSampler;
                //Specular = c.Specular;
                //SpecularPower = c.SpecularPower;
            }
            float3 diffuse = Diffuse.Sample(TextureSampler, surfel.Coordinates).xyz / pi;
            float3 H = normalize(win + wout);
            float3 specular = Specular * pow(max(0, dot(H, surfel.Normal)), SpecularPower) * (SpecularPower + 2) / two_pi;
            return diffuse * (1 - Glossyness) + specular * Glossyness;
        }

    }

    public struct NoMaterial : IMaterial
    {
    }

    #endregion

    #region RayPayloads

    public struct DefaultRayPayload
    {
        public float3 Color;
    }

    public struct MyShadowRayPayload
    {
        public bool Shadowed;
    }

    #endregion

    #region Utils

    public static class MaterialsUtils
    {
        /// <summary>
        /// Maps the Mesh vertexes representing a truncated plane in its original axis without transformations to the material
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="face"></param>
        public static Mesh<T> MapPlane<T>(Mesh<T> face) where T : struct, IVertex<T>, ICoordinatesVertex<T> // TODO
        {
            // FOR TESTING ONLY 
            var ret = face.Clone();
            var clone = face.Clone();
            if (Math.Abs(clone.BoundBox.topCorner.x - clone.BoundBox.oppositeCorner.x) < 0.00001)
            {
                clone = clone.Transform(Transforms.RotateY(pi / 2));
            }
            else if (Math.Abs(clone.BoundBox.topCorner.y - clone.BoundBox.oppositeCorner.y) < 0.00001)
            {
                clone = clone.Transform(Transforms.RotateX(pi / 2));
            }
            clone = clone.Transform(MyTransforms.ExpandInto(clone.BoundBox.oppositeCorner, clone.BoundBox.topCorner, 1.0f, 1.0f, 1.0f));
            for (int i = 0; i < clone.Vertices.Length; i++)
            {
                ret.Vertices[i].Coordinates = float2(Math.Abs(clone.Vertices[i].Position.x), Math.Abs(clone.Vertices[i].Position.y));
            }
            return ret;
        }

        /// <summary>
        /// Maps the Mesh vertexes representing a truncated cylinder to the material
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="face"></param>
        public static Mesh<T> MapCylinderCoordinates<T>(Mesh<T> baseCyl) where T : struct, IVertex<T>, ICoordinatesVertex<T> // TODO
        {
            // FOR TESTING ONLY 
            var clone = baseCyl.Clone();
            for (int i = 0; i < clone.Vertices.Length; i++)
            {
                clone.Vertices[i].Coordinates = float2(clone.Vertices[i].Position.x, clone.Vertices[i].Position.y);
            }
            return clone;
        }
    }

    #endregion

}
