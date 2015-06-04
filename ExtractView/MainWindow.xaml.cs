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
using System.IO;
using AFilter = AForge.Imaging.Filters;
using Xceed.Wpf.AvalonDock;
using Drawing = System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.ObjectModel;

namespace WpfApplication1
{
    public class ViewData
    {
        public int ID { get; set;}
        public int X { get; set;}
        public int Y { get; set;}
        public int wd { get; set;}
        public int ht { get; set;}
    }
    public partial class MainWindow : Window
    {
        Point? lastCenterPositionOnTarget;
        Point? lastMousePositionOnTarget;
        Point? lastDragPoint;
        double DpiRatio;
        BitmapImage bmpi;
        System.Drawing.Bitmap bmp;
        ObservableCollection<ViewData> _ViewList= new ObservableCollection<ViewData>();
        public ObservableCollection<ViewData> ViewList {get {return _ViewList;} }
        int nID=1;
    
        public MainWindow()
        {
            InitializeComponent();
            scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            scrollViewer.MouseLeftButtonUp += OnMouseLeftButtonUp;
            scrollViewer.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
            scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
            scrollViewer.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            scrollViewer.MouseMove += OnMouseMove;
            slider.ValueChanged += OnSliderValueChanged;
            imagefile.Text=@"F:\Cel files\57975.tif"; //set default values
            t1.Text=(348).ToString(); t2.Text=(260).ToString();
            f1.Text=(1392).ToString(); f2.Text=(1040).ToString(); 
        }

