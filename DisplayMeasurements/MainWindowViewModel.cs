namespace DisplayMeasurements
{
	using System.Linq;
	using System.Windows.Media.Imaging;
	using ReactiveUI;
	using System.Reactive.Linq;

	using static ImageGenerator;
	using static ImageSharpWpf.Utils;

	internal class MainWindowViewModel : ReactiveObject
	{
		readonly ObservableAsPropertyHelper<BitmapSource> _bitmapSource;
		public BitmapSource ImageSource => _bitmapSource.Value;

		public MainWindowViewModel()
		{
			_bitmapSource = this.WhenAnyValue(vm => vm.Character)
				.Select(GenerateImage)
				.Select(ConvertToBitmapSource)
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
