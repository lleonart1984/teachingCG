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
            float3 diffuse = Diffuse.Sample(TextureSampler, surfel.Coordinates).xyz / pi;
            float3 H = normalize(win + wout);
            float3 specular = Specular * pow(max(0, dot(H, surfel.Normal)), SpecularPower) * (SpecularPower + 2) / two_pi;
            return diffuse * (1 - Glossyness) + specular * Glossyness;
        }

        public void MapCylinder<T1>(Mesh<T1> baseCyl) where T1 : struct, IVertex<T1>, ICoordinatesVertex<T1> // TODO
        {
            for (int i = 0; i < baseCyl.Vertices.Length; i++)
            {
                baseCyl.Vertices[i].Coordinates = float2(5, 5);
            }
        }

        public void MapPlane<T1>(Mesh<T1> face) where T1 : struct, IVertex<T1>, ICoordinatesVertex<T1> // TODO
        {
            for (int i = 0; i < face.Vertices.Length; i++)
            {
                face.Vertices[i].Coordinates = float2(5, 5);
            }
        }
    }

    public struct NoMaterial : IMaterial
    {
        public void MapCylinder<T>(Mesh<T> baseCyl) where T : struct, IVertex<T>, ICoordinatesVertex<T>
        {
        }

        public void MapPlane<T>(Mesh<T> face) where T : struct, IVertex<T>, ICoordinatesVertex<T>
        {
        }
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

}
