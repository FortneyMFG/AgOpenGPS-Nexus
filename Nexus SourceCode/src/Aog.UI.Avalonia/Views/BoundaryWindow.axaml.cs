using System;
using Avalonia.Controls;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.Views
{
    /// <summary>
    /// Window for editing field boundaries.
    /// </summary>
    public partial class BoundaryWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoundaryWindow"/> class for XAML loading.
        /// </summary>
        public BoundaryWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundaryWindow"/> class.
        /// </summary>
        public BoundaryWindow(BoundaryToolViewModel viewModel)
            : this()
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }
    }
}
