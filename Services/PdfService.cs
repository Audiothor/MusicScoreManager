using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace MusicScoreManager.Services
{
    public class PdfPageItem : INotifyPropertyChanged
    {
        private int _pageNumber;
        private int _rotation = 0;
        private ImageSource? _thumbnailSource;

        public string? ImagePath { get; set; }
        public string? SourcePdfPath { get; set; }
        public int SourcePageIndex { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public bool IsImage => !string.IsNullOrEmpty(ImagePath);

        public int PageNumber
        {
            get => _pageNumber;
            set
            {
                if (_pageNumber != value)
                {
                    _pageNumber = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PageDisplay));
                }
            }
        }

        public string PageDisplay => $"Page {PageNumber}";

        public int Rotation
        {
            get => _rotation;
            set
            {
                if (_rotation != value)
                {
                    _rotation = (value % 360 + 360) % 360;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RotationDisplay));
                }
            }
        }

        public string RotationDisplay => Rotation == 0 ? "0°" : $"{Rotation}°";

        public ImageSource? ThumbnailSource
        {
            get => _thumbnailSource;
            set
            {
                if (_thumbnailSource != value)
                {
                    _thumbnailSource = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class NaturalStringComparer
    {
        public static int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            return Regex.Replace(x, @"\d+", m => m.Value.PadLeft(10, '0'))
                .CompareTo(Regex.Replace(y, @"\d+", m => m.Value.PadLeft(10, '0')));
        }
    }

    public class PdfService
    {
        /// <summary>
        /// Convertit une liste de fichiers image en un unique fichier PDF multi-pages haute fidélité.
        /// Chaque image occupe 100% de la page avec ses proportions réelles.
        /// </summary>
        public async Task<string> ConvertImagesToPdfAsync(IEnumerable<string> imagePaths, string outputPdfPath, List<int>? rotations = null)
        {
            return await Task.Run(() =>
            {
                var imageList = imagePaths.ToList();
                var pageItems = new List<PdfPageItem>();

                for (int i = 0; i < imageList.Count; i++)
                {
                    int rot = (rotations != null && i < rotations.Count) ? rotations[i] : 0;
                    pageItems.Add(new PdfPageItem
                    {
                        ImagePath = imageList[i],
                        Rotation = rot,
                        PageNumber = i + 1
                    });
                }

                return BuildPdfFromItemsInternal(pageItems, outputPdfPath);
            });
        }

        /// <summary>
        /// Assemble une liste d'éléments images en un nouveau fichier PDF.
        /// </summary>
        public async Task<string> BuildPdfFromItemsAsync(IEnumerable<PdfPageItem> items, string outputPdfPath)
        {
            return await Task.Run(() =>
            {
                return BuildPdfFromItemsInternal(items, outputPdfPath);
            });
        }

        private string BuildPdfFromItemsInternal(IEnumerable<PdfPageItem> items, string outputPdfPath)
        {
            var itemList = items.Where(it => !string.IsNullOrEmpty(it.ImagePath) && File.Exists(it.ImagePath)).ToList();
            if (!itemList.Any())
            {
                throw new InvalidOperationException("Aucune image valide à convertir en PDF.");
            }

            var dir = Path.GetDirectoryName(outputPdfPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Préparer les données JPEG encodées pour chaque page
            var encodedPages = new List<(byte[] JpegBytes, int Width, int Height)>();

            foreach (var item in itemList)
            {
                using var sourceImage = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(item.ImagePath!);
                
                // Correction automatique de l'orientation Exif de l'appareil photo
                sourceImage.Mutate(x => x.AutoOrient());

                // Appliquer la rotation demandée par l'utilisateur
                int normalizedRotation = (item.Rotation % 360 + 360) % 360;
                if (normalizedRotation == 90)
                {
                    sourceImage.Mutate(x => x.Rotate(RotateMode.Rotate90));
                }
                else if (normalizedRotation == 180)
                {
                    sourceImage.Mutate(x => x.Rotate(RotateMode.Rotate180));
                }
                else if (normalizedRotation == 270)
                {
                    sourceImage.Mutate(x => x.Rotate(RotateMode.Rotate270));
                }

                // Aplatir sur un fond blanc pur en RGB24 (3 canaux garantis : évite le bug des 3 miniatures sur images 1-canal N&B/niveaux de gris)
                using var rgbImage = new Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(sourceImage.Width, sourceImage.Height);
                rgbImage.Mutate(ctx =>
                {
                    ctx.BackgroundColor(SixLabors.ImageSharp.Color.White);
                    ctx.DrawImage(sourceImage, new SixLabors.ImageSharp.Point(0, 0), 1f);
                });

                using var ms = new MemoryStream();
                rgbImage.SaveAsJpeg(ms, new JpegEncoder
                {
                    Quality = 92,
                    ColorType = JpegColorType.Rgb
                });

                encodedPages.Add((ms.ToArray(), rgbImage.Width, rgbImage.Height));
            }

            // Écriture du document PDF standard conforme ISO 32000-1 (PDF 1.4)
            using var fileStream = File.Create(outputPdfPath);
            using var writer = new BinaryWriter(fileStream, Encoding.ASCII);

            var offsets = new List<long>();
            offsets.Add(0); // Index 0 non utilisé dans xref

            void WriteString(string str)
            {
                var bytes = Encoding.ASCII.GetBytes(str);
                writer.Write(bytes);
            }

            // 1. En-tête PDF
            WriteString("%PDF-1.4\n%\xe2\xe3\xcf\xd3\n");

            int pageCount = encodedPages.Count;

            // Structure des IDs d'objets :
            // Obj 1 : Catalog
            // Obj 2 : Pages collection
            // Pour chaque page i (0-based) :
            //   Obj (3 + i*3)     : Page
            //   Obj (3 + i*3 + 1) : Contents (stream de dessin)
            //   Obj (3 + i*3 + 2) : Image XObject (/Filter /DCTDecode)

            int catalogId = 1;
            int pagesRootId = 2;

            // Obj 1 : Catalog
            offsets.Add(fileStream.Position);
            WriteString($"{catalogId} 0 obj\n<< /Type /Catalog /Pages {pagesRootId} 0 R >>\nendobj\n");

            // Obj 2 : Pages
            var pageKidsRefs = new StringBuilder();
            for (int i = 0; i < pageCount; i++)
            {
                int pageObjId = 3 + i * 3;
                pageKidsRefs.Append($"{pageObjId} 0 R ");
            }

            offsets.Add(fileStream.Position);
            WriteString($"{pagesRootId} 0 obj\n<< /Type /Pages /Kids [ {pageKidsRefs.ToString().Trim()} ] /Count {pageCount} >>\nendobj\n");

            // Pour chaque page
            for (int i = 0; i < pageCount; i++)
            {
                var (jpegBytes, width, height) = encodedPages[i];

                int pageObjId = 3 + i * 3;
                int contentsObjId = pageObjId + 1;
                int imageObjId = pageObjId + 2;

                // Flux de commande pour afficher l'image plein cadre sur la page
                string contentStream = $"q\n{width} 0 0 {height} 0 0 cm\n/Im1 Do\nQ\n";
                byte[] contentBytes = Encoding.ASCII.GetBytes(contentStream);

                // Page Object
                offsets.Add(fileStream.Position);
                WriteString($"{pageObjId} 0 obj\n");
                WriteString($"<< /Type /Page\n");
                WriteString($"   /Parent {pagesRootId} 0 R\n");
                WriteString($"   /MediaBox [ 0 0 {width} {height} ]\n");
                WriteString($"   /Contents {contentsObjId} 0 R\n");
                WriteString($"   /Resources <<\n");
                WriteString($"     /ProcSet [ /PDF /ImageC ]\n");
                WriteString($"     /XObject << /Im1 {imageObjId} 0 R >>\n");
                WriteString($"   >>\n");
                WriteString($">>\nendobj\n");

                // Contents Object
                offsets.Add(fileStream.Position);
                WriteString($"{contentsObjId} 0 obj\n");
                WriteString($"<< /Length {contentBytes.Length} >>\n");
                WriteString("stream\n");
                writer.Write(contentBytes);
                WriteString("\nendstream\nendobj\n");

                // Image XObject
                offsets.Add(fileStream.Position);
                WriteString($"{imageObjId} 0 obj\n");
                WriteString($"<< /Type /XObject\n");
                WriteString($"   /Subtype /Image\n");
                WriteString($"   /Width {width}\n");
                WriteString($"   /Height {height}\n");
                WriteString($"   /ColorSpace /DeviceRGB\n");
                WriteString($"   /BitsPerComponent 8\n");
                WriteString($"   /Filter /DCTDecode\n");
                WriteString($"   /Length {jpegBytes.Length}\n");
                WriteString($">>\n");
                WriteString("stream\n");
                writer.Write(jpegBytes);
                WriteString("\nendstream\nendobj\n");
            }

            // Table XRef
            long xrefOffset = fileStream.Position;
            int totalObjects = 2 + pageCount * 3;

            WriteString($"xref\n0 {totalObjects + 1}\n");
            WriteString("0000000000 65535 f \n");

            for (int i = 1; i <= totalObjects; i++)
            {
                long objOffset = offsets[i];
                WriteString($"{objOffset:D10} 00000 n \n");
            }

            // Trailer
            WriteString("trailer\n");
            WriteString($"<< /Size {totalObjects + 1}\n");
            WriteString($"   /Root {catalogId} 0 R\n");
            WriteString($">>\n");
            WriteString("startxref\n");
            WriteString($"{xrefOffset}\n");
            WriteString("%%EOF\n");

            fileStream.Flush();
            return outputPdfPath;
        }
    }
}
