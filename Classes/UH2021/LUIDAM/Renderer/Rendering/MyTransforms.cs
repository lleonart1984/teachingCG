using GMath;
using static GMath.Gfx;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Rendering
{
    public static class MyTransforms
    {
		public static float4x4 FitIn(float3 lowerBound, float3 upperBound, float width, float height, float deep)
		{
			var toRender = Transforms.Translate(-lowerBound);
			var scale = new float[] { width / (upperBound.x - lowerBound.x), height / (upperBound.y - lowerBound.y), deep / (upperBound.z - lowerBound.z) }.Min();
			toRender = mul(toRender, Transforms.Scale(scale, scale, scale));
			return toRender;
		}
	}
}
