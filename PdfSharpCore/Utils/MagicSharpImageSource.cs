using ImageMagick;
using ImageMagick.Formats;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using System;
using System.IO;

namespace PdfSharpCore.Utils
{
    public class MagickSharpImageSource : ImageSource
    {
        public static IImageSource FromMagickImage(MagickImage image, int? quality = 75)
        {
            var _path = "*" + Guid.NewGuid().ToString("B");
            return new MagickSharpImageSourceImpl(_path, image, (int)quality);
        }

        protected override IImageSource FromBinaryImpl(string name, Func<byte[]> imageSource, int? quality = 75)
        {
            var image = new MagickImage(imageSource.Invoke());
            return new MagickSharpImageSourceImpl(name, image, (int)quality);
        }

        protected override IImageSource FromFileImpl(string path, int? quality = 75)
        {
            var image = new MagickImage(path);
            return new MagickSharpImageSourceImpl(path, image, (int)quality);
        }

        protected override IImageSource FromStreamImpl(string name, Func<Stream> imageStream, int? quality = 75)
        {
            using (var stream = imageStream.Invoke())
            {
                var image = new MagickImage(stream);
                return new MagickSharpImageSourceImpl(name, image, (int)quality);
            }
        }

        private class MagickSharpImageSourceImpl : IImageSource
        {
            private MagickImage Image { get; }
            private readonly int _quality;

            public int Width => Image.Width;
            public int Height => Image.Height;
            public string Name { get; }
            public bool Transparent => Image.HasAlpha;

            public MagickSharpImageSourceImpl(string name, MagickImage image, int quality)
            {
                Name = name;
                Image = image;
                _quality = quality;
            }

            public void SaveAsJpeg(MemoryStream ms)
            {
                Image.Format = MagickFormat.Jpeg;
                Image.Quality = this._quality;
                Image.Write(ms);
            }

            public void Dispose()
            {
                Image.Dispose();
            }

            public void SaveAsPdfBitmap(MemoryStream ms)
            {
                //Image.SetBitDepth(5);
                //Image.Density = new Density(300);
                ////Image.Quantize(new QuantizeSettings() { Colors = 32 });
                //Image.Write(ms,MagickFormat.Bmp);
                Image.Format = MagickFormat.Bmp;
                Image.Settings.SetDefine(MagickFormat.Bmp, "bits-per-pixel", "32");
                Image.Write(ms);
            }
        }
    }
}
