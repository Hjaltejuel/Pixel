using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Pixel.Views
{
    public partial class ImageView :  Page
    {
        private SoftwareBitmap bitmap;
        private SoftwareBitmap alteredImage;
        private String technique = "Pixelated average";
        public ImageView() : base()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
            rootFrame.CanGoBack ?
            AppViewBackButtonVisibility.Visible :
            AppViewBackButtonVisibility.Collapsed;

            base.OnNavigatedTo(e);
            var softwareBitmap = SoftwareBitmap.Convert((SoftwareBitmap)e.Parameter, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            bitmap = softwareBitmap;
            alteredImage = bitmap;
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(softwareBitmap);
            imageControl.Source = source;

        }
        private async void StartDownload_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fileSavePicker.FileTypeChoices.Add("PNG files", new List<string>() {  ".png" });
            fileSavePicker.SuggestedFileName = "PixelatedImage";

            var outputFile = await fileSavePicker.PickSaveFileAsync();

            if (outputFile == null)
            {
                // The user cancelled the picking operation
                return;
            }
            SaveSoftwareBitmapToFile(alteredImage, outputFile);

        }

        private async void SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, StorageFile outputFile)
        {
            using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // Create an encoder with the desired format
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

                // Set the software bitmap
                encoder.SetSoftwareBitmap(softwareBitmap);
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.IsThumbnailGenerated = true;

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception err)
                {
                    const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                    switch (err.HResult)
                    {
                        case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                            // If the encoder does not support writing a thumbnail, then try again
                            // but disable thumbnail generation.
                            encoder.IsThumbnailGenerated = false;
                            break;
                        default:
                            throw;
                    }
                }

                if (encoder.IsThumbnailGenerated == false)
                {
                    await encoder.FlushAsync();
                }


            }
        }
        private async void slider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            var softwareBitmap = bitmap;
            if(e.NewValue != 0.0)
            {
                switch (technique)
                {
                    case "Pixelated average":
                        softwareBitmap = Edit_image(bitmap, (int)e.NewValue);
                        break;
                    case "Pixelated median":
                        softwareBitmap = Edit_image_Middle(bitmap, (int)e.NewValue);
                        break;
                    default:
                        softwareBitmap = Blur_image(bitmap, (int)e.NewValue);
                        break;
                }
                
                

            }
            alteredImage = softwareBitmap;
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(softwareBitmap);
            imageControl.Source = source;
            

        }

       

        private unsafe SoftwareBitmap Edit_image_Middle(SoftwareBitmap softwareBitmape, int PixelationRatio)
        {

            var softwareBitmap = SoftwareBitmap.Convert(softwareBitmape, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
            {
                using (var reference = buffer.CreateReference())
                {
                    byte* dataInBytes;
                    uint capacity;
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);

                    // Fill-in the BGRA plane
                    BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);




                    for (int i = 0; i < bufferLayout.Height + PixelationRatio; i = i + PixelationRatio * 2)
                    {

                        for (int j = 0; j < bufferLayout.Width + PixelationRatio; j = j + PixelationRatio * 2)
                        {




                            for (int k = j - PixelationRatio; k <= j + PixelationRatio; k++)
                            {
                                for (int h = i - PixelationRatio; h <= i + PixelationRatio; h++)
                                {
                                    if (k >= 0 && h >= 0 && k < bufferLayout.Width && h < bufferLayout.Height)
                                    {
                                        var iIndex = i >= bufferLayout.Height ? bufferLayout.Height - 1 : i;
                                        var jIndex = j >= bufferLayout.Width ? bufferLayout.Width - 1 : j;
                                        byte value1 = dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * iIndex + 4 * jIndex + 0];
                                        byte value2 = dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * iIndex + 4 * jIndex + 1];
                                        byte value3 = dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * iIndex + 4 * jIndex + 2];
                                        byte value4 = dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * iIndex + 4 * jIndex + 3];

                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 0] = value1;
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 1] = value2;
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 2] = value3;
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 3] = value4;
                          
                                  
                                    }
                                }

                            }
                     

                        }
                    }
                }
            }

            return softwareBitmap;
        }
        private unsafe SoftwareBitmap Edit_image(SoftwareBitmap softwareBitmape, int PixelationRatio)
        {

            var softwareBitmap = SoftwareBitmap.Convert(softwareBitmape, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
            {
                using (var reference = buffer.CreateReference())
                {
                    byte* dataInBytes;
                    uint capacity;
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);

                    // Fill-in the BGRA plane
                    BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);




                    for (int i = 0; i < bufferLayout.Height + PixelationRatio; i = i + PixelationRatio*2)
                    {

                        for (int j = 0; j < bufferLayout.Width + PixelationRatio; j = j + PixelationRatio * 2)
                        {


                            float avg1 = 0;
                            float avg2 = 0;
                            float avg3 = 0;
                            float avg4 = 0;
                            float l = 0;

                            for (int k = j - PixelationRatio; k <= j + PixelationRatio; k++)
                            {
                                for (int h = i - PixelationRatio; h <= i + PixelationRatio; h++)
                                {
                                    if (k >= 0 && h >= 0 && k < bufferLayout.Width && h < bufferLayout.Height)
                                    {
                                        avg1 += dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 0];
                                        avg2 += dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 1];
                                        avg3 += dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 2];
                                        avg4 += dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 3];
                                        var test = dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 0];
                                        l++;
                                    }
                                }

                            }


                            for (int k = j - PixelationRatio; k <= j + PixelationRatio; k++)
                            {
                                for (int h = i - PixelationRatio; h <= i + PixelationRatio; h++)
                                {
                                    if (k >= 0 && h >= 0 && k < bufferLayout.Width && h < bufferLayout.Height)
                                    {
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 0] = (byte)(avg1 / l);
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 1] = (byte)(avg2 / l);
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 2] = (byte)(avg3 / l);
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 3] = (byte)(avg4 / l);
                                    }
                                }

                            }

                        }
                    }
                }
            }

            return softwareBitmap;
        }



        private unsafe SoftwareBitmap Blur_image(SoftwareBitmap softwareBitmape, int blurRatio)
        {

            var softwareBitmap = SoftwareBitmap.Convert(softwareBitmape, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
            {
                using (var reference = buffer.CreateReference())
                {
                    byte* dataInBytes;
                    uint capacity;
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);

                    // Fill-in the BGRA plane
                    BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);



                    for (int i = 0; i < bufferLayout.Height; i++)
                    {
                        for (int j = 0; j < bufferLayout.Width; j++)
                        {
                            float avg1 = 0;
                            float avg2 = 0;
                            float avg3 = 0;
                            float avg4 = 0;
                            float l = 0;

                            for (int k = j - blurRatio; k <= j + blurRatio; k++)
                            {
                                for (int h = i - blurRatio; h <= i + blurRatio; h++)
                                {
                                    if (k >= 0 && h >= 0 && k < bufferLayout.Width && h < bufferLayout.Height)
                                    {
                                        avg1 += dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 0];
                                        avg2 += dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 1];
                                        avg3 += dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 2];
                                        avg4 += dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 3];
                                        var test = dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * h + 4 * k + 0];
                                        l++;
                                    }
                                }

                            }





                            dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] = (byte)(avg1 / l);
                            dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = (byte)(avg2 / l);
                            dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = (byte)(avg3 / l);
                            dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = (byte)(avg4 / l);
                        }
                    }
                }
            }

            return softwareBitmap;
        }

        private void colorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            technique = (string)e.AddedItems[0];
            slider.Value = 0;
            switch (technique)
            {
                case "Pixelated average":
                    slider.Maximum = 100;
                    break;
                case "Pixelated median":
                    slider.Maximum = 100;
                    break;
                default:
                    slider.Maximum = 15;
                    break;
            }
        }
    }
}
