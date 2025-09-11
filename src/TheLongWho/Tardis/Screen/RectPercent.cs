using UnityEngine;

namespace TheLongWho.Tardis.Screen
{
	public struct RectPercent
	{
		public float X, Y, Width, Height;

		public RectPercent(float x, float y, float width, float height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public Rect ToRect(Vector2 canvasSize)
		{
			float realWidth = (Width / 100f) * canvasSize.x;
			float realHeight = (Height / 100f) * canvasSize.y;

			float realX = (X / 100f) * canvasSize.x;
			float realY = (Y / 100f) * canvasSize.y;

			return new Rect(realX, realY, realWidth, realHeight);
		}
	}
}
