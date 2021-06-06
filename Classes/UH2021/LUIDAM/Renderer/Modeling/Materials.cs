using GMath;
using static GMath.Gfx;
using Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using static Renderer.Program;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Linq;
using Renderer.Modeling;

namespace Renderer.Modeling
{
    public static class Materials<T> where T : struct, INormalVertex<T>, ICoordinatesVertex<T>
    {
        public static MyMaterial<T> FrontMainGuitarBodyMaterial = GetFrontMainGuitarBodyMaterial();
        public static MyMaterial<T> BackMainGuitarBodyMaterial = GetBackMainGuitarBodyMaterial();
        public static MyMaterial<T> GuitarBodyHoleMaterial = GetGuitarBodyHoleMaterial();
        public static MyMaterial<T> BasePinMaterial = GetBasePinMaterial();
        public static MyMaterial<T> BasePinHeadMaterial = GetBasePinHeadMaterial();
        public static MyMaterial<T> MetalStringMaterial = GetMetalStringMaterial();
        public static MyMaterial<T> NylonStringMaterial = GetNylonStringMaterial();
        public static MyMaterial<T> WallMaterial = GetWallMaterial();
        public static MyMaterial<T> FloorMaterial = GetFloorMaterial();


        public static MyMaterial<T> GetFrontMainGuitarBodyMaterial()
        {
            return LoadMaterialFromFile("textures\\guitar_texture.bmp", 0.01f, 60, 0.07f, 0, specular: float3(1, 1, 1) * .5f);
        }

        public static MyMaterial<T> GetBackMainGuitarBodyMaterial()
        {
            return LoadMaterialFromFile("textures\\headstock_texture.bmp", 0.01f, 60, 0.04f, 0, specular: float3(1, 1, 1) * .5f);
        }

        public static MyMaterial<T> GetGuitarBodyHoleMaterial()
        {
            return LoadMaterialFromFile("textures\\circle_texture.bmp", 0.01f, 60, 0.01f, 0);
        }

        public static MyMaterial<T> GetBasePinMaterial()
        {
            return LoadMaterialFromFile("textures\\pin_texture.bmp", 0.04f, 60, 0, 0);
        }

        public static MyMaterial<T> GetBasePinHeadMaterial()
        {
            return LoadMaterialFromFile("textures\\pin_head_texture.bmp", 0.02f, 60, 0, 0);
        }

        public static MyMaterial<T> GetMetalStringMaterial()
        {
            return LoadMaterialFromFile("textures\\pin_texture.bmp", 0.04f, 60, 0, 0, bumpDir: "textures\\string_bump.bmp");
        }

        public static MyMaterial<T> GetNylonStringMaterial()
        {
            return LoadMaterialFromFile("textures\\pin_head_texture.bmp", 0.04f, 200, .7f, 0, diffuseWeight: 0.4f, refraction: 1.6f);
        }

        public static MyMaterial<T> GetWallMaterial()
        {
            return LoadMaterialFromFile("textures\\wall_texture.bmp", 0f, 260, 0, 0);
        }

        public static MyMaterial<T> GetFloorMaterial()
        {
            return LoadMaterialFromFile("textures\\floor_texture.bmp", 0f, 260, 0, 0);
        }

        public static MyMaterial<T> LoadMaterialFromFile(string diffuseDir, float glossyness, float specularPower, float fresnel, float mirror, float diffuseWeight = 1.0f, float refraction = 1.0f, string bumpDir = null, float3? specular = default, float3? diffuse = default)
        {
            var item = Texture2D.LoadBmpFromFile(diffuseDir);
            Texture2D bump = null;
            if (bumpDir != null)
                bump = Texture2D.LoadBmpFromFile(bumpDir);
            var realSpecular = specular.HasValue ? specular.Value : float3(1, 1, 1);
            var realDifusse = diffuse.HasValue ? diffuse.Value : float3(1, 1, 1);
            return new MyMaterial<T>
            {
                DiffuseMap = item,
                BumpMap = bump,
                WeightGlossy = glossyness,
                SpecularPower = specularPower,
                Specular = realSpecular,
                Diffuse = realDifusse,
                WeightDiffuse = diffuseWeight,
                WeightMirror = mirror,
                WeightFresnel = fresnel,
                RefractionIndex = refraction,
                TextureSampler = new Sampler
                {
                    Wrap = WrapMode.Repeat,
                },
            };
        }

        /// <summary>
        /// Create a noisy bump texture
        /// </summary>
        /// <param name="file">Image file</param>
        /// <param name="noiseScalar">Noise presence, between 0 and 1</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        public static void CreateNoisyBumpMap(string file, float noiseScalar, int width, int height)
        {
            var bmp = new Bitmap(width, height);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    bmp.SetPixel(i, j, Color.FromArgb(255, (int)(127.5 + 127.5 * noiseScalar * random()), (int)(127.5 + 127.5 * noiseScalar * random()), (int)(127.5 + 127.5 * noiseScalar * random())));
                }
            }
            bmp.Save(file);
        }

        /// <summary>
        /// Create a sine like bump texture
        /// </summary>
        /// <param name="file"></param>
        /// <param name="bumpScatterScalar">Separation between peaks</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        public static void CreateRoughStringBumpMap(string file, int bumpScatterScalar, int width, int height)
        {
            var bmp = new Bitmap(width, height);
            for (int i = 0; i < width; i++)
            {
                float3 value = abs(float3(0, sin(pi * i / bumpScatterScalar), 0));
                for (int j = 0; j < height; j++)
                {
                    bmp.SetPixel(i, j, Color.FromArgb(255, (int)(127.5 + 127.5 * value.x), (int)(127.5 + 127.5 * value.y), (int)(127.5 + 127.5 * value.z)));
                }
            }
            bmp.Save(file);
        }

    }
}
