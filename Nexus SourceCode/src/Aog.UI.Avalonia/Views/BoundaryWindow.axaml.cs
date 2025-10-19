using System;
using Avalonia.Controls;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Aog.UI.Avalonia.Views
{
    /// <summary>
    /// Window for editing field boundaries.
    /// </summary>
    public partial class BoundaryWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoundaryWindow"/> class.
        /// </summary>
        public BoundaryWindow()
            : this(CreateDefaultViewModel())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundaryWindow"/> class.
        /// </summary>
        public BoundaryWindow(BoundaryToolViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }

        private static BoundaryToolViewModel CreateDefaultViewModel()
        {
            if (AvaloniaServiceProviderAccessor.TryGetServiceProvider(out var services))
            {
                var host = services.GetService<MainWindowViewModel>();
                if (host is not null)
                {
                    return host.CreateBoundaryToolViewModel();
                }
            }

            return BoundaryToolViewModel.CreateSample();
        }
    }
}
