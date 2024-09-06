using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;
using System.IO;
using ReactiveUI;
using System.Reactive.Linq;

namespace DisplayMeasurements
{
	internal class MainWindowViewModel : ReactiveObject
	{
		readonly ObservableAsPropertyHelper<BitmapSource> _bitmapSource;
		public BitmapSource ImageSource => _bitmapSource.Value;

		public MainWindowViewModel()
		{
			var whenAny = this.WhenAnyValue(vm => vm.Character);
			var observableImage = whenAny.Select(c => ImageGenerator.GenerateImage(c));
			var observableBitmapSource = observableImage.Select(img => ImageSharpWpf.Utils.ConvertToBitmapSource(img));

			_bitmapSource = observableBitmapSource
				.ToProperty(this, vm => vm.ImageSource, out _bitmapSource);
		}

		private char _character = 'a';

		public char Character
		{
			get => _character;
			set => this.RaiseAndSetIfChanged(ref _character, value, nameof(Character));
		}
	}
}