        private void loadImage(object sender, RoutedEventArgs e)
        {
            bmpi = new BitmapImage();
            bmpi.BeginInit();
            bmpi.UriSource = new Uri(imagefile.Text, UriKind.RelativeOrAbsolute);
            bmpi.EndInit();
            canvImage.Source = bmpi;
            bmp=BitmapImage2Bitmap(bmpi);
            DpiRatio = bmpi.DpiX/96.0d;

        }
        void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.X)) // Add single view
            {
                   addView(null,null);
            }
            if (Keyboard.IsKeyDown(Key.Y)) // Add Overview
            {
                exportOverview(null,null);
                bmpi.UriSource = null; canvImage.Source = null;
                bmp.Dispose(); MagView.Image = null;
                myCanvas.Children.Clear();
                _ViewList.Clear();
                //this.Close();
            }
        }

        private void exportOverview(object sender, RoutedEventArgs e)
        {
            //SnapShotPNG(scrollViewer,new Uri(@"F:\out.png", UriKind.Absolute),1);
            var element = scrollViewer;
            var target = new RenderTargetBitmap(
                (int)element.RenderSize.Width, (int)element.RenderSize.Height,
                (int)bmpi.DpiX, (int)bmpi.DpiY, PixelFormats.Pbgra32);
            target.Render(element);

            var encoder = new PngBitmapEncoder();
            var outputFrame = BitmapFrame.Create(target);
            encoder.Frames.Add(outputFrame);

            using (var file = File.OpenWrite(System.IO.Path.GetDirectoryName(imagefile.Text) + "\\" + System.IO.Path.GetFileNameWithoutExtension(imagefile.Text) + "_overview.jpe"))
            {
                encoder.Save(file);
            }
        }
        void imagefile_PreviewDragOver(object sender, DragEventArgs e) { e.Handled = true; }
        void imagefile_DragEnter(object sender, DragEventArgs e) {
            if (!e.Data.GetDataPresent("myFormat") || sender == e.Source)
                { e.Effects = DragDropEffects.Copy; } 
        }
        void imagefile_Drop(object sender, DragEventArgs e) {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            imagefile.Text=files[0]; // take first one if multiple files
            loadImage(null,null);
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (lastDragPoint.HasValue)
            {
                Point posNow = e.GetPosition(scrollViewer);
                double dX = posNow.X - lastDragPoint.Value.X;
                double dY = posNow.Y - lastDragPoint.Value.Y;
                lastDragPoint = posNow;
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - dX);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - dY);
            }
            if (bmp != null && canvImage.Source != null)
            { 
                Point p = Mouse.GetPosition(myCanvas);
                label.Content = p.X.ToString("0") + "," + p.Y.ToString("0") + " | " + (p.X * DpiRatio).ToString("0") + "," + (p.Y * DpiRatio).ToString("0");
                vID.Content=nID.ToString(); vXC.Content=(p.X).ToString("0"); vYC.Content=(p.Y).ToString("0");
                vX.Content=(p.X * DpiRatio).ToString("0"); vY.Content=(p.Y * DpiRatio).ToString("0");
                AFilter.Crop magcrop = new AFilter.Crop( new System.Drawing.Rectangle(int.Parse(vX.Content.ToString()),int.Parse(vY.Content.ToString()),
                    int.Parse(t1.Text), int.Parse(t2.Text)));
                //AFilter.ResizeBicubic magresize = new AFilter.ResizeBicubic(int.Parse(f1.Text),int.Parse(f2.Text));
                System.Drawing.Bitmap newImage = magcrop.Apply(bmp);
                MagView.Image=newImage;
            }
        }

        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(scrollViewer);
            if (mousePos.X <= scrollViewer.ViewportWidth && mousePos.Y <
                scrollViewer.ViewportHeight) //make sure we still can use the scrollbars
            {
                scrollViewer.Cursor = Cursors.SizeAll;
                lastDragPoint = mousePos;
                Mouse.Capture(scrollViewer);
                
            }
        }

        void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            lastMousePositionOnTarget = Mouse.GetPosition(myCanvas);
            if (e.Delta > 0) { slider.Value += 1; }
            if (e.Delta < 0) { slider.Value -= 1; }
            e.Handled = true;
        }

        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            scrollViewer.Cursor = Cursors.Arrow;
            scrollViewer.ReleaseMouseCapture();
            lastDragPoint = null;
        }

        void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            scaleTransform.ScaleX = e.NewValue;
            scaleTransform.ScaleY = e.NewValue;
            var centerOfViewport = new Point(scrollViewer.ViewportWidth / 2, scrollViewer.ViewportHeight / 2);
            lastCenterPositionOnTarget = scrollViewer.TranslatePoint(centerOfViewport, myCanvas);
        }

        void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                Point? targetBefore = null;
                Point? targetNow = null;

                if (!lastMousePositionOnTarget.HasValue)
                {
                    if (lastCenterPositionOnTarget.HasValue)
                    {
                        var centerOfViewport = new Point(scrollViewer.ViewportWidth / 2, scrollViewer.ViewportHeight / 2);
                        Point centerOfTargetNow = scrollViewer.TranslatePoint(centerOfViewport, myCanvas);
                        targetBefore = lastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                } else
                {
                    targetBefore = lastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(myCanvas);
                    lastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue)
                {
                    double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                    double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;
                    double multiplicatorX = e.ExtentWidth / myCanvas.Width;
                    double multiplicatorY = e.ExtentHeight / myCanvas.Height;
                    double newOffsetX = scrollViewer.HorizontalOffset - dXInTargetPixels * multiplicatorX;
                    double newOffsetY = scrollViewer.VerticalOffset - dYInTargetPixels * multiplicatorY;
                    if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY)) { return; }
                    scrollViewer.ScrollToHorizontalOffset(newOffsetX);
                    scrollViewer.ScrollToVerticalOffset(newOffsetY);
                }
            }
        }
        public void SnapShotPNG(UIElement source, Uri destination, int zoom)
        {
            try
            {
                double actualHeight = source.DesiredSize.Height;
                double actualWidth = source.DesiredSize.Width;
                double renderHeight = actualHeight * zoom;
                double renderWidth = actualWidth * zoom;

                RenderTargetBitmap renderTarget = new RenderTargetBitmap((int)renderWidth, (int)renderHeight, 96, 96, PixelFormats.Pbgra32);
                VisualBrush sourceBrush = new VisualBrush(source);

                DrawingVisual drawingVisual = new DrawingVisual();
                DrawingContext drawingContext = drawingVisual.RenderOpen();

                using (drawingContext)
                {
                    drawingContext.PushTransform(new ScaleTransform(zoom, zoom));
                    drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0), new Point(actualWidth, actualHeight)));
                }
                renderTarget.Render(drawingVisual);

                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));
                using (FileStream stream = new FileStream(destination.LocalPath, FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(stream);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
        private System.Drawing.Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {       
            using(MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);
                return new System.Drawing.Bitmap(bitmap);
            }
        }

        private void addView(object sender, RoutedEventArgs e)
        {
                Rectangle r1 = new Rectangle();
                r1.Stroke = new SolidColorBrush(Colors.Black);
                r1.StrokeThickness=10;
                try { r1.Width = int.Parse(t1.Text)/DpiRatio; r1.Height = int.Parse(t2.Text)/DpiRatio; }
                catch { r1.Width = 300; r1.Height = 300; }
                Canvas.SetLeft(r1, int.Parse(vXC.Content.ToString())); Canvas.SetTop(r1, int.Parse(vYC.Content.ToString()));
                myCanvas.Children.Add(r1);
                TextBlock textBlock = new TextBlock();
                textBlock.Text = nID.ToString("0"); //textBlock.Foreground = new SolidColorBrush(color);
                textBlock.FontSize=int.Parse(t2.Text.ToString());
                Canvas.SetLeft(textBlock, int.Parse(vXC.Content.ToString())); Canvas.SetTop(textBlock, int.Parse(vYC.Content.ToString()));
                myCanvas.Children.Add(textBlock);
                AFilter.Crop magcrop = new AFilter.Crop( new System.Drawing.Rectangle(int.Parse(vX.Content.ToString()),int.Parse(vY.Content.ToString()), (int)(r1.Width*DpiRatio), (int)(r1.Height*DpiRatio)));
                //AFilter.ResizeBicubic magresize = new AFilter.ResizeBicubic(int.Parse(f1.Text),int.Parse(f2.Text));
                System.Drawing.Bitmap newImage = magcrop.Apply(bmp);
            if (chk_resize.IsChecked==true && f1.Text!=t1.Text && f2.Text!=t2.Text) {
                Drawing.Bitmap resizedImage = new Drawing.Bitmap(int.Parse(f1.Text),int.Parse(f2.Text));
                using (var graphics = System.Drawing.Graphics.FromImage(resizedImage))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(newImage, 0, 0, resizedImage.Width,resizedImage.Height);                   
                }
                //newImage.Save(@"F:\TestMagnifiedS.jpg",Drawing.Imaging.ImageFormat.Jpeg);
                MagView.Image=resizedImage;
                resizedImage.Save(System.IO.Path.GetDirectoryName(imagefile.Text)+"\\"+System.IO.Path.GetFileNameWithoutExtension(imagefile.Text)
                    +"_"+nID.ToString()+".jpg",Drawing.Imaging.ImageFormat.Jpeg);
            } else {
                MagView.Image = newImage;
                newImage.Save(System.IO.Path.GetDirectoryName(imagefile.Text) + "\\" + System.IO.Path.GetFileNameWithoutExtension(imagefile.Text)
                    + "_" + nID.ToString() + ".jpg", Drawing.Imaging.ImageFormat.Jpeg);
            }
                _ViewList.Add(new ViewData { ID=nID, X=int.Parse(vX.Content.ToString()), Y=int.Parse(vY.Content.ToString()),
                                wd=int.Parse(t1.Text), ht=int.Parse(t2.Text)});
                nID++;
        }

        
    }
}