using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NetworkImitator.NetworkComponents;

namespace NetworkImitator.UI
{
    public partial class ComponentControl : UserControl
    {
        private readonly Component _canvas;
        private bool isDragging;

        public ComponentControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ComponentProperty =
            DependencyProperty.Register("Component", typeof(Component), typeof(ComponentControl), new PropertyMetadata(null, OnComponentChanged));

        public Component Component
        {
            get => (Component)GetValue(ComponentProperty);
            set => SetValue(ComponentProperty, value);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Component == null) return;

            Component.IsSelected = !Component.IsSelected;
            isDragging = true;
            CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                //TODO Костыль
                var newPosition = e.GetPosition(null);
                Console.WriteLine("Mouse position: " + newPosition.X + ", " + newPosition.Y);
                Component.X = newPosition.X;
                Component.Y = newPosition.Y;
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            ReleaseMouseCapture();
        }
        
        private static void OnComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComponentControl control && e.NewValue is Component component)
            {
                control.UpdateComponentVisuals(component);
            }
        }

        private void UpdateComponentVisuals(Component component)
        {
            if (component == null) return;

            // Применение изображения или цвета для компонента
            /*ComponentEllipse.Fill = new ImageBrush(component.Image);
            ComponentEllipse.Stroke = component.IsSelected ? Strokes.SelectedStroke : component.GetBrush();*/
        }
    }
}