using GMath;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rendering
{
    public class MyTexture2D : Texture2D
    {
        public MyTexture2D(int width, int height) : base(width, height)
        {
        }

        public class PixelDrawedEventArg : EventArgs
        {
            public int x { get; }
            public int y { get; }
            public float4 color { get; }

            public PixelDrawedEventArg(int x, int y, float4 color)
            {
                this.x = x;
                this.y = y;
                this.color = color;
            }
        }

        public event EventHandler<PixelDrawedEventArg> PixelDrawed;

        public override void Write(int x, int y, float4 value)
        {
            base.Write(x, y, value);
            PixelDrawed?.Invoke(this, new PixelDrawedEventArg(x, y, value));
        }
    }
}
