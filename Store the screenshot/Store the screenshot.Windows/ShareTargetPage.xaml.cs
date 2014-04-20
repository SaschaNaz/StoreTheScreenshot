using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Store_the_screenshot
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ShareTargetPage : Page
    {
        private ShareOperation shareOperation;
        private RandomAccessStreamReference sharedBitmapStream;

        public ShareTargetPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            shareOperation = (ShareOperation)e.Parameter;
            var test = shareOperation.Data.AvailableFormats;
            sharedBitmapStream = await shareOperation.Data.GetBitmapAsync();
            IRandomAccessStreamWithContentType bitmapStream = await sharedBitmapStream.OpenReadAsync();

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.SetSource(bitmapStream);
            ImageHolder.Source = bitmapImage;
        }

        private async void StoreClick(object sender, RoutedEventArgs e)
        {
            ProgressArea.Visibility = Visibility.Visible;
            StorageFolder savedPicturesFolder = KnownFolders.SavedPictures;
            var screenshotFolder = await savedPicturesFolder.GetFolderAsync("Screenshots");//temporary way
            var screenshots = await screenshotFolder.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.OrderByName);
            Int32 lastIndex = 0;
            Regex regex = new Regex(@"^Screenshot \(([0-9]+)\).png$");
            foreach (var file in screenshots)
            {
                var match = regex.Match(file.Name);
                if (match.Groups.Count < 2)
                    continue;
                var index = Int32.Parse(match.Groups[1].Value);
                if (index == lastIndex + 1)
                    lastIndex = index;
                else
                    break;
            }
            var saveName = String.Format("Screenshot ({0}).png", lastIndex + 1);
            var saveFile = await screenshotFolder.CreateFileAsync(saveName);
            using (var stream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(await sharedBitmapStream.OpenReadAsync());
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                var pixelData = await decoder.GetPixelDataAsync();
                encoder.SetPixelData(decoder.BitmapPixelFormat, decoder.BitmapAlphaMode, decoder.PixelWidth, decoder.PixelHeight, decoder.DpiX, decoder.DpiY, pixelData.DetachPixelData());
                await encoder.FlushAsync();
            }
            shareOperation.ReportCompleted();
        }
    }
}
