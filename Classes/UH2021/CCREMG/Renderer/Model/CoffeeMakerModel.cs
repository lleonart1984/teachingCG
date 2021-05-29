using GMath;
using Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using static GMath.Gfx;

namespace Renderer
{
    public class CoffeeMakerModel<V> where V: struct, ICoordinatesVertex<V>
    {
        private static int sides = 10;
        private static float h_base = 3;
        private static float altura_base = 0;
        private static float h_union = 0.5f;
        private static float altura_union = altura_base + h_base;
        private static float h_tope = 3;
        private static float altura_tope = altura_union + h_union;
        private static float h_tapa = 0.3f;
        private static float altura_tapa = altura_tope + h_tope;
        private static float h_cosita = 1f;
        private static float altura_cosita = altura_tapa + h_tapa;
        
        public Mesh<V> GetPlasticMesh()
        {
            List<float3> handlePoints = AsaXZ(float3(0, altura_tapa, 1.7f), h_tope, h_tope/2, h_union);
            List<float3> handlePoints1 = new List<float3>();
            List<float3> handlePoints2 = new List<float3>();
            for(int i = 0; i < handlePoints.Count; i++)
            {
                if(i < handlePoints.Count/2)
                    handlePoints1.Add(handlePoints[i]);
                else
                    handlePoints2.Add(handlePoints[i]);
            }
            Mesh<V> handle_mesh = CoffeMakerSection_Mesh(handlePoints2, handlePoints1);

            handle_mesh = handle_mesh.Add_Mesh(AsaLateralMesh(handlePoints1, 0));
            handle_mesh = handle_mesh.Add_Mesh(AsaLateralMesh(handlePoints2, 1));

            List<float3> buttonCositaPoints = PoliedroXZ(sides, float3(0, altura_cosita, 0), 0.3f);
            List<float3> topCositaPoints = PoliedroXZ(sides, float3(0, altura_cosita + h_cosita, 0), 0.4f);
            Mesh<V> cosita_mesh = CoffeMakerSection_Mesh(buttonCositaPoints, topCositaPoints);
            Mesh<V> top_cosita_mesh = Mesh_Poliedro(topCositaPoints, float3(0, altura_cosita + h_cosita, 0), 0);
            cosita_mesh = cosita_mesh.Add_Mesh(top_cosita_mesh);

            Mesh<V> up_mesh = handle_mesh.Add_Mesh(cosita_mesh);
            up_mesh = up_mesh.Transform(Transforms.RotateRespectTo(float3(0,0,0), float3(0,1,0), pi/3 + pi/10));

            Mesh<V> r_model = up_mesh.Bigger_Mesh();

            return r_model;
        }


        public Mesh<V> GetMetalMesh()
        {
            List<float3> buttonBasePoints = PoliedroXZ(sides, float3(0, 0, 0), 2);
            List<float3> topBasePoints = PoliedroXZ(sides, float3(0, altura_base + h_base, 0), 1.3f);
            Mesh<V> coffee_base_mesh = CoffeMakerSection_Mesh(buttonBasePoints, topBasePoints);
            Mesh<V> base_mesh = Mesh_Poliedro(buttonBasePoints, float3(0, 0, 0), 1);

            List<float3> buttonUnionPoints = PoliedroXZ(sides * 10, float3(0, altura_union, 0), 1.35f);
            List<float3> topUnionPoints = PoliedroXZ(sides * 10, float3(0, altura_union + h_union, 0), 1.35f);
            Mesh<V> union_mesh = CoffeMakerSection_Mesh(buttonUnionPoints, topUnionPoints);

            List<float3> buttonTopPoints = PoliedroXZ(sides, float3(0, altura_tope, 0), 1.4f);
            List<float3> topTopPoints = PoliedroXZ(sides, float3(0, altura_tope + h_tope, 0), 2.1f);
            Mesh<V> top_mesh = CoffeMakerTopSection_Mesh(buttonTopPoints, topTopPoints, 2.1f, 0.5f);

            List<float3> buttonTapaPoints = PoliedroXZ(sides, float3(0, altura_tapa, 0), 2.1f);
            List<float3> topTapaPoints = PoliedroXZ(sides, float3(0, altura_tapa + h_tapa, 0), 0.3f);
            Mesh<V> tapa_mesh = CoffeMakerSection_Mesh(buttonTapaPoints, topTapaPoints);

            Mesh<V> up_mesh = top_mesh.Add_Mesh(tapa_mesh);
            up_mesh = up_mesh.Transform(Transforms.RotateRespectTo(float3(0,0,0), float3(0,1,0), pi/3 + pi/10));

            Mesh<V> r_model = base_mesh.Add_Mesh(coffee_base_mesh).Add_Mesh(union_mesh).Add_Mesh(up_mesh);
            r_model = r_model.Bigger_Mesh();

            return r_model;
        }

