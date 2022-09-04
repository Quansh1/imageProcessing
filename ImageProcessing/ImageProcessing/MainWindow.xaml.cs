using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using static System.Net.Mime.MediaTypeNames;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Rectangle = System.Drawing.Rectangle;

namespace ImageProcessing
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			error_txt.Opacity = 0;
			ApplyContrast_btn.IsEnabled = false;
			SobelConvertImage_btn.IsEnabled = false;
			SaveConvertedImage_btn.IsEnabled = false;
		}

		//Выбор изображения для обработки
		private void ChooseFile_btn_Click(object sender, RoutedEventArgs e)
		{
			ApplyContrast_btn.IsEnabled = true;
			SobelConvertImage_btn.IsEnabled = true;
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Title = "Выберите картинку для конвертации";
			openFileDialog.Filter = "Data Files (*.bmp)|*.bmp|(*.png)|*.png|(*.jpg)|*.jpg";
			if(openFileDialog.ShowDialog() == true)
			{
				InputImage_img.Source = new BitmapImage(new Uri(openFileDialog.FileName));
			}
		}

		//Сохранение изображения после обработки
		private void SaveConvertedImage_btn_Click(object sender, RoutedEventArgs e)
		{
			if(OutputImage_img.Source != null)
			{ 
				SaveFileDialog saveFileDialog = new SaveFileDialog();
				saveFileDialog.Title = "Сохранить изображение как...";
				saveFileDialog.Filter = "Data Files (*.bmp)|*.bmp|(*.png)|*.png|(*.jpg)|*.jpg";
				saveFileDialog.DefaultExt = ".bmp";
				saveFileDialog.AddExtension = true;
				if(saveFileDialog.ShowDialog() == true)
				{
					String filePath = saveFileDialog.FileName;
					var encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create((BitmapSource)OutputImage_img.Source));
					using(FileStream stream = new FileStream(filePath, FileMode.Create))
						encoder.Save(stream);
				}
			}
			else 
			{
				SaveConvertedImage_btn.IsEnabled = false;
			}
		}

		//Применение контрастности к изображению
		private async void ApplyContrast_btn_Click(object sender, RoutedEventArgs e)
		{
			if(InputImage_img.Source == null)
			{
				error_txt.Opacity = 100;
				error_txt.Content = "Ошибка: \n Изображение не найдено";

			}
			else
			{
				error_txt.Opacity = 100;
				error_txt.Content = "Ошибка: \n Выберите к какому изображение применить контрастность";
			}
			if(OutputImageChecked_btn.IsChecked == true)
			{
				BitmapSource bitmapSource = ApplyingContrastByСhoice((BitmapSource)OutputImage_img.Source);
				OutputImage_img.Source = bitmapSource;
			}
			if(InputImageChecked_btn.IsChecked == true)
			{
				BitmapSource bitmapSource = ApplyingContrastByСhoice((BitmapSource)InputImage_img.Source);
				InputImage_img.Source = bitmapSource;
			}
		}
		//Выбор изображения для контрастирования
		public BitmapSource ApplyingContrastByСhoice(BitmapSource imageSource)
		{
			if(InputImage_img.Source != null)
			{
				if(ContrastPercentage_txt.Text != "")
				{
					if(int.Parse(ContrastPercentage_txt.Text) >= -100 && int.Parse(ContrastPercentage_txt.Text) <= 100)
					{
						SaveConvertedImage_btn.IsEnabled = true;
						error_txt.Opacity = 0;
						Bitmap imageBitmap = ConvertToBitmap((BitmapSource)InputImage_img.Source);
						Bitmap processedtBitmap = ImageContrast(imageBitmap, int.Parse(ContrastPercentage_txt.Text));
						BitmapSource bitmapSource = BitmapToBitmapImage(processedtBitmap);
						imageSource = bitmapSource;
					}
					else
					{
						error_txt.Opacity = 100;
						error_txt.Content = "Ошибка:\r\nдопустимые значения: от -100 до 100";
					}
				}
				else
				{
					error_txt.Opacity = 100;
					error_txt.Content = "Ошибка:\r\nдопустимые значения: от -100 до 100";
				}
			}
			else
			{
				error_txt.Opacity = 100;
				error_txt.Content = "Ошибка: \n Изображение не найдено";
			}
			return imageSource;
		}

		//Применение оператора Собеля к изображению
		private void SobelConvertImage_btn_Click(object sender, RoutedEventArgs e)
		{
			SaveConvertedImage_btn.IsEnabled = true;
			if(InputImage_img.Source == null)
			{
				error_txt.Opacity = 100;
				error_txt.Content = "Ошибка: \n Изображение не найдено";

			}
			else
			{
				error_txt.Opacity = 0;
				Bitmap image = ConvertToBitmap((BitmapSource)InputImage_img.Source);
				Bitmap processedtBitmap = SobelOperator(image);
				BitmapSource bitmapSource = BitmapToBitmapImage(processedtBitmap);
				OutputImage_img.Source = bitmapSource;
			}
		}

		/// <summary>
		/// Контрастинирование изображения
		/// </summary>
		/// <param Изображение="Image"></param>
		/// <param Значение="Value"></param>
		/// <returns></returns>
		public static Bitmap ImageContrast(Bitmap Image, float Value)
		{
			Value = (100.0f + Value) / 100.0f;
			Value *= Value;
			Bitmap NewBitmap = (Bitmap)Image.Clone();
			BitmapData data = NewBitmap.LockBits(
				new Rectangle(0, 0, NewBitmap.Width, NewBitmap.Height),
				ImageLockMode.ReadWrite,
				NewBitmap.PixelFormat);
			int Height = NewBitmap.Height;
			int Width = NewBitmap.Width;

			unsafe
			{
				for(int y = 0; y < Height; ++y)
				{
					byte* row = (byte*)data.Scan0 + (y * data.Stride);
					int columnOffset = 0;
					for(int x = 0; x < Width; ++x)
					{
						byte B = row[columnOffset];
						byte G = row[columnOffset + 1];
						byte R = row[columnOffset + 2];

						float Red = R / 255.0f;
						float Green = G / 255.0f;
						float Blue = B / 255.0f;
						Red = (((Red - 0.5f) * Value) + 0.5f) * 255.0f;
						Green = (((Green - 0.5f) * Value) + 0.5f) * 255.0f;
						Blue = (((Blue - 0.5f) * Value) + 0.5f) * 255.0f;

						int iR = (int)Red;
						iR = iR > 255 ? 255 : iR;
						iR = iR < 0 ? 0 : iR;
						int iG = (int)Green;
						iG = iG > 255 ? 255 : iG;
						iG = iG < 0 ? 0 : iG;
						int iB = (int)Blue;
						iB = iB > 255 ? 255 : iB;
						iB = iB < 0 ? 0 : iB;

						row[columnOffset] = (byte)iB;
						row[columnOffset + 1] = (byte)iG;
						row[columnOffset + 2] = (byte)iR;

						columnOffset += 4;
					}
				}
			}

			NewBitmap.UnlockBits(data);
			
			return NewBitmap;
		}


		/// <summary>
		/// Обработка изображения с помощью оператора Собеля
		/// </summary>
		/// <param Изображение="img"></param>
		/// <returns></returns>
		public static Bitmap SobelOperator(Bitmap img)
		{
			var bmp = new Bitmap(img);

			byte[,] pixels = new byte[bmp.Width, bmp.Height];
			byte[,] sobeled = new byte[bmp.Width, bmp.Height];

			for(int i = 0; i < pixels.GetLength(0); i++)
			{
				for(int j = 0; j < pixels.GetLength(1); j++)
				{
					pixels[i, j] = bmp.GetPixel(i, j).G;
				}
			}

			float[,] gx = new float[bmp.Width, bmp.Height];
			float[,] gy = new float[bmp.Width, bmp.Height];

			for(int i = 1; i < pixels.GetLength(0) - 1; i++)
			{
				for(int j = 1; j < pixels.GetLength(1) - 1; j++)
				{
					gx[i, j] = (pixels[i - 1, j + 1] + 2 * pixels[i, j + 1] + pixels[i + 1, j + 1]) - (
								pixels[i - 1, j - 1] + 2 * pixels[i, j - 1] + pixels[i + 1, j - 1]);
					gy[i, j] = (pixels[i + 1, j - 1] + 2 * pixels[i + 1, j] + pixels[i + 1, j + 1]) - (
							   pixels[i - 1, j - 1] + 2 * pixels[i - 1, j] + pixels[i - 1, j + 1]);

					float sobeled_pixel = MathF.Sqrt(gx[i, j] * gx[i, j] + gy[i, j] * gy[i, j]);
					sobeled_pixel = sobeled_pixel > 255 ? 255 : sobeled_pixel;

					byte byte_pixel = Convert.ToByte(sobeled_pixel);
					sobeled[i, j] = byte_pixel;
				}
			}

			Bitmap bitmapSobeled = new Bitmap(img);
			for(int i = 0; i < pixels.GetLength(0); i++)
			{
				for(int j = 0; j < pixels.GetLength(1); j++)
				{
					Color color = Color.FromArgb(sobeled[i, j], sobeled[i, j], sobeled[i, j]);
					bitmapSobeled.SetPixel(i, j, color);
				}
			}

			return bitmapSobeled;
		}

		public BitmapImage BitmapToBitmapImage(Bitmap src)
		{
			MemoryStream ms = new MemoryStream();
			((Bitmap)src).Save(ms, ImageFormat.Bmp);
			BitmapImage image = new BitmapImage();
			image.BeginInit();
			ms.Seek(0, SeekOrigin.Begin);
			image.StreamSource = ms;
			image.EndInit();
			return image;
		}
		public static Bitmap ConvertToBitmap(BitmapSource bitmapSource)
		{
				var width = bitmapSource.PixelWidth;
				var height = bitmapSource.PixelHeight;
				var stride = width * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
				var memoryBlockPointer = Marshal.AllocHGlobal(height * stride);
				bitmapSource.CopyPixels(new Int32Rect(0, 0, width, height), memoryBlockPointer, height * stride, stride);
				var bitmap = new Bitmap(width, height, stride, PixelFormat.Format32bppPArgb, memoryBlockPointer);
				return bitmap;
		}
	}
}
