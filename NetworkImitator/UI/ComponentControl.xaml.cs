using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NetworkImitator.NetworkComponents;

namespace NetworkImitator.UI
{
    public partial class ComponentControl : UserControl
    {
        private bool _isDragging;
        private MainViewModel _mainViewModel => Component.MainViewModel;

        public ComponentControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ComponentProperty =
            DependencyProperty.Register(nameof(Component), typeof(Component), typeof(ComponentControl));

        public Component Component
        {
            get => (Component)GetValue(ComponentProperty);
            set => SetValue(ComponentProperty, value);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mainViewModel.SelectVertex(Component);
            _isDragging = true;
            CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _mainViewModel.SelectedComponent != Component)
            {
                //Извне перестали считвать компонент как выделенный 
                _isDragging = false;
            }

            if (_isDragging)
            {
                //TODO Костыль, работающий только при Grid=0
                var newPosition = e.GetPosition(null);
                Component.X = newPosition.X - Width / 2;
                Component.Y = newPosition.Y - Height / 2;
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ReleaseMouseCapture();
        }
    }
}