        private static Mesh<V> Mesh_Poliedro(List<float3> poli, float3 center, int normal_side = 0)
        {
            V[] vertices = new V[poli.Count + 1];
            int[] indices = new int[3 * (poli.Count - 1)];

            vertices[0] = new V{Position = center, Coordinates = float2(0.5f, 0.5f)};

            float radio = Gfx.length(center - poli[0]);
            radio *= 2f;

            int j = 0;
            int i = 1;
            for(; i < poli.Count; i++)
            {
                vertices[i] = new V{Position = poli[i - 1], Coordinates = float2((poli[i - 1].z - center.z) / radio + 0.5f, (poli[i - 1].x - center.x) / radio + 0.5f)};

                if(normal_side == 0)
                {
                    indices[j] = 0;
                    indices[j+1] = i + 1;
                    indices[j+2] = i;
                }

                if(normal_side == 1)
                {
                    indices[j] = 0 ;
                    indices[j+1] = i;
                    indices[j+2] = i + 1;
                }

                j = j + 3;
            }
            vertices[i] = new V{Position = poli[i - 1], Coordinates = float2((poli[i - 1].z - center.z) / radio + 0.5f, (poli[i - 1].x - center.x) / radio + 0.5f)};

            return new Mesh<V>(vertices, indices);
        }

        private static Mesh<V> CoffeMakerSection_Mesh(List<float3> baseF, List<float3> topF)
        {
            V[] vertices = new V[baseF.Count + topF.Count];
            int[] indices = new int[(baseF.Count - 1) * 3 * 2];

            float[] side_len = new float[baseF.Count];
            float total_length = 0f;
            for(int i = 0; i < side_len.Length - 1; i++)
            {
                side_len[i] = total_length;
                total_length += Gfx.length(baseF[i] - baseF[i + 1]);
            }
            side_len[side_len.Length - 1] = total_length;

            int j = 0;
            int k = 0;
            for(int i = 0; i < baseF.Count - 1; i++)
            {
                vertices[j] = new V{Position = baseF[i], Coordinates = float2(side_len[i] / total_length, 1f)};
                vertices[j+1] = new V{Position = topF[i], Coordinates = float2(side_len[i] / total_length, 0f)};

                indices[k] = j;
                indices[k+1] = j + 1;
                indices[k+2] = j + 2;
                indices[k+3] = j + 1;
                indices[k+4] = j + 3;
                indices[k+5] = j + 2;

                j += 2;
                k += 6;
            }

            vertices[j] = new V{Position = baseF[baseF.Count - 1], Coordinates = float2(1f, 1f)};
            vertices[j+1] = new V{Position = topF[topF.Count - 1], Coordinates = float2(1f, 0f)};

            return new Mesh<V>(vertices, indices);
        }

