using Renderer.Modeling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Renderer.Rendering;
using static GMath.Gfx;
using GMath;
using System.IO;

namespace MainForm
{
    public partial class Form1 : Form
    {
        private Model _baseModel;
        private Model _model;
        private float3 _baseTranslation;
        private float3 _baseZoom;
        private float _maxScale;
        private float _minScale;

        public Form1()
        {
            InitializeComponent();

            imageFile.FileOk += ImageFile_FileOk;

            //_baseModel = ShapeGenerator.Sphere(5000);
            //_baseModel = ShapeGenerator.Box(5000);
            SetGuitar();
            _baseTranslation = float3(imagePbx.Width / 2, imagePbx.Height / 2, 0);
            _baseZoom = float3(40, 40, 40);
            _model = _baseModel.ApplyTransforms(Transforms.Scale(_baseZoom), Transforms.Translate(_baseTranslation));
            _maxScale = 4.0f;
            _minScale = .5f;
            var stepSize = (_maxScale - _minScale) / (zoomBar.Maximum - zoomBar.Minimum);
            zoomBar.Value = (int)((1 - _minScale) / stepSize); // Setting zoomBar value to the value that represent scaling by 1
            DrawModel();
        }

        private void SetGuitar()
        {
            //var body = ShapeGenerator.Box(5000).ApplyTransforms(Transforms.Scale(float3(1, 2, .3f)));
            //var bridge = ShapeGenerator.Box(5000).ApplyTransforms(Transforms.Scale(float3(.3f, 2, .2f)),
            //                                                      Transforms.Translate(float3(0, 2, 0)));
            //var top = ShapeGenerator.Box(5000).ApplyTransforms(Transforms.Scale(.4f,.6f,.1f),
            //                                                   Transforms.Translate(float3(0,3.3f,0)));
            //body += bridge + top;
            //_baseModel = body;
            //var axesy = ShapeGenerator.Box(4000).ApplyTransforms(Transforms.Scale(10, .01f, 10),
            //                                                     Transforms.Translate(0,.5f,0));

            float stringLength = 20;

            float bridgeLength = stringLength * 475 / 610;
            float bridgeWidth = 4;

            var strings = new Model();
            var stringWidthScalars = new float[] { .06f, .05f, .04f, .04f, .03f, .02f };
            var step = bridgeWidth / (stringWidthScalars.Length + 1);
            for (int i = 0; i < stringWidthScalars.Length; i++)
            {
                var cylinder = ShapeGenerator.Cylinder(1000).ApplyTransforms(Transforms.Translate(0,0,.5f),
                                                                             Transforms.Scale(stringWidthScalars[i], stringWidthScalars[i], 1),
                                                                             Transforms.Translate(-bridgeWidth/2 + step*(i+1),-1,0));
                strings += cylinder;
            }

            var bridge = ShapeGenerator.Box(4000).ApplyTransforms(Transforms.Translate(0, 0, .5f), 
                                                                  Transforms.Scale(bridgeWidth, 1, 1))
                                                 .ApplyFilter(x => x.y != .5f); // Remove face facing the cylinder
            var bridge2 = ShapeGenerator.Cylinder(5000).ApplyFilter(x => x.y > 0) // Remove top half of the cylinder
                                                       .ApplyTransforms(Transforms.Translate(0, 0, .5f), 
                                                                        Transforms.Scale(bridgeWidth/2, 1, 1),
                                                                        Transforms.Translate(0,.5f,0));

            var baseBridge = bridge.Min(x => x.z);
            var fretsAmount = 20;
            step = bridgeLength / fretsAmount;
            var frets = new Model();
            for (int i = 0; i < fretsAmount; i++) // Frets
            {
                var fret = ShapeGenerator.Box(500).ApplyTransforms(Transforms.Translate(0, 0, .5f), 
                                                                   Transforms.Scale(bridgeWidth, bridgeWidth/30, bridgeWidth / 30),
                                                                   Transforms.Translate(0, -.5f, -baseBridge + step * i)); // This is not the correct fret spacing
                frets += fret;
            }

            _baseModel = (bridge + bridge2).ApplyTransforms(Transforms.Scale(1, 1, bridgeLength)) + 
                          strings.ApplyTransforms(Transforms.Scale(1,1, stringLength)) + 
                          frets;

        }

        public void DrawModel()
        {
            Bitmap image = new Bitmap(imagePbx.Width, imagePbx.Height);
            for (int i = 0; i < _model.Length; i++)
            {
                var point = _model[i];
                (int x, int y) = ((int)point.x, (int)point.y);
                if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
                    continue;
                image.SetPixel(x, y, Color.White);
            }
            imagePbx.Image = image;
            imagePbx.Invalidate();
        }

        private void imagePbx_Click(object sender, EventArgs e)
        {
            imageFile.ShowDialog();
        }

        private void ImageFile_FileOk(object sender, CancelEventArgs e)
        {
            if (!e.Cancel)
            {
                BinaryReader reader = new BinaryReader(imageFile.OpenFile());

                int width = reader.ReadInt32(); //read width value
                int height = reader.ReadInt32(); // read height value

                Bitmap bmp = new Bitmap(width, height);

                for (int py = 0; py < height; py++)
                    for (int px = 0; px < width; px++)
                    {
                        float r = reader.ReadSingle();
                        float g = reader.ReadSingle();
                        float b = reader.ReadSingle();
                        float a = reader.ReadSingle();
                        a = 1;// force no transparent

                        bmp.SetPixel(px, py, Color.FromArgb(ColorComponent(a), ColorComponent(r), ColorComponent(g), ColorComponent(b)));
                    }
                reader.Close();
                imagePbx.Image = bmp;
                imagePbx.Invalidate();
            }
        }

        static int ColorComponent(float x)
        {
            return (int)Math.Max(0, Math.Min(255, 256 * x));
        }

        private void UpdateModel(object sender, EventArgs e)
        {
            var stepSize = (_maxScale - _minScale) / (zoomBar.Maximum - zoomBar.Minimum);
            var scalar = _minScale + stepSize * zoomBar.Value;
            label1.Text = $"{xRotation.Value}, {yRotation.Value}, {zRotation.Value}, {scalar}";

            _model = _baseModel.ApplyTransforms(Transforms.Scale(_baseZoom * float3(scalar, scalar, scalar)),
                                                Transforms.RotateX((xRotation.Value * two_pi) / (xRotation.Maximum - xRotation.Minimum)),
                                                Transforms.RotateY((yRotation.Value * two_pi) / (yRotation.Maximum - yRotation.Minimum)),
                                                Transforms.RotateZ((zRotation.Value * two_pi) / (zRotation.Maximum - zRotation.Minimum)),
                                                Transforms.Translate(_baseTranslation + float3((float)xTranslation.Value, (float)yTranslation.Value, (float)zTranslation.Value)));
            DrawModel();
        }
    }
}
