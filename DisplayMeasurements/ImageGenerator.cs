using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DisplayMeasurements
{
	enum Orientation { Horizontal, Vertical };

	record Measurement(Orientation Orientation, float Start, float End, string Name);

	internal static class ImageGenerator
	{
		const int ImageWidth = 720;
		const int ImageHeight = 720;

		const int TriangleHeight = 8;
		const int TriangleWidth = 8;

		private static IPath GenerateArrowHead()
		{
			var p1 = new PointF(0, 0);
			var p2 = new PointF(p1.X - TriangleWidth / 2, p1.Y + TriangleHeight);
			var p3 = new PointF(p1.X + TriangleWidth / 2, p2.Y);
			return new Polygon(
				new LinearLineSegment(
					p1,
					p2
				),
				new LinearLineSegment(
					p2,
					p3
				)
			);
		}

		private static IPath ArrowHead = GenerateArrowHead();

		private static IPath GenerateArrow(PointF tip, float radians) =>
			ArrowHead
				.Transform(Matrix3x2Extensions.CreateRotation(radians, new PointF(0, 0)))
				.Transform(Matrix3x2Extensions.CreateTranslation(tip));

		private static Font AnnotationFont = new Font(
			SystemFonts.Get("Segoe ui"),
			20
		);

		record AnnotationPosition(
			PointF PointF,
			HorizontalAlignment HorizontalAlignment,
			VerticalAlignment VerticalAlignment,
			PointF lineStartOffset
		);

		private static void RenderMeasurement(
			IImageProcessingContext imagingProcessingContext,
			RectangleF dividingLineBounds,
			Measurement measurement, 
			float measurementLinePos,
			AnnotationPosition annotationPosition
		)
		{
			Func<float, ILineSegment> toDividingLine = measurement.Orientation switch
			{
				Orientation.Horizontal => m => new LinearLineSegment(new(m, dividingLineBounds.Top), new(m, dividingLineBounds.Bottom)),
				Orientation.Vertical => m => new LinearLineSegment(new(dividingLineBounds.Left, m), new(dividingLineBounds.Right, m)),
				_ => throw new Exception($"Encounted unexpected orientation of ${measurement.Orientation}")
			};
			var dividingLines = new[] { measurement.Start, measurement.End }.Select(toDividingLine);
			foreach(var line in dividingLines)
			{
				var p = line.Flatten().ToArray();
				imagingProcessingContext.DrawLines(
					new Pen(Color.Gray, 1),
					p[0],
					p[1]
				);
			}

			(
				PointF mLineStart,
				float arrowStartRotation,
				PointF mLineEnd,
				float arrowEndRotation
				) = measurement.Orientation switch
			{
				Orientation.Vertical => (
					new PointF( measurementLinePos, (float)measurement.Start),
					0.0f,
					new PointF(measurementLinePos, (float)measurement.End),
					(float)Math.PI
				),
				Orientation.Horizontal => (
					new PointF((float)measurement.Start, measurementLinePos),
					(float)Math.PI*3f/2,
					new PointF((float)measurement.End, measurementLinePos),
					(float)Math.PI/2.0f
				),
				_ => throw new Exception($"Unexpected orientation of {measurement.Orientation}")
			};
			imagingProcessingContext.DrawLines(
				new Pen(Color.Black, 2),
				mLineStart,
				mLineEnd
			);
			foreach(
				var triangle in new[]
				{
					GenerateArrow(mLineStart, arrowStartRotation),
					GenerateArrow(mLineEnd, arrowEndRotation)
				}
			)
			{
				imagingProcessingContext.Fill(Color.Black, triangle);
			}

			RenderAnnotation(imagingProcessingContext, measurement.Name, annotationPosition);
			imagingProcessingContext.DrawLines(
				new Pen(Color.Gray, 1),
				new (
					annotationPosition.PointF.X + annotationPosition.lineStartOffset.X,
					annotationPosition.PointF.Y + annotationPosition.lineStartOffset.Y
				),
				new PointF((mLineStart.X + mLineEnd.X) / 2, (mLineStart.Y + mLineEnd.Y) / 2)
			);
		}

		private static void RenderAnnotation(IImageProcessingContext imagingProcessingContext, string text, AnnotationPosition annotationPosition)
		{
			IPathCollection annotationGlyphs = TextBuilder.GenerateGlyphs(
				text,
				new TextOptions(AnnotationFont)
				{
					ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
				}
			);
			var bounds = annotationGlyphs.Bounds;
			var leftPoint = annotationPosition.HorizontalAlignment switch
			{
				HorizontalAlignment.Left => annotationPosition.PointF.X,
				HorizontalAlignment.Right => annotationPosition.PointF.X - bounds.Width,
				_ => annotationPosition.PointF.X - bounds.Width / 2,
			};
			var topPoint = annotationPosition.VerticalAlignment switch
			{
				VerticalAlignment.Top => annotationPosition.PointF.Y,
				VerticalAlignment.Bottom => annotationPosition.PointF.Y - bounds.Height,
				_ => annotationPosition.PointF.Y - (bounds.Height / 2),
			};
			annotationGlyphs = annotationGlyphs.Translate(new PointF(leftPoint, topPoint));
			imagingProcessingContext.Fill(Color.Black, annotationGlyphs);
		}

		public static Image<Rgba32> GenerateImage(char theCharacter)
		{
			var fontCollection = new FontCollection();
			FontFamily fontFamily = fontCollection.Add("./NettoOffc.ttf");

			float fontSize = 200;
			Font font = new Font(fontFamily, fontSize, FontStyle.Regular);

			var codePoint = new SixLabors.Fonts.Unicode.CodePoint(theCharacter);
			var glyphs = font.GetGlyphs(codePoint, ColorFontSupport.MicrosoftColrFormat);

			var fontMetrics = font.FontMetrics;
			var glyphMetrics = fontMetrics.GetGlyphMetrics(codePoint, ColorFontSupport.MicrosoftColrFormat).First();


			var imageWidth = 720;
			var imageHeight = 720;
			var imgAll = new Image<Rgba32>(ImageWidth, ImageHeight);

			float dpi = (float)imgAll.Metadata.HorizontalResolution; // ???
			float scaleFactor = fontSize * dpi / fontMetrics.ScaleFactor;

			imgAll.Mutate(
				imageProcessingContext =>
				{
					imageProcessingContext.Fill(Color.White);

					var textOptions = new TextOptions(font)
					{
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Top,
						Dpi = (float)dpi,
					};
					var text = theCharacter.ToString();


					// Draw a rectangle made up of the advanceWidth and the AdvanceHeight
					var rectangleWidth = glyphMetrics.AdvanceWidth * scaleFactor;
					var rectangleHeight = glyphMetrics.AdvanceHeight * scaleFactor;
					var rectangleStart = new PointF(
						(float)(imageWidth - rectangleWidth) / 2.0f,
						(float)(imageHeight - rectangleHeight) / 2.0f
					);
					RectangleF advanceBounds = new (
						rectangleStart, 
						new SizeF((float)rectangleWidth, 
						(float)rectangleHeight)
					);

					RectangularPolygon advanceBoundsPolygon = new RectangularPolygon(advanceBounds);
					imageProcessingContext.Fill(Color.Azure, advanceBoundsPolygon);

					RectangleF glyphBounds = new RectangleF(
						new PointF(
							advanceBounds.X + glyphMetrics.LeftSideBearing * scaleFactor,
							advanceBounds.Y + glyphMetrics.TopSideBearing * scaleFactor
						),
						new SizeF(
							glyphMetrics.Width * scaleFactor,
							glyphMetrics.Height * scaleFactor
						)
					);
					imageProcessingContext.Fill(Color.LightGreen, new RectangularPolygon(glyphBounds));


					RectangleF dividingLineBounds = new RectangleF(
						new PointF(
							advanceBounds.Left - 50,
							advanceBounds.Top - 50
						),
						new SizeF(
							advanceBounds.Width + 100,
							advanceBounds.Height + 100
						)
					);

					var topBearingSize = glyphMetrics.TopSideBearing * scaleFactor;
					var topBearingMeasurement = new Measurement(
							Orientation.Vertical,
							advanceBounds.Top,
							advanceBounds.Top + topBearingSize, 
							"TopSideBearing"
						);

					var heightMeasurement = new Measurement(
						Orientation.Vertical,
						advanceBounds.Top + topBearingSize,
						advanceBounds.Top + topBearingSize + glyphMetrics.Height * scaleFactor,
						"Height"
					);

					var bottomBearingMeasurement = new Measurement(
						Orientation.Vertical,
						heightMeasurement.End,
						heightMeasurement.End + glyphMetrics.BottomSideBearing * scaleFactor,
						"BottomSideBearing"
					);
					var leftMeasurementPos = advanceBounds.Left - 50;
					const float leftMeasurementMargin = 40.0f;
					RenderMeasurement(
						imageProcessingContext,
						dividingLineBounds,
						topBearingMeasurement,
						leftMeasurementPos,
						new AnnotationPosition(
							new PointF(
								leftMeasurementPos - leftMeasurementMargin,
								advanceBoundsPolygon.Top + advanceBoundsPolygon.Height / 4
							),
							HorizontalAlignment.Right,
							VerticalAlignment.Center,
							new PointF(5, 5)
						)
					);
					RenderMeasurement(
						imageProcessingContext,
						dividingLineBounds,
						heightMeasurement,
						leftMeasurementPos + 20,
						new AnnotationPosition(
							new PointF(
								leftMeasurementPos - leftMeasurementMargin,
								advanceBoundsPolygon.Top + advanceBoundsPolygon.Height/2
							),
							HorizontalAlignment.Right,
							VerticalAlignment.Center,
							new PointF(5, 5)
						)
					);
					RenderMeasurement(
						imageProcessingContext,
						dividingLineBounds,
						bottomBearingMeasurement, 
						leftMeasurementPos,
						new AnnotationPosition(
							new PointF(
								leftMeasurementPos - leftMeasurementMargin,
								advanceBoundsPolygon.Top + advanceBoundsPolygon.Height*3/4
							),
							HorizontalAlignment.Right,
							VerticalAlignment.Center,
							new PointF(5, 5)
						)
					);

					var topMeasurementPos = advanceBounds.Top - 50;

					var leftBearingMeasurement = new Measurement(
						Orientation.Horizontal,
						advanceBounds.Left,
						advanceBounds.Left + glyphMetrics.LeftSideBearing * scaleFactor,
						"LeftSideBearing"
					);
					RenderMeasurement(
						imageProcessingContext,
						dividingLineBounds,
						leftBearingMeasurement, 
						topMeasurementPos, 
						new AnnotationPosition(
							new PointF(
								leftBearingMeasurement.Start,
								topMeasurementPos - 30
							),
							HorizontalAlignment.Right,
							VerticalAlignment.Bottom,
							new PointF(-10, 10)
						)
					);

					var widthMeasurement = new Measurement(
						Orientation.Horizontal,
						leftBearingMeasurement.End,
						leftBearingMeasurement.End + glyphMetrics.Width * scaleFactor,
						"Width"
					);
					RenderMeasurement(
						imageProcessingContext,
						dividingLineBounds,
						widthMeasurement, 
						topMeasurementPos + 20,
						new AnnotationPosition(
							new PointF(
								(widthMeasurement.Start + widthMeasurement.End)/2,
								topMeasurementPos - 60
							),
							HorizontalAlignment.Center,
							VerticalAlignment.Bottom,
							new PointF(0, 10)
						)
					);

					var rightSideBearingMeasurement = new Measurement(
						Orientation.Horizontal,
						widthMeasurement.End,
						widthMeasurement.End + glyphMetrics.RightSideBearing * scaleFactor,
						"RightSideBearing"
					);
					RenderMeasurement(
						imageProcessingContext,
						dividingLineBounds,
						rightSideBearingMeasurement,
						topMeasurementPos,
						new AnnotationPosition(
							new PointF(
								rightSideBearingMeasurement.End,
								topMeasurementPos - 30
							),
							HorizontalAlignment.Left,
							VerticalAlignment.Bottom,
							new PointF(10, 10)
						)
					);

					RenderMeasurement(
						imageProcessingContext,
						dividingLineBounds,
						new Measurement(
							Orientation.Vertical,
							advanceBoundsPolygon.Top,
							advanceBoundsPolygon.Bottom,
							nameof(glyphMetrics.AdvanceHeight)
						),
						advanceBoundsPolygon.Right + 50,
						new AnnotationPosition(
							new PointF(advanceBoundsPolygon.Right + 50 + 30, (advanceBoundsPolygon.Top + advanceBoundsPolygon.Bottom)/2),
							HorizontalAlignment.Left,
							VerticalAlignment.Center,
							new PointF(-10, 0)
						)
					);

					RenderMeasurement(
						imageProcessingContext,
						dividingLineBounds,
						new Measurement(
							Orientation.Horizontal,
							advanceBoundsPolygon.Left,
							advanceBoundsPolygon.Right,
							nameof(glyphMetrics.AdvanceWidth)
						),
						advanceBoundsPolygon.Bottom + 50,
						new AnnotationPosition(
							new PointF((advanceBoundsPolygon.Left + advanceBoundsPolygon.Right) / 2, advanceBoundsPolygon.Bottom + 50 + 30),
							HorizontalAlignment.Center,
							VerticalAlignment.Top,
							new PointF(0, -10)
						)
					);

					imageProcessingContext.Draw(new Pen(Color.DarkBlue, 3), advanceBoundsPolygon);
					imageProcessingContext.Draw(new Pen(Color.DarkGreen, 2), new RectangularPolygon(glyphBounds));


					// The left position will be the left-side bearing
					var leftOffset = glyphMetrics.LeftSideBearing * scaleFactor;
					var topOffset = glyphMetrics.TopSideBearing * scaleFactor;



					var pathLine = new[] {
						new PointF(rectangleStart.X + (float)leftOffset, rectangleStart.Y + (float)topOffset),
						new PointF(advanceBounds.Right, rectangleStart.Y + (float)topOffset)
					};
					var path = new PathBuilder().AddLine(
						pathLine[0],
						pathLine[1]
					).Build();
					//imageProcessingContext.DrawLines(new Pen(Color.Red, 30), pathLine);
					imageProcessingContext.Fill(
						new DrawingOptions { GraphicsOptions = { Antialias = true } },
						Brushes.Solid(Color.Black),
						TextBuilder.GenerateGlyphs(text, path, textOptions)
					);
				}
			);
			return imgAll;
		}
	}
}
