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
using Rendering;
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
            _baseZoom = float3(20, 20, 20);
            _model = _baseModel.ApplyTransforms(Transforms.Scale(_baseZoom), Transforms.Translate(_baseTranslation));
            _maxScale = 4.0f;
            _minScale = .5f;
            var stepSize = (_maxScale - _minScale) / (zoomBar.Maximum - zoomBar.Minimum);
            zoomBar.Value = (int)((1 - _minScale) / stepSize); // Setting zoomBar value to the value that represent scaling by 1
            DrawModel();
        }

        private void SetGuitar()
        {

            var generator = new GuitarBuilder();

            _baseModel = generator.Guitar();
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

            // WORLD TRANSFORMATIONS
            _model = _baseModel.ApplyTransforms(Transforms.Scale(_baseZoom * float3(scalar, scalar, scalar)),
                                                Transforms.RotateX((xRotation.Value * two_pi) / (xRotation.Maximum - xRotation.Minimum)),
                                                Transforms.RotateY((yRotation.Value * two_pi) / (yRotation.Maximum - yRotation.Minimum)),
                                                Transforms.RotateZ((zRotation.Value * two_pi) / (zRotation.Maximum - zRotation.Minimum)),
                                                Transforms.Translate(_baseTranslation + float3((float)xTranslation.Value, (float)yTranslation.Value, (float)zTranslation.Value)));
            DrawModel();
        }
    }
}
