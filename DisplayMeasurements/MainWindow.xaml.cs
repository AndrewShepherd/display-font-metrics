using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DisplayMeasurements
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void WhenTextInput(object sender, TextCompositionEventArgs e)
		{
			var viewModel = this.Resources["MainWindowViewModel"] as MainWindowViewModel;
			if ((viewModel != null) && !string.IsNullOrEmpty(e.Text))
			{
				viewModel.Character = e.Text[0];
			}
		}
	}
}
