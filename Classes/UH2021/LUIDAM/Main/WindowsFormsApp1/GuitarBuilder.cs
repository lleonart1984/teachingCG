using Renderer.Modeling;
using Renderer.Rendering;
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

        public Model BridgeStrings()
        {
            var strings = new Model();
            var step = BridgeWidth / (StringWidths.Length + 1);

            for (int i = 0; i < StringWidths.Length; i++)
            {
                var cylinder = ShapeGenerator.Cylinder(1000).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                             Transforms.Scale(StringWidths[i], StringWidths[i], 1),
                                                                             Transforms.Translate(-BridgeWidth / 2 + step * (i + 1), -1, 0));
                strings += cylinder;
            }

            return strings.ApplyTransforms(Transforms.Scale(1, 1, StringLength));
        }

        public Model Bridge()
        {

            var bridge = ShapeGenerator.Box(4000).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                  Transforms.Scale(BridgeWidth, 1, 1))
                                                 .ApplyFilter(x => x.y != .5f); // Remove face facing the cylinder
            var bridge2 = ShapeGenerator.Cylinder(5000).ApplyFilter(x => x.y > 0) // Remove top half of the cylinder
                                                       .ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                        Transforms.Scale(BridgeWidth / 2, 1, 1),
                                                                        Transforms.Translate(0, .5f, 0));

            var baseBridge = bridge.Min(x => x.z);
            var fretsAmount = 20;
            var step = BridgeLength / fretsAmount;
            var frets = new Model();
            for (int i = 0; i < fretsAmount; i++) // Frets
            {
                var fret = ShapeGenerator.Box(500).ApplyTransforms(Transforms.Translate(0, 0, .5f),
                                                                   Transforms.Scale(BridgeWidth, BridgeWidth / 30, BridgeWidth / 30),
                                                                   Transforms.Translate(0, -.5f, -baseBridge + step * i)); // This is not the correct fret spacing
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

            //var body = ShapeGenerator.Box(5000).ApplyTransforms(Transforms.Scale(float3(1, 2, .3f)))
            //                                   .ApplyFreeTransform(x => float3(x.x, x.y, x.z));

            return new Model();
        }

        public Model Guitar()
        {
            return Bridge() + BridgeStrings() + Headstock() + MainBody();
        }
    }
}
