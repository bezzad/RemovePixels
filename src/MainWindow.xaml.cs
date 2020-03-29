using Microsoft.Win32;
using RemovePixel.Annotations;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RemovePixel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();

            NamedColors = GetColors();
            LoadImageCommand = new Command(LoadImage, () => true);
            SaveCommand = new Command(Save, () =>
                !string.IsNullOrEmpty(SourceColor) && !string.IsNullOrEmpty(ReplaceColor) && Images?.Any() == true);
            ApplyCommand = new Command(Apply, () =>
                !string.IsNullOrEmpty(SourceColor) && !string.IsNullOrEmpty(ReplaceColor) && Images?.Any() == true);
            DataContext = this;
        }



        public IEnumerable<KeyValuePair<string, System.Windows.Media.Color>> NamedColors { get; }
        public string ReplaceColor { get; set; } = "[Transparent, #00FFFFFF]";
        public string SourceColor { get; set; } = "#cdcec1";
        public Dictionary<string, Bitmap> Images { get; set; }
        public bool UpThreshold { get; set; } = true;
        public bool DownThreshold { get; set; }
        public Command LoadImageCommand { get; set; }
        public Command ApplyCommand { get; set; }
        public Command SaveCommand { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;


        private IEnumerable<KeyValuePair<string, System.Windows.Media.Color>> GetColors()
        {
            return typeof(Colors)
                .GetProperties()
                .Where(prop =>
                    typeof(System.Windows.Media.Color).IsAssignableFrom(prop.PropertyType))
                .Select(prop =>
                    new KeyValuePair<string, System.Windows.Media.Color>(prop.Name, (System.Windows.Media.Color)prop.GetValue(null)));
        }

        protected void LoadImage()
        {
            var ofd = new OpenFileDialog
            {
                Multiselect = true,
                CheckFileExists = true,
                DefaultExt = "Portable Network Graphics (.png)|*.png",
                RestoreDirectory = true,
                Title = "Open image file"
            };

            if (ofd.ShowDialog(this) == true)
            {
                Images = new Dictionary<string, Bitmap>();
                foreach (var fileName in ofd.FileNames)
                {
                    Images[fileName] = CreateNonIndexedImage(new Bitmap(fileName));
                }
                ApplyCommand?.RaiseCanExecuteChanged();
            }
        }
        protected void Apply()
        {
            Cursor = Cursors.Wait;
            var sourceColor = ColorTranslator.FromHtml(SourceColor);
            var targetColor = ColorTranslator.FromHtml(ReplaceColor.Substring(ReplaceColor.IndexOf("#")).Replace("]", ""));

            foreach (var bmp in Images.Values)
            {

                for (var x = 0; x < bmp.Width; x++)
                {
                    for (var y = 0; y < bmp.Height; y++)
                    {
                        var pixel = bmp.GetPixel(x, y);

                        if (!UpThreshold && !DownThreshold &&
                            pixel.A == sourceColor.A &&
                            pixel.R == sourceColor.R &&
                            pixel.G == sourceColor.G &&
                            pixel.B == sourceColor.B)
                        {
                            bmp.SetPixel(x, y, targetColor);
                        }

                        else if (UpThreshold &&
                               pixel.A >= sourceColor.A &&
                               pixel.R >= sourceColor.R &&
                               pixel.G >= sourceColor.G &&
                               pixel.B >= sourceColor.B)
                        {
                            bmp.SetPixel(x, y, targetColor);
                        }

                        else if (DownThreshold &&
                                 pixel.A <= sourceColor.A &&
                                 pixel.R <= sourceColor.R &&
                                 pixel.G <= sourceColor.G &&
                                 pixel.B <= sourceColor.B)
                        {
                            bmp.SetPixel(x, y, targetColor);
                        }
                    }
                }
            }

            SaveCommand?.RaiseCanExecuteChanged();
            Cursor = Cursors.Arrow;
        }

        protected void Save()
        {
            foreach (var img in Images)
            {
                var targetFilename = Path.Combine(Path.GetDirectoryName(img.Key), "Edited",
                    Path.GetFileNameWithoutExtension(img.Key) + "_edited.png");
                img.Value.Save(targetFilename, ImageFormat.Png);
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            ApplyCommand?.RaiseCanExecuteChanged();
        }

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public Bitmap CreateNonIndexedImage(Image src)
        {
            Bitmap newBmp = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics gfx = Graphics.FromImage(newBmp))
            {
                gfx.DrawImage(src, 0, 0);
            }

            return newBmp;
        }
    }
}