        private static Mesh<V> CoffeMakerTopSection_Mesh(List<float3> baseF, List<float3> topF, float r, float d)
        {
            int n = baseF.Count;

            float3 a = (topF[n - 2] + baseF[n - 2]) / 2;
            float3 b = (topF[n - 1] + baseF[n - 1]) / 2;
            float3 q = (b + a) / 2;
            float3 p = Find(topF[n - 2], topF[n - 1], r, d);

            V[] vertices = new V[baseF.Count + topF.Count + 4];
            int[] indices = new int[(baseF.Count - 1) * 3 * 2 + 12];

            float side_len = Gfx.length(baseF[0] - baseF[1]);
            float side_len_total = (baseF.Count - 1) * side_len;

            int j = 0;
            int k = 0;
            for(int i = 0; i < baseF.Count - 2; i++)
            {
                vertices[j] = new V{Position = baseF[i], Coordinates = float2(side_len * i / side_len_total, 1f)};
                vertices[j+1] = new V{Position = topF[i], Coordinates = float2(side_len * i / side_len_total, 0f)};

                indices[k] = j;
                indices[k+1] = j + 1;
                indices[k+2] = j + 2;
                indices[k+3] = j + 1;
                indices[k+4] = j + 3;
                indices[k+5] = j + 2;

                j += 2;
                k += 6;
            }

            vertices[j] = new V{Position = baseF[baseF.Count - 2], Coordinates = float2(side_len * (baseF.Count - 2) / side_len_total, 1f)};
            vertices[j+1] = new V{Position = topF[topF.Count - 2], Coordinates = float2(side_len * (topF.Count - 2) / side_len_total, 0f)};
            vertices[j+2] = new V{Position = a, Coordinates = float2(side_len * (baseF.Count - 2) / side_len_total, 0.5f)};

            vertices[j+3] = new V{Position = baseF[baseF.Count - 1], Coordinates = float2(1f, 1f)};
            vertices[j+4] = new V{Position = topF[topF.Count - 1], Coordinates = float2(1f, 0f)};
            vertices[j+5] = new V{Position = b, Coordinates = float2(1f, 0.5f)};

            vertices[j+6] = new V{Position = q, Coordinates = float2(side_len * (baseF.Count - 2 + 0.5f) / side_len_total, 0.5f)};
            vertices[j+7] = new V{Position = p, Coordinates = float2(side_len * (baseF.Count - 2 + 0.5f) / side_len_total, 0f)};

            indices[k] = j;
            indices[k+1] = j + 2;
            indices[k+2] = j + 3;
            indices[k+3] = j + 3;
            indices[k+4] = j + 2;
            indices[k+5] = j + 5;


            k += 6;
            indices[k] = j + 1;
            indices[k+1] = j + 6;
            indices[k+2] = j + 2;
            indices[k+3] = j + 6;
            indices[k+4] = j + 1;
            indices[k+5] = j + 7;

            k += 6;
            indices[k] = j + 6;
            indices[k+1] = j + 4;
            indices[k+2] = j + 5;
            indices[k+3] = j + 7;
            indices[k+4] = j + 4;
            indices[k+5] = j + 6;

            return new Mesh<V>(vertices, indices);
        }

        private static Mesh<V> AsaLateralMesh(List<float3>asa, int normal_side = 0)
        {
            int n = asa.Count;
            V[] vertices = new V[n];
            int[] indices = new int[36];

            int j = 0;
            for(int i = 0; i < n; i++)
            {
                vertices[i] = new V{Position = asa[i]};

            }

            float length = 1f;
            float width = 1f;

            vertices[0].Coordinates = float2(0, 0); //0
            vertices[1].Coordinates = float2(width/3, 0); //1
            vertices[2].Coordinates = float2(2 * width / 3, 0); //2
            vertices[3].Coordinates = float2(width, length/4); //3
            vertices[4].Coordinates = float2(width/3, length); //4
            vertices[5].Coordinates = float2(width / 6, 5 * length / 6); //5
            vertices[6].Coordinates = float2(width/3, 4 * length / 6); //6
            vertices[7].Coordinates = float2(width/2, length/2); //7
            vertices[8].Coordinates = float2(2 * width/3, length/4); //8
            vertices[9].Coordinates = float2(3 * width/5, length/5); //9
            vertices[10].Coordinates = float2(2 * width/5, length/4); //10
            vertices[11].Coordinates = float2(width/5, length/5); //11
            vertices[12].Coordinates = float2(width/6, length/4); //12
            vertices[13].Coordinates = float2(-1 * width/8, length/4); //13
            vertices[14].Coordinates = float2(0, 0); //14

            if(normal_side == 0)
            {
                indices[j] = 0;
                indices[++j] = 13;
                indices[++j] = 12;

                indices[++j] = 0;
                indices[++j] = 12;
                indices[++j] = 11;

                indices[++j] = 0;
                indices[++j] = 11;
                indices[++j] = 1;

                indices[++j] = 1;
                indices[++j] = 11;
                indices[++j] = 10;

                indices[++j] = 1;
                indices[++j] = 10;
                indices[++j] = 9;

                indices[++j] = 1;
                indices[++j] = 9;
                indices[++j] = 2;

                indices[++j] = 2;
                indices[++j] = 9;
                indices[++j] = 3;

                indices[++j] = 3;
                indices[++j] = 9;
                indices[++j] = 8;

                indices[++j] = 3;
                indices[++j] = 8;
                indices[++j] = 7;

                indices[++j] = 3;
                indices[++j] = 7;
                indices[++j] = 6;

                indices[++j] = 3;
                indices[++j] = 6;
                indices[++j] = 4;

                indices[++j] = 4;
                indices[++j] = 6;
                indices[++j] = 5;
            }

            if(normal_side == 1)
            {
                indices[j] = 0;
                indices[++j] = 12;
                indices[++j] = 13;

                indices[++j] = 0;
                indices[++j] = 11;
                indices[++j] = 12;

                indices[++j] = 0;
                indices[++j] = 1;
                indices[++j] = 11;

                indices[++j] = 1;
                indices[++j] = 10;
                indices[++j] = 11;

                indices[++j] = 1;
                indices[++j] = 9;
                indices[++j] = 10;

                indices[++j] = 1;
                indices[++j] = 2;
                indices[++j] = 9;

                indices[++j] = 2;
                indices[++j] = 3;
                indices[++j] = 9;

                indices[++j] = 3;
                indices[++j] = 8;
                indices[++j] = 9;

                indices[++j] = 3;
                indices[++j] = 7;
                indices[++j] = 8;

                indices[++j] = 3;
                indices[++j] = 6;
                indices[++j] = 7;

                indices[++j] = 3;
                indices[++j] = 4;
                indices[++j] = 6;

                indices[++j] = 4;
                indices[++j] = 5;
                indices[++j] = 6;
            }

            return new Mesh<V>(vertices, indices);
        }

