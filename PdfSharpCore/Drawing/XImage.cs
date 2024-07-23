#region PDFsharp - A .NET library for processing PDF
//
// Authors:
//   Stefan Lange
//
// Copyright (c) 2005-2016 empira Software GmbH, Cologne Area (Germany)
//
// http://www.PdfSharp.com
// http://sourceforge.net/projects/pdfsharp
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Diagnostics;
using System.IO;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf.Advanced;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using PdfSharpCore.Pdf.IO.enums;
using static MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes.ImageSource;
using PdfSharpCore.Utils;
using ImageMagick;

namespace PdfSharpCore.Drawing
{
    [Flags]
    internal enum XImageState
    {
        UsedInDrawingContext = 0x00000001,
        StateMask = 0x0000FFFF,
    }

    /// <summary>
    /// Defines an object used to draw image files (bmp, png, jpeg, gif) and PDF forms.
    /// An abstract base class that provides functionality for the Bitmap and Metafile descended classes.
    /// </summary>
    public class XImage : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XImage"/> class.
        /// </summary>
        protected XImage() { }

        XImage(string path)
        {
            if (ImageSource.ImageSourceImpl == null) ImageSource.ImageSourceImpl = new MagickSharpImageSource();
            _source = ImageSource.FromFile(path);
            Initialize();
        }

        XImage(IImageSource imageSource)
        {
            _source = imageSource;
            _path = _source.Name;
            Initialize();
        }

        XImage(Func<Stream> stream)
        {
            // Create a dummy unique path.
            _path = "*" + Guid.NewGuid().ToString("B");
            if (ImageSource.ImageSourceImpl == null)
                ImageSource.ImageSourceImpl = new MagickSharpImageSource();
            _source = ImageSource.FromStream(_path, stream);
            Initialize();
        }

        XImage(Func<byte[]> data)
        {
            // Create a dummy unique path.
            _path = "*" + Guid.NewGuid().ToString("B");
            _source = ImageSource.FromBinary(_path, data);
            Initialize();
        }

        public static XImage FromFile(string path)
        {
            return FromFile(path, PdfReadAccuracy.Strict);
        }

        public static XImage FromFile(string path, PdfReadAccuracy accuracy)
        {
            if (PdfReader.TestPdfFile(path) > 0)
                return new XPdfForm(path, accuracy);
            return new XImage(path);
        }

        public static XImage FromStream(Func<Stream> stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            return new XImage(stream);
        }

        public static XImage FromImageSource(IImageSource imageSouce)
        {
            return new XImage(imageSouce);
        }

        public static bool ExistsFile(string path)
        {
            if (PdfReader.TestPdfFile(path) > 0)
                return true;
            return false;
        }

        internal XImageState XImageState
        {
            get { return _xImageState; }
            set { _xImageState = value; }
        }
        XImageState _xImageState;

        internal void Initialize()
        {
            if (_source != null)
            {
                _format = _source.Transparent ? XImageFormat.Png : XImageFormat.Jpeg;
            }
        }

        public MemoryStream AsJpeg()
        {
            var ms = new MemoryStream();
            _source.SaveAsJpeg(ms);
            ms.Position = 0;
            return ms;
        }

        public MemoryStream AsBitmap()
        {
            var ms = new MemoryStream();
            _source.SaveAsPdfBitmap(ms);
            ms.Position = 0;
            return ms;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
                _disposed = true;
        }
        bool _disposed;

        public virtual double PointWidth
        {
            get
            {
                return _source.Width * 72 / 96.0;
            }
        }

        public virtual double PointHeight
        {
            get
            {
                return _source.Height * 72 / 96.0;
            }
        }

        public virtual int PixelWidth
        {
            get
            {
                return _source.Width;
            }
        }

        public virtual int PixelHeight
        {
            get
            {
                return _source.Height;
            }
        }

        public virtual XSize Size
        {
            get { return new XSize(PointWidth, PointHeight); }
        }

        public virtual double HorizontalResolution
        {
            get
            {
                return 96;
            }
        }

        public virtual double VerticalResolution
        {
            get
            {
                return 96;
            }
        }

        public virtual bool Interpolate
        {
            get { return _interpolate; }
            set { _interpolate = value; }
        }
        bool _interpolate = true;

        public XImageFormat Format
        {
            get { return _format; }
        }
        XImageFormat _format;

        internal void AssociateWithGraphics(XGraphics gfx)
        {
            if (_associatedGraphics != null)
                throw new InvalidOperationException("XImage already associated with XGraphics.");
            _associatedGraphics = null;
        }

        internal void DisassociateWithGraphics()
        {
            if (_associatedGraphics == null)
                throw new InvalidOperationException("XImage not associated with XGraphics.");
            _associatedGraphics.DisassociateImage();

            Debug.Assert(_associatedGraphics == null);
        }

        internal void DisassociateWithGraphics(XGraphics gfx)
        {
            if (_associatedGraphics != gfx)
                throw new InvalidOperationException("XImage not associated with XGraphics.");
            _associatedGraphics = null;
        }

        internal XGraphics AssociatedGraphics
        {
            get { return _associatedGraphics; }
            set { _associatedGraphics = value; }
        }
        XGraphics _associatedGraphics;

        internal string _path;

        internal PdfImageTable.ImageSelector _selector;
        private IImageSource _source;
    }
}
