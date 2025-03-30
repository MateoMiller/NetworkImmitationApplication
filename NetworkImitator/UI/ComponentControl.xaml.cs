using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NetworkImitator.NetworkComponents;

namespace NetworkImitator.UI
{
    public partial class ComponentControl : UserControl
    {
        private bool isDragging;
        public MainViewModel _mainViewModel => Component.MainViewModel;

        public ComponentControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ComponentProperty =
            DependencyProperty.Register("Component", typeof(Component), typeof(ComponentControl));

        public Component Component
        {
            get => (Component)GetValue(ComponentProperty);
            set => SetValue(ComponentProperty, value);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mainViewModel.SelectVertex(Component);
            isDragging = true;
            CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && _mainViewModel.SelectedComponent != Component)
            {
                //Извне перестали считвать компонент как выделенный 
                isDragging = false;
            }

            if (isDragging)
            {
                //TODO Костыль, работающий только при Grid=0
                var newPosition = e.GetPosition(null);
                Component.X = newPosition.X - Width / 2;
                Component.Y = newPosition.Y - Height / 2;
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            ReleaseMouseCapture();
        }
    }
}