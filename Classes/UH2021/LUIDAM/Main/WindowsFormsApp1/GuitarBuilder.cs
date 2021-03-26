using Renderer.Modeling;
using Rendering;
using System;
using System.Collections.Generic;
using static GMath.Gfx;
using System.Linq;
using System.Text;
using GMath;

namespace MainForm
{
    public class GuitarBuilder
    {

        public float StringLength => 20;

        public float BridgeLength => StringLength * 475 / 610;

        public float[] StringWidths => new float[] { .06f, .05f, .04f, .04f, .03f, .02f };

        public float BridgeWidth => 4;

        public float BridgeHeight => 1.5f;

        public float BodyWidth => BridgeWidth * 5;

        public Model BridgeStrings()
        {
            var strings = new Model();
            var step = BridgeWidth / (StringWidths.Length + 1);

            for (int i = 0; i < StringWidths.Length; i++)
            {
                var cylinder = ShapeGenerator.Cylinder(1000).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                             Transforms.Scale(StringWidths[i], StringWidths[i], 1),
                                                                             Transforms.Translate(-BridgeWidth / 2 + step * (i + 1), -1 * BridgeHeight/2, 0));
                strings += cylinder;
            }

            return strings.ApplyTransforms(Transforms.Scale(1, 1, StringLength));
        }

        public Model Bridge()
        {

            var bridge = ShapeGenerator.Box(4000).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                  Transforms.Scale(BridgeWidth, BridgeHeight/2, 1))
                                                 .ApplyFilter(x => x.y != .5f); // Remove face facing the cylinder
            var bridge2 = ShapeGenerator.Cylinder(5000).ApplyFilter(x => x.y > 0) // Remove top half of the cylinder
                                                       .ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                        Transforms.Scale(BridgeWidth / 2, BridgeHeight / 2, 1),
                                                                        Transforms.Translate(0, .5f * BridgeHeight / 2, 0));

            var baseBridge = bridge.Min(x => x.z);
            var fretsAmount = 20;
            var step = BridgeLength / fretsAmount;
            var frets = new Model();
            for (int i = 0; i < fretsAmount; i++) // Frets
            {
                var fret = ShapeGenerator.Box(500).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                   Transforms.Scale(BridgeWidth, BridgeWidth / 30, BridgeWidth / 30),
                                                                   Transforms.Translate(0, -.5f * BridgeHeight/2, -baseBridge + step * i)); // This is not the correct fret spacing
                frets += fret;
            }

            return (bridge + bridge2).ApplyTransforms(Transforms.Scale(1, 1, BridgeLength)) + frets;
        }

        public Model Headstock()
        {
            // IMPLEMENTATION OF THE GUITAR HEAD
            return new Model();
        }

        public Model MainBody()
        {
            // IMPLEMENTATION OF THE GUITAR MAIN BODY

            var body = ShapeGenerator.Box(10000).ApplyTransforms(Transforms.Translate(0,0,.5f),
                                                                Transforms.Scale(BridgeWidth*5, BridgeHeight*2, BridgeLength),
                                                                Transforms.Translate(0, BridgeHeight, BridgeLength - BridgeLength / 4));

            var radius = BridgeWidth * 1.5f / 2;
            var dz = BridgeLength + radius - (BridgeLength / 4 * .2f);
            var c = ShapeGenerator.Cylinder().ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                              Transforms.Scale(radius, 2, radius),
                                                              Transforms.Translate(0, -.25f, dz));

            var hole = ShapeGenerator.Cylinder(thickness: BridgeWidth/8).ApplyTransforms(Transforms.RotateX(pi_over_4 * 2),
                                                              Transforms.Scale(radius, .5f, radius),
                                                              Transforms.Translate(0, -.25f, dz));
            
            dz = (c.Max(x => x.z) + c.Min(x => x.z)) / 2;
            var dx = (c.Max(x => x.x) + c.Min(x => x.x)) / 2;
            var minY = c.Min(x => x.y);
            var maxY = c.Max(x => x.y);

            body = body.ApplyFilter(x => !(Math.Pow(x.x - dx, 2) + Math.Pow(x.z - dz, 2) <= Math.Pow(radius, 2) && 
                                         minY <= x.y && x.y <= maxY));

            float3[] ff = new float3[1000];
            var step = (maxY - minY) / ff.Length;
            for (int i = 0; i < ff.Length; i++)
            {
                ff[i] = float3(dx,minY + i*step,dz);
            }

            return hole + body + new Model(ff);
        }

        public Model Guitar()
        {
            var body = MainBody() + Bridge();
            return body + BridgeStrings() + Headstock();
            //return MainBody();
        }
    }
}
