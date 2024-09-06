namespace ImageSharpWpf
{
	using SixLabors.ImageSharp;
	using System.IO;
	using System.Windows.Media.Imaging;

	public class Utils
	{
		public static BitmapSource ConvertToBitmapSource(Image img)
		{
			MemoryStream bitmapStream = new MemoryStream();
			img.SaveAsBmp(bitmapStream);

			bitmapStream.Seek(0, SeekOrigin.Begin);
			var source = new BitmapImage();
			source.BeginInit();
			source.StreamSource = bitmapStream;
			source.EndInit();
			return source;
		}
	}
}