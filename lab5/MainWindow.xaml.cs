using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace lab5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IList<((int, int), (int, int))> coordinates;
        private IList<(double, double)> polygonCoordinates;
        private double pointSize = 5;
        private int gridSize = 50;
        private double zoomMax = 5;
        private double zoomMin = 0.5;
        private double zoomSpeed = 0.001;
        private double zoom = 1;
        private bool isPolygon;

        public MainWindow()
        {
            coordinates = new List<((int, int), (int, int))>();
            polygonCoordinates = new List<(double, double)>();
            InitializeComponent();
        }

        private void SelectFile(object sender, RoutedEventArgs e)
        {
            string filename = null;
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files|*.txt"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                filename = openFileDialog.FileName;
            }
            if (string.IsNullOrEmpty(filename))
            {
                return;
            }

            using (TextReader reader = File.OpenText(filename))
            {
                try
                {
                    coordinates.Clear();
                    polygonCoordinates.Clear();
                    int n = int.Parse(reader.ReadLine());
                    if (n < 0)
                    {
                        n = -n;
                        isPolygon = true;
                    }
                    else
                    {
                        isPolygon = false;
                    }
                    for (int i = 0; i <= n; i++)
                    {
                        string text = reader.ReadLine();
                        string[] c = text.Split(' ');
                        int x1 = int.Parse(c[0]);
                        int y1 = int.Parse(c[1]);
                        int x2 = int.Parse(c[2]);
                        int y2 = int.Parse(c[3]);
                        coordinates.Add(((x1, y1), (x2, y2)));
                    }
                }
                catch
                {
                    MessageBox.Show("Неверный формат файла. Требуемый формат:\n*число отрезков*" +
                        "\nX1_1 Y1_1 X2_1 Y2_1" +
                        "\nX1_2 Y1_2 X2_2 Y2_2" +
                        "\nX1_n Y1_n X2_n Y2_n * координаты отрезков *" +
                        "\nXmin Ymin Xmax Ymax * координаты отсекающего прямоугольного окна * ",
                        "Wrong file format");
                }
                PaintGrid();
            }
        }

        private void ZoomCanvas(object sender, MouseWheelEventArgs e)
        {
            zoom += zoomSpeed * e.Delta;
            if (zoom < zoomMin) { zoom = zoomMin; }
            if (zoom > zoomMax) { zoom = zoomMax; }

            Point mousePos = e.GetPosition(canvas);

            if (zoom > 1)
            {
                canvas.RenderTransform = new ScaleTransform(zoom, zoom, mousePos.X, mousePos.Y);
            }
            else
            {
                canvas.RenderTransform = new ScaleTransform(zoom, zoom);
            }
        }

        public void PaintGrid()
        {
            canvas.Children.Clear();
            Line xLine = new Line();
            Line yLine = new Line();
            xLine.Stroke = Brushes.Red;
            yLine.Stroke = Brushes.Red;
            xLine.X1 = 0;
            xLine.X2 = canvas.Width;
            xLine.Y1 = canvas.Height / 2;
            xLine.Y2 = xLine.Y1;
            yLine.X1 = canvas.Width / 2;
            yLine.X2 = yLine.X1;
            yLine.Y1 = 0;
            yLine.Y2 = canvas.Height;
            yLine.StrokeThickness = 2;
            canvas.Children.Add(xLine);
            canvas.Children.Add(yLine);

            for (int i = -gridSize; i <= gridSize; i += 10)
            {
                TextBlock xTextBlock = new TextBlock();
                TextBlock yTextBlock = new TextBlock();
                xTextBlock.FontSize = 10;
                yTextBlock.FontSize = 10;
                xTextBlock.Text = i.ToString();
                xTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                yTextBlock.Text = i.ToString();
                yTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                Canvas.SetLeft(xTextBlock, (i + gridSize) * pointSize);
                Canvas.SetTop(xTextBlock, 0);
                Canvas.SetLeft(yTextBlock, 0);
                Canvas.SetTop(yTextBlock, canvas.Height - (i + gridSize) * pointSize);
                canvas.Children.Add(xTextBlock);
                canvas.Children.Add(yTextBlock);
            }

            DrawSegments();
            if (isPolygon)
            {
                DrawPolygon();
            }
            else
            {
                DrawCutSegments();
            }
        }

        private void DrawSegments()
        {
            for (int i = 0; i < coordinates.Count - 1; i++)
            {
                Line line = new Line();
                line.Stroke = Brushes.Blue;
                line.X1 = coordinates[i].Item1.Item1 * pointSize + canvas.Width / 2;
                line.X2 = coordinates[i].Item2.Item1 * pointSize + canvas.Width / 2;
                line.Y1 = -coordinates[i].Item1.Item2 * pointSize + canvas.Height / 2;
                line.Y2 = -coordinates[i].Item2.Item2 * pointSize + canvas.Height / 2;
                canvas.Children.Add(line);
            }
            int k = coordinates.Count - 1;
            Rectangle rectangle = new Rectangle();
            rectangle.Stroke = Brushes.Yellow;
            rectangle.Width = (coordinates[k].Item2.Item1 - coordinates[k].Item1.Item1) * pointSize;
            rectangle.Height = (coordinates[k].Item2.Item2 - coordinates[k].Item1.Item2) * pointSize;
            canvas.Children.Add(rectangle);
            Canvas.SetTop(rectangle, -coordinates[k].Item2.Item2 * pointSize + canvas.Height / 2);
            Canvas.SetLeft(rectangle, coordinates[k].Item1.Item1 * pointSize + canvas.Width / 2);
        }

        private void DrawPolygon()
        {
            double xMin = coordinates[coordinates.Count - 1].Item1.Item1;
            double yMin = coordinates[coordinates.Count - 1].Item1.Item2;
            double xMax = coordinates[coordinates.Count - 1].Item2.Item1;
            double yMax = coordinates[coordinates.Count - 1].Item2.Item2;
            IList<((double, double), (double, double))> c = new List<((double, double), (double, double))>();

            for (int i = 0; i < coordinates.Count - 1; i++)
            {
                c.Add(coordinates[i]);
            }

            for (int i = 0; i < c.Count; i++)
            {
                if (c[i].Item1.Item1 >= xMin && c[i].Item2.Item1 >= xMin)
                {
                    polygonCoordinates.Add(c[i].Item1);
                    polygonCoordinates.Add(c[i].Item2);
                }
                else if (c[i].Item1.Item1 >= xMin && c[i].Item2.Item1 < xMin) {
                    (double, double) a = c[i].Item1;
                    (double, double) b = c[i].Item2;
                    while (Math.Abs(b.Item1 - xMin) > 0.2)
                    {
                        double x = (a.Item1 + b.Item1) / 2;
                        double y = (a.Item2 + b.Item2) / 2;
                        if (x < xMin)
                        {
                            b = (x, y);
                        }
                        else
                        {
                            a = (x, y);
                        }
                    }
                    polygonCoordinates.Add((xMin, b.Item2));
                }
                else if (c[i].Item1.Item1 < xMin && c[i].Item2.Item1 >= xMin) {
                    (double, double) a = c[i].Item1;
                    (double, double) b = c[i].Item2;
                    while (Math.Abs(a.Item1 - xMin) > 0.2)
                    {
                        double x = (a.Item1 + b.Item1) / 2;
                        double y = (a.Item2 + b.Item2) / 2;
                        if (x < xMin)
                        {
                            a = (x, y);
                        }
                        else
                        {
                            b = (x, y);
                        }
                    }
                    polygonCoordinates.Add((xMin, a.Item2));
                }
            }

            c.Clear();
            for(int i = 0; i < polygonCoordinates.Count - 1; i++)
            {
                c.Add((polygonCoordinates[i], polygonCoordinates[i + 1]));
            }
            c.Add((polygonCoordinates[polygonCoordinates.Count - 1], polygonCoordinates[0]));
            polygonCoordinates.Clear();

            for (int i = 0; i < c.Count; i++)
            {
                if (c[i].Item1.Item2 >= yMin && c[i].Item2.Item2 >= yMin)
                {
                    polygonCoordinates.Add(c[i].Item1);
                    polygonCoordinates.Add(c[i].Item2);
                }
                else if (c[i].Item1.Item2 >= yMin && c[i].Item2.Item2 < yMin) {
                    (double, double) a = c[i].Item1;
                    (double, double) b = c[i].Item2;
                    while (Math.Abs(b.Item2 - yMin) > 0.2)
                    {
                        double x = (a.Item1 + b.Item1) / 2;
                        double y = (a.Item2 + b.Item2) / 2;
                        if (y < yMin)
                        {
                            b = (x, y);
                        }
                        else
                        {
                            a = (x, y);
                        }
                    }
                    polygonCoordinates.Add((b.Item1, yMin));
                }
                else if (c[i].Item1.Item2 < yMin && c[i].Item2.Item2 >= yMin) {
                    (double, double) a = c[i].Item1;
                    (double, double) b = c[i].Item2;
                    while (Math.Abs(a.Item2 - yMin) > 0.2)
                    {
                        double x = (a.Item1 + b.Item1) / 2;
                        double y = (a.Item2 + b.Item2) / 2;
                        if (y < yMin)
                        {
                            a = (x, y);
                        }
                        else
                        {
                            b = (x, y);
                        }
                    }
                    polygonCoordinates.Add((a.Item1, yMin));
                }
            }

            c.Clear();
            for(int i = 0; i < polygonCoordinates.Count - 1; i++)
            {
                c.Add((polygonCoordinates[i], polygonCoordinates[i + 1]));
            }
            c.Add((polygonCoordinates[polygonCoordinates.Count - 1], polygonCoordinates[0]));
            polygonCoordinates.Clear();

            for (int i = 0; i < c.Count; i++)
            {
                if (c[i].Item1.Item1 <= xMax && c[i].Item2.Item1 <= xMax)
                {
                    polygonCoordinates.Add(c[i].Item1);
                    polygonCoordinates.Add(c[i].Item2);
                }
                else if (c[i].Item1.Item1 <= xMax && c[i].Item2.Item1 > xMax)
                {
                    (double, double) a = c[i].Item1;
                    (double, double) b = c[i].Item2;
                    while (Math.Abs(b.Item1 - xMax) > 0.2)
                    {
                        double x = (a.Item1 + b.Item1) / 2;
                        double y = (a.Item2 + b.Item2) / 2;
                        if (x > xMax)
                        {
                            b = (x, y);
                        }
                        else
                        {
                            a = (x, y);
                        }
                    }
                    polygonCoordinates.Add((xMax, b.Item2));
                }
                else if (c[i].Item1.Item1 > xMax && c[i].Item2.Item1 <= xMax)
                {
                    (double, double) a = c[i].Item1;
                    (double, double) b = c[i].Item2;
                    while (Math.Abs(a.Item1 - xMax) > 0.2)
                    {
                        double x = (a.Item1 + b.Item1) / 2;
                        double y = (a.Item2 + b.Item2) / 2;
                        if (x > xMax)
                        {
                            a = (x, y);
                        }
                        else
                        {
                            b = (x, y);
                        }
                    }
                    polygonCoordinates.Add((xMax, a.Item2));
                }
            }

            c.Clear();
            for (int i = 0; i < polygonCoordinates.Count - 1; i++)
            {
                c.Add((polygonCoordinates[i], polygonCoordinates[i + 1]));
            }
            c.Add((polygonCoordinates[polygonCoordinates.Count - 1], polygonCoordinates[0]));
            polygonCoordinates.Clear();

            for (int i = 0; i < c.Count; i++)
            {
                if (c[i].Item1.Item2 <= yMax && c[i].Item2.Item2 <= yMax)
                {
                    polygonCoordinates.Add(c[i].Item1);
                    polygonCoordinates.Add(c[i].Item2);
                }
                else if (c[i].Item1.Item2 <= yMax && c[i].Item2.Item2 > yMax)
                {
                    (double, double) a = c[i].Item1;
                    (double, double) b = c[i].Item2;
                    while (Math.Abs(b.Item2 - yMax) > 0.2)
                    {
                        double x = (a.Item1 + b.Item1) / 2;
                        double y = (a.Item2 + b.Item2) / 2;
                        if (y > yMax)
                        {
                            b = (x, y);
                        }
                        else
                        {
                            a = (x, y);
                        }
                    }
                    polygonCoordinates.Add((b.Item1, yMax));
                }
                else if (c[i].Item1.Item2 > yMax && c[i].Item2.Item2 <= yMax)
                {
                    (double, double) a = c[i].Item1;
                    (double, double) b = c[i].Item2;
                    while (Math.Abs(a.Item2 - yMax) > 0.2)
                    {
                        double x = (a.Item1 + b.Item1) / 2;
                        double y = (a.Item2 + b.Item2) / 2;
                        if (y > yMax)
                        {
                            a = (x, y);
                        }
                        else
                        {
                            b = (x, y);
                        }
                    }
                    polygonCoordinates.Add((a.Item1, yMax));
                }
            }

            for (int i = 0; i < polygonCoordinates.Count - 1; i++)
            {
                Line l = new Line();
                l.Stroke = Brushes.Green;
                l.X1 = polygonCoordinates[i].Item1 * pointSize + canvas.Width / 2;
                l.X2 = polygonCoordinates[i + 1].Item1 * pointSize + canvas.Width / 2;
                l.Y1 = -polygonCoordinates[i].Item2 * pointSize + canvas.Height / 2;
                l.Y2 = -polygonCoordinates[i + 1].Item2 * pointSize + canvas.Height / 2;
                canvas.Children.Add(l);
            }

            Line line = new Line();
            line.Stroke = Brushes.Green;
            line.X1 = polygonCoordinates[polygonCoordinates.Count - 1].Item1 * pointSize + canvas.Width / 2;
            line.X2 = polygonCoordinates[0].Item1 * pointSize + canvas.Width / 2;
            line.Y1 = -polygonCoordinates[polygonCoordinates.Count - 1].Item2 * pointSize + canvas.Height / 2;
            line.Y2 = -polygonCoordinates[0].Item2 * pointSize + canvas.Height / 2;
            canvas.Children.Add(line);
        }

        private void DrawCutSegments()
        {
            for (int i = 0; i < coordinates.Count - 1; i++)
            {
                DrawCutSegment(coordinates[i]);              
            }
        }

        private void DrawCutSegment(((double, double), (double, double)) c)
        {
            if (Math.Sqrt((c.Item2.Item1 - c.Item1.Item1) * (c.Item2.Item1 - c.Item1.Item1)
                + (c.Item2.Item2 - c.Item1.Item2) * (c.Item2.Item2 - c.Item1.Item2)) < 0.2)
            {
                return;
            }
            if (isOutside(c))
            {
                return;
            }
            if (isInside(c))
            {                             
                Line line = new Line();
                line.Stroke = Brushes.Green;
                line.X1 = c.Item1.Item1 * pointSize + canvas.Width / 2;
                line.X2 = c.Item2.Item1 * pointSize + canvas.Width / 2;
                line.Y1 = -c.Item1.Item2 * pointSize + canvas.Height / 2;
                line.Y2 = -c.Item2.Item2 * pointSize + canvas.Height / 2;
                canvas.Children.Add(line);
                return;
            }

            (double, double) center 
                = ((c.Item2.Item1 + c.Item1.Item1) / 2, (c.Item2.Item2 + c.Item1.Item2) / 2);
            DrawCutSegment((c.Item1, center));
            DrawCutSegment((center, c.Item2));
        }

        private bool isInside(((double, double), (double, double)) c)
        {
            double xMin = coordinates[coordinates.Count - 1].Item1.Item1;
            double yMin = coordinates[coordinates.Count - 1].Item1.Item2;
            double xMax = coordinates[coordinates.Count - 1].Item2.Item1;
            double yMax = coordinates[coordinates.Count - 1].Item2.Item2;

            return c.Item1.Item1 <= xMax && c.Item1.Item1 >= xMin
                && c.Item1.Item2 <= yMax && c.Item1.Item2 >= yMin
                && c.Item2.Item1 <= xMax && c.Item2.Item1 >= xMin
                && c.Item2.Item2 <= yMax && c.Item2.Item2 >= yMin;
        }

        private bool isOutside(((double, double), (double, double)) c)
        {
            double xMin = coordinates[coordinates.Count - 1].Item1.Item1;
            double yMin = coordinates[coordinates.Count - 1].Item1.Item2;
            double xMax = coordinates[coordinates.Count - 1].Item2.Item1;
            double yMax = coordinates[coordinates.Count - 1].Item2.Item2;

            return (c.Item1.Item1 < xMin && c.Item2.Item1 < xMin)
                || (c.Item1.Item1 > xMax && c.Item2.Item1 > xMax)
                || (c.Item1.Item2 > yMax && c.Item2.Item2 > yMax)
                || (c.Item1.Item2 < yMin && c.Item2.Item2 < yMin);
        }
    }
}
