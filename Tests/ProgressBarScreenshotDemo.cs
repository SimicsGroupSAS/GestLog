using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GestLog.Tests
{
    public class ProgressBarScreenshotDemo
    {
        // Esta clase simula diferentes estados de la barra de progreso
        // para crear capturas de pantalla de referencia para el manual de pruebas
        
        public static void CaptureProgressBarScreenshots()
        {
            // Este método se puede llamar manualmente desde una herramienta de prueba
            // para generar capturas de ejemplo para el manual de pruebas
            
            Console.WriteLine("Este es un código de demostración para generar capturas de pantalla.");
            Console.WriteLine("Para usarlo, deberías crear una ventana de prueba que simule diferentes");
            Console.WriteLine("estados de la barra de progreso y luego capture imágenes como sigue:");
            
            /*
            // Ejemplo de cómo capturar una pantalla:
            var screenshotFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
            if (!Directory.Exists(screenshotFolder))
                Directory.CreateDirectory(screenshotFolder);
                
            // Capturar la ventana actual
            var visual = Application.Current.MainWindow;
            var bounds = VisualTreeHelper.GetDescendantBounds(visual);
            var bitmap = new RenderTargetBitmap(
                (int)bounds.Width, (int)bounds.Height, 
                96, 96, PixelFormats.Pbgra32);
                
            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                var brush = new VisualBrush(visual);
                context.DrawRectangle(brush, null, new Rect(new Point(), bounds.Size));
            }
            
            bitmap.Render(drawingVisual);
            
            // Guardar la imagen
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            var filename = Path.Combine(screenshotFolder, $"ProgressBar_Estado.png");
            using (var stream = File.Create(filename))
            {
                encoder.Save(stream);
            }
            */
        }
    }
}
