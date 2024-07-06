using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Size = System.Drawing.Size;

namespace EarthClock
{
    public partial class MainWindow : Window
    {
        private static readonly string GOESImageHIGHESTUrl = "https://cdn.star.nesdis.noaa.gov/GOES16/ABI/FD/GEOCOLOR/latest.jpg";
        private static readonly string GOESImageHIGHUrl = "https://cdn.star.nesdis.noaa.gov/GOES16/ABI/FD/GEOCOLOR/5424x5424.jpg";
        private static readonly string GOESImageMEDUrl = "https://cdn.star.nesdis.noaa.gov/GOES16/ABI/FD/GEOCOLOR/1808x1808.jpg";
        private static readonly string GOESImageSMALLUrl = "https://cdn.star.nesdis.noaa.gov/GOES16/ABI/FD/GEOCOLOR/678x678.jpg";
        private static readonly string GOESImageSMALLESTUrl = "https://cdn.star.nesdis.noaa.gov/GOES16/ABI/FD/GEOCOLOR/339x339.jpg";
        private static string GOESImageUrl = GOESImageMEDUrl;
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly DispatcherTimer earthUpdateTimer;
        private bool isDragging;
        private Point mouseStartPosition;

        public MainWindow()
        {
            InitializeComponent();
            earthUpdateTimer = new DispatcherTimer();
            earthUpdateTimer.Interval = TimeSpan.FromMinutes(15); // Update every 15 minutes
            earthUpdateTimer.Tick += Timer_Tick;
            earthUpdateTimer.Start();

            WaitText.Text = "Getting latest Earth image...";
            _ = UpdateImageAsync();

            MainGrid.MouseLeftButtonDown += Image_MouseLeftButtonDown;
            MainGrid.MouseLeftButtonUp += Image_MouseLeftButtonUp;
            MainGrid.MouseMove += Image_MouseMove;
            MainGrid.MouseWheel += Image_MouseWheel;
            MainGrid.MouseLeave += (s, e) => { isDragging = false; };
            
        }

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.Height += e.Delta/20;
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            WaitText.Text = "Getting latest Earth image...";
            await UpdateImageAsync();
        }
        private Task FadeToOpacityAsync(UIElement element, double toOpacity, TimeSpan duration)
        {
            var tcs = new TaskCompletionSource<bool>();
            var animation = new DoubleAnimation
            {
                To = toOpacity,
                Duration = duration,
                FillBehavior = FillBehavior.HoldEnd
            };
            animation.Completed += (s, e) =>
            {
                element.Opacity = toOpacity;
                MainBorder.Opacity = 1- toOpacity;
                tcs.SetResult(true);
            };
            element.BeginAnimation(UIElement.OpacityProperty, animation);
            return tcs.Task;
        }

        private async Task UpdateImageAsync()
        {
            try
            {
                MainGrid.Opacity = 1;
                await FadeToOpacityAsync(GOESImage,0,TimeSpan.FromMilliseconds(1500));

                var imageBytes = await httpClient.GetByteArrayAsync(GOESImageUrl);
                using (var stream = new System.IO.MemoryStream(imageBytes))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    GOESImage.Source = bitmap;
                }

                await FadeToOpacityAsync(GOESImage, 1, TimeSpan.FromMilliseconds(1500));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            mouseStartPosition = e.GetPosition(this);
            GOESImage.CaptureMouse();
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            GOESImage.ReleaseMouseCapture();
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && MainGrid.IsMouseOver)
            {
                Point currentMousePosition = e.GetPosition(this);
                double offsetX = currentMousePosition.X - mouseStartPosition.X;
                double offsetY = currentMousePosition.Y - mouseStartPosition.Y;

                this.Left += offsetX;
                this.Top += offsetY;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Background = Brushes.Transparent;
            this.Width = this.Height;

            string oldURL = GOESImageUrl;
            switch(this.Width)
            {
                case double n when (n >= 5424):
                    GOESImageUrl = GOESImageHIGHESTUrl;
                    break;
                case double n when (n < 5424 && n >= 1808):
                    GOESImageUrl = GOESImageHIGHUrl;
                    break;
                case double n when (n < 1808 && n >= 678):
                    GOESImageUrl = GOESImageMEDUrl;
                    break;
                case double n when (n < 678 && n >= 339):
                    GOESImageUrl = GOESImageSMALLUrl;
                    break;
                case double n when (n < 339):
                    GOESImageUrl = GOESImageSMALLESTUrl;
                    break;
            }

            if (GOESImageUrl != oldURL)
            {
                WaitText.Text = "Resizing...";
                _ = UpdateImageAsync();
                
            }
                
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
