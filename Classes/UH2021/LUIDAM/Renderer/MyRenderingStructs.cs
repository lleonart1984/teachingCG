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
        private float3? _color;
        public float3 Color { get => _color.HasValue ? _color.Value : float3(1,1,1); set => _color = value; }

        public float3 Emissive;

        public Texture2D DiffuseMap;
        public Texture2D BumpMap;
        public Sampler TextureSampler;

        public float3 Diffuse;
        public float3 Specular;
        public float SpecularPower;
        public float RefractionIndex;

        // 4 float values with Diffuseness, Glossyness, Mirrorness, Fresnelness
        public float WeightDiffuse { get { return 1 - OneMinusWeightDiffuse; } set { OneMinusWeightDiffuse = 1 - value; } }
        float OneMinusWeightDiffuse; // This is intended for default values of the struct to work as 1, 0, 0, 0 weight initial settings
        public float WeightGlossy;
        public float WeightMirror;
        public float WeightFresnel;

        public float WeightNormalization
        {
            get { return max(0.0001f, WeightDiffuse + WeightGlossy + WeightMirror + WeightFresnel); }
        }

        public float3 EvalBRDF(T surfel, float3 wout, float3 win)
        {
            float3 diffuse = Diffuse * (DiffuseMap == null ? Color : DiffuseMap.Sample(TextureSampler, surfel.Coordinates).xyz) / pi;
            float3 H = normalize(win + wout);
            float3 specular = Specular * pow(max(0, dot(H, surfel.Normal)), SpecularPower) * (SpecularPower + 2) / two_pi;
            return diffuse * WeightDiffuse / WeightNormalization + specular * WeightGlossy / WeightNormalization;
        }

        // Compute fresnel reflection component given the cosine of input direction and refraction index ratio.
        // Refraction can be obtained subtracting to one.
        // Uses the Schlick's approximation
        float ComputeFresnel(float NdotL, float ratio)
        {
            float f = pow((1 - ratio) / (1 + ratio), 2);
            return (f + (1.0f - f) * pow((1.0f - NdotL), 5));
        }

        public IEnumerable<MyImpulse> GetBRDFImpulses(T surfel, float3 wout)
        {
            if (!any(Specular))
                yield break; // No specular => Ratio == 0

            float NdotL = dot(surfel.Normal, wout);
            // Check if ray is entering the medium or leaving
            bool entering = NdotL > 0;

            // Invert all data if leaving
            NdotL = entering ? NdotL : -NdotL;
            surfel.Normal = entering ? surfel.Normal : -surfel.Normal;
            float ratio = entering ? 1.0f / this.RefractionIndex : this.RefractionIndex / 1.0f; // 1.0f air refraction index approx

            // Reflection vector
            float3 R = reflect(wout, surfel.Normal);

            // Refraction vector
            float3 T = refract(wout, surfel.Normal, ratio);

            // Reflection quantity, (1 - F) will be the refracted quantity.
            float F = ComputeFresnel(NdotL, ratio);

            if (!any(T))
                F = 1; // total internal reflection (produced with critical angles)

            if (WeightMirror + WeightFresnel * F > 0) // something is reflected
                yield return new MyImpulse
                {
                    Direction = R,
                    Ratio = Specular * (WeightMirror + WeightFresnel * F) / WeightNormalization
                };

            if (WeightFresnel * (1 - F) > 0) // something to refract
                yield return new MyImpulse
                {
                    Direction = T,
                    Ratio = Specular * WeightFresnel * (1 - F) / WeightNormalization
                };
        }

        /// <summary>
        /// Scatter a ray using the BRDF and Impulses
        /// </summary>
        public MyScatteredRay Scatter(T surfel, float3 w)
        {
            float selection = random();
            float impulseProb = 0;

            foreach (var impulse in GetBRDFImpulses(surfel, w))
            {
                float pdf = (impulse.Ratio.x + impulse.Ratio.y + impulse.Ratio.z) / 3;
                if (selection < pdf) // this impulse is choosen
                    return new MyScatteredRay
                    {
                        Ratio = impulse.Ratio,
                        Direction = impulse.Direction,
                        PDF = pdf
                    };
                selection -= pdf;
                impulseProb += pdf;
            }

            float3 wout = randomHSDirection(surfel.Normal);
            /// BRDF uniform sampling
            return new MyScatteredRay
            {
                Direction = wout,
                Ratio = EvalBRDF(surfel, wout, w),
                PDF = (1 - impulseProb) / (2 * pi)
            };
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

    public struct MyRTRayPayload
    {
        public float3 Color;
        public int Bounces; // Maximum value of allowed bounces
    }

    public struct MyPTRayPayload
    {
        public float3 Color; // Accumulated color to the viewer
        public float3 Importance; // Importance of the ray to the viewer
        public int Bounces; // Maximum value of allowed bounces
    }

    #endregion

    #region Rays

    public struct MyImpulse
    {
        public float3 Direction;
        public float3 Ratio;
    }

    public struct MyScatteredRay
    {
        public float3 Direction;
        public float3 Ratio;
        public float PDF;
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
