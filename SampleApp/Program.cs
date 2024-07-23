using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Drawing;

namespace SampleApp
{
    public class Program
    {
        public async Task<MemoryStream> SealDocumentNew(byte[] seal, Stream originalDocument, int pageNumber, bool? isMinistryDocument, string sealPosition)
        {
            try
            {
                using (Stream inputPdfStream = originalDocument)
                using (Stream inputImageStream = new MemoryStream(seal))
                using (MemoryStream outputPdfStream = new MemoryStream())
                {
                    // Reset stream positions to 0
                    inputPdfStream.Position = 0;
                    inputImageStream.Position = 0;

                    // Load the PDF document
                    var document = PdfReader.Open(inputPdfStream, PdfDocumentOpenMode.Modify);
                    var gfx = XGraphics.FromPdfPage(document.Pages[pageNumber - 1]);

                    // Reset the image stream position before creating a bitmap
                    inputImageStream.Position = 0;
                    System.Drawing.Image bit = Bitmap.FromStream(inputImageStream);

                    using (MemoryStream msBit = new MemoryStream())
                    {
                        bit.Save(msBit, System.Drawing.Imaging.ImageFormat.Png);
                        msBit.Position = 0;
                        XImage image = XImage.FromStream(() => msBit);

                        // Calculate the position for the seal
                        XRect pageSize = document.Pages[pageNumber - 1].MediaBox.ToXRect();
                        var w = pageSize.Width;
                        var h = pageSize.Height;
                        double imageWidth = 168;
                        double imageHeight = 60;
                        double xPosition = 0;
                        double yPosition = 0;

                        switch (sealPosition)
                        {
                            case "btm_r":
                                xPosition = w - 210;
                                yPosition = h - imageHeight - ((isMinistryDocument ?? false) ? 80 : 40);
                                break;
                            case "btm_c":
                                xPosition = w / 2 - imageWidth / 2;
                                yPosition = h - imageHeight - ((isMinistryDocument ?? false) ? 80 : 40);
                                break;
                            case "btm_l":
                                xPosition = imageWidth + 40;
                                yPosition = h - imageHeight - ((isMinistryDocument ?? false) ? 80 : 40);
                                break;
                            case "top_r":
                                xPosition = w - 210;
                                yPosition = 50 + imageWidth;
                                break;
                            case "top_c":
                                xPosition = w / 2 - imageWidth / 2;
                                yPosition = 50 + imageWidth;
                                break;
                            case "top_l":
                                xPosition = imageWidth + 40;
                                yPosition = 50 + imageWidth;
                                break;
                            default:
                                throw new ArgumentException("Invalid seal position specified");
                        }

                        // Draw the image on the specified page
                        gfx.DrawImage(image, xPosition, yPosition, imageWidth, imageHeight);

                        // Save the modified PDF to the output stream
                        document.Save(outputPdfStream, false);
                    }

                    return outputPdfStream;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static string GetOutFilePath(string name)
        {
            string OutputDirName = @".";
            return Path.Combine(OutputDirName, name);
        }

        private static void SaveDocument(PdfDocument document, string name)
        {
            string outFilePath = GetOutFilePath(name);
            string? dir = Path.GetDirectoryName(outFilePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            document.Save(outFilePath);
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting...");

            const string outName = "test1.pdf";
            const string modifiedOutName = "modified_test1.pdf";
            const string sealImagePath = "seal.png"; // Path to your seal image
            const string pdfFilePath = "somepdf.pdf"; // Path to your original PDF file

            // Create a new PDF document for testing
            PdfDocument document = new PdfDocument();
            PdfPage pageNewRenderer = document.AddPage();
            XGraphics renderer = XGraphics.FromPdfPage(pageNewRenderer);
            renderer.DrawString("Testy Test Test", new XFont("Arial", 12), XBrushes.Black, new XPoint(12, 12));
            SaveDocument(document, outName);

            // Load the seal image as byte[]
            byte[] sealImage = await File.ReadAllBytesAsync(sealImagePath);

            // Load the original PDF
            using (FileStream originalPdfStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.Read))
            {
                Program program = new Program();
                MemoryStream modifiedPdfStream = await program.SealDocumentNew(sealImage, originalPdfStream, 1, false, "btm_r");

                // Save the modified PDF to a new file
                using (FileStream outputStream = new FileStream(modifiedOutName, FileMode.Create, FileAccess.Write))
                {
                    modifiedPdfStream.WriteTo(outputStream);
                }
            }

            Console.WriteLine("Done!");
        }
    }
}