        private static float3[] ApplyTransform(float3[] points, float4x4 matrix)
        {
            float3[] result = new float3[points.Length];

            // Transform points with a matrix
            // Linear transform in homogeneous coordinates
            for (int i = 0; i < points.Length; i++)
            {
                float4 h = float4(points[i], 1);
                h = mul(h, matrix);
                result[i] = h.xyz / h.w;
            }

            return result;
        }

        private static float3[] ApplyTransform(float3[] points, Func<float3, float3> freeTransform)
        {
            float3[] result = new float3[points.Length];

            // Transform points with a function
            for (int i = 0; i < points.Length; i++)
                result[i] = freeTransform(points[i]);

            return result;
        }

        private static float3 Find(float3 a, float3 b, float r, float d){
            float3 m = (a + b) / 2;
            float k = m[2] / m[0];
            float xp = (float)Math.Sqrt(((d + r) * (d + r)) / (1 + k * k));
            return float3(xp, m[1], k * xp);
        }

        private static List<float3> PoliedroXZ(int sides, float3 centre, float radio)
        {
            List<float3> points = new List<float3>();

            for(float i = 0; i < pi * 2; i += (pi * 2 / sides))
            {
                points.Add(centre + float3(radio * (float)Math.Cos(i), 0, radio * (float)Math.Sin(i)));
            }
            points.Add(points[0]);

            return points;
        }

        private static List<float3> AsaXZ(float3 site, float length, float width, float height)
        {
            List<float3> points = new List<float3>();

            points.Add(float3(0, 0, 0)); //0
            points.Add(float3(0, 0, width/3)); //1
            points.Add(float3(0, 0, 2 * width / 3)); //2
            points.Add(float3(length/4, 0, width)); //3
            points.Add(float3(length, 0, width/3)); //4
            points.Add(float3(5 * length / 6, 0, width / 6)); //5
            points.Add(float3(4 * length / 6, 0,  width/3)); //6
            points.Add(float3(length/2, 0, width/2)); //7
            points.Add(float3(length/4, 0, 2 * width/3)); //8
            points.Add(float3(length/5, 0, 3 * width/5)); //9
            points.Add(float3(length/4, 0, 2 * width/5)); //10
            points.Add(float3(length/5, 0, width/5)); //11
            points.Add(float3(length/4, 0, width/6)); //12
            points.Add(float3(length/4, 0, -1 * width/8)); //13
            points.Add(float3(0, 0, 0)); //14

            int l = points.Count;
            for(int i = 0; i < l; i++)
            {
                points.Add(points[i] + float3(0, height, 0));
            }


            float4x4 transform = mul(mul(mul(Transforms.Translate(0, -1 * height/2, 0), Transforms.RotateRespectTo(float3(0,0,0), float3(0,0,1), -1 * pi / 2)), Transforms.Translate(site)), Transforms.RotateRespectTo(float3(0,0,0), float3(0,1,0), 8 * pi/5));

            float3[] points_r = ApplyTransform(points.ToArray(), transform);

            return new List<float3>(points_r);
        }
    }
}