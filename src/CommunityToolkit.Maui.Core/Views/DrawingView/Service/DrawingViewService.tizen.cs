﻿using Microsoft.Maui.Graphics.Skia;
using SkiaSharp;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Drawing view service
/// </summary>
public static class DrawingViewService
{
	/// <summary>
	/// Get image stream from points
	/// </summary>
	/// <param name="points">Drawing points</param>
	/// <param name="imageSize">Image size</param>
	/// <param name="lineWidth">Line Width</param>
	/// <param name="strokeColor">Line color</param>
	/// <param name="background">Image background</param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(IList<PointF> points, Size imageSize, float lineWidth, Color strokeColor, Paint? background)
	{
		var image = GetBitmapForPoints(points, lineWidth, strokeColor, background);

		if (image is null)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		var resized = image.Resize(new SKImageInfo((int)imageSize.Width, (int)imageSize.Height, SKColorType.Bgra8888, SKAlphaType.Opaque), SKFilterQuality.High);
		var data = resized.Encode(SKEncodedImageFormat.Png, 100);

		var stream = new MemoryStream();
		data.SaveTo(stream);
		stream.Seek(0, SeekOrigin.Begin);

		return ValueTask.FromResult<Stream>(stream);
	}

	/// <summary>
	/// Get image stream from lines
	/// </summary>
	/// <param name="lines">Drawing lines</param>
	/// <param name="imageSize">Image size</param>
	/// <param name="background">Image background</param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(IList<IDrawingLine> lines, Size imageSize, Paint? background)
	{
		var image = GetBitmapForLines(lines, background);

		if (image is null)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		var resized = image.Resize(new SKImageInfo((int)imageSize.Width, (int)imageSize.Height, SKColorType.Bgra8888, SKAlphaType.Opaque), SKFilterQuality.High);
		var data = resized.Encode(SKEncodedImageFormat.Png, 100);

		var stream = new MemoryStream();
		data.SaveTo(stream);
		stream.Seek(0, SeekOrigin.Begin);

		return ValueTask.FromResult<Stream>(stream);
	}

	static (SKBitmap?, SizeF offset) GetBitmap(in ICollection<PointF> points)
	{
		if (points.Count is 0)
		{
			return (null, SizeF.Zero);
		}

		const int minSize = 1;
		var minPointX = points.Min(static p => p.X);
		var minPointY = points.Min(static p => p.Y);
		var drawingWidth = points.Max(static p => p.X) - minPointX;
		var drawingHeight = points.Max(static p => p.Y) - minPointY;

		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return (null, SizeF.Zero);
		}

		var bitmap = new SKBitmap((int)drawingWidth, (int)drawingHeight, SKColorType.Bgra8888, SKAlphaType.Opaque);

		return (bitmap, new SizeF(minPointX, minPointY));
	}

	static SKBitmap? GetBitmapForLines(in IList<IDrawingLine> lines, in Paint? background)
	{
		var points = lines.SelectMany(static x => x.Points).ToList();
		var (image, offset) = GetBitmap(points);

		if (image is null)
		{
			return null;
		}

		using var canvas = new SKCanvas(image);

		DrawBackground(canvas, image.Info, background);

		foreach (var line in lines)
		{
			DrawStrokes(canvas, line.Points, line.LineWidth, line.LineColor, offset);
		}

		return image;
	}

	static SKBitmap? GetBitmapForPoints(in ICollection<PointF> points, in float lineWidth, in Color strokeColor, in Paint? background)
	{
		var (image, offset) = GetBitmap(points);
		if (image is null)
		{
			return null;
		}

		using var canvas = new SKCanvas(image);

		DrawBackground(canvas, image.Info, background);
		DrawStrokes(canvas, points, lineWidth, strokeColor, offset);

		return image;
	}

	static void DrawBackground(in SKCanvas canvas, in SKImageInfo info, in Paint? brush)
	{
		switch (brush)
		{
			case SolidPaint solidColorBrush:
				canvas.DrawColor(solidColorBrush.Color is not null
									? solidColorBrush.Color.AsSKColor()
									: DrawingViewDefaults.BackgroundColor.AsSKColor());
				break;

			case LinearGradientPaint linearGradientBrush:
				var paint = new SKPaint();
				var colors = new SKColor[linearGradientBrush.GradientStops.Length];
				var positions = new float[linearGradientBrush.GradientStops.Length];

				for (var index = 0; index < linearGradientBrush.GradientStops.Length; index++)
				{
					var gradientStop = linearGradientBrush.GradientStops[index];
					colors[index] = gradientStop.Color.AsSKColor();
					positions[index] = gradientStop.Offset;
				}

				var x1 = (float)linearGradientBrush.StartPoint.X * info.Width;
				var y1 = (float)linearGradientBrush.StartPoint.Y * info.Height;
				var x2 = (float)linearGradientBrush.EndPoint.X * info.Width;
				var y2 = (float)linearGradientBrush.EndPoint.Y * info.Height;

				var shader = SKShader.CreateLinearGradient(new SKPoint(x1, y1),
															new SKPoint(x2, y2),
															colors,
															positions,
															SKShaderTileMode.Clamp);
				paint.Shader = shader;
				canvas.DrawRect(0, 0, info.Width, info.Height, paint);
				break;

			case RadialGradientPaint radialGradientBrush:
				var paint = new SKPaint();
				var colors = new SKColor[radialGradientBrush.GradientStops.Length];
				var positions = new float[radialGradientBrush.GradientStops.Length];

				for (var index = 0; index < radialGradientBrush.GradientStops.Length; index++)
				{
					var gradientStop = radialGradientBrush.GradientStops[index];
					colors[index] = gradientStop.Color.AsSKColor();
					positions[index] = gradientStop.Offset;
				}

				float centerX = (float)(radialGradientBrush.Center.X * info.Width);
				float centerY = (float)(radialGradientBrush.Center.Y * info.Height);
				float radius = (float)(radialGradientBrush.Radius * info.Width);

				var shader = SKShader.CreateRadialGradient(new SKPoint(centerX, centerX),
															radius,
															colors,
															positions,
															SKShaderTileMode.Clamp);
				paint.Shader = shader;
				canvas.DrawRect(0, 0, info.Width, info.Height, paint);
				break;

			default:
				canvas.DrawColor(DrawingViewDefaults.BackgroundColor.AsSKColor());
				break;
		}
	}

	static void DrawStrokes(in SKCanvas canvas, in ICollection<PointF> points, in float lineWidth, in Color strokeColor, in SizeF offset)
	{
		using var paint = new SKPaint
		{
			StrokeWidth = lineWidth,
			StrokeJoin = SKStrokeJoin.Round,
			StrokeCap = SKStrokeCap.Round,
			IsAntialias = true,
			Color = strokeColor.AsSKColor(),
			Style = SKPaintStyle.Stroke,
		};

		var pointsCount = points.Count;
		for (var i = 0; i < pointsCount - 1; i++)
		{
			var p1 = points.ElementAt(i);
			var p2 = points.ElementAt(i + 1);

			canvas.DrawLine(p1.X - offset.Width, p1.Y - offset.Height, p2.X - offset.Width, p2.Y - offset.Height, paint);
		}
	}
}