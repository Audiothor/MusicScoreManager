using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Path = System.IO.Path;
#if ANDROID
using Android.Graphics;
using Android.Graphics.Pdf;
using Android.OS;
#endif
#if WINDOWS
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
#endif
#if IOS || MACCATALYST
using Foundation;
using UIKit;
using PdfKit;
using CoreGraphics;
#endif

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

    public class RenderedPdfPage
    {
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
        public int Width { get; set; }
        public int Height { get; set; }
        public int PageNumber { get; set; }
        public bool IsTwoPages { get; set; }
        public int LeftPage { get; set; }
        public int RightPage { get; set; }
        public bool HasRightPage { get; set; }
    }

    public class PdfService
    {
        private static readonly Dictionary<string, RenderedPdfPage> _pageMemoryCache = new();
        private static readonly object _cacheLock = new();
        private const int MaxMemoryCacheEntries = 12;

        public static bool IsNativePdfSupported =>
#if ANDROID || WINDOWS || IOS || MACCATALYST
            true;
#else
            false;
#endif

        public static void ClearMemoryCache()
        {
            lock (_cacheLock)
            {
                _pageMemoryCache.Clear();
            }
        }

        public async Task<int> GetPdfPageCountAsync(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath)) return 1;

            return await Task.Run(async () =>
            {
#if ANDROID
                try
                {
                    var file = new Java.IO.File(pdfPath);
                    using var fd = ParcelFileDescriptor.Open(file, ParcelFileMode.ReadOnly);
                    if (fd != null)
                    {
                        using var renderer = new PdfRenderer(fd);
                        return renderer.PageCount;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PdfService] Android GetPdfPageCountAsync error: {ex.Message}");
                }
#endif
#if WINDOWS
                try
                {
                    var storageFile = await StorageFile.GetFileFromPathAsync(pdfPath);
                    var pdfDoc = await PdfDocument.LoadFromFileAsync(storageFile);
                    return (int)pdfDoc.PageCount;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PdfService] Windows GetPdfPageCountAsync error: {ex.Message}");
                }
#endif
#if IOS || MACCATALYST
                try
                {
                    using var url = Foundation.NSUrl.FromFilename(pdfPath);
                    using var pdfDoc = new PdfKit.PdfDocument(url);
                    if (pdfDoc != null) return (int)pdfDoc.PageCount;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PdfService] iOS GetPdfPageCountAsync error: {ex.Message}");
                }
#endif
                return 1;
            });
        }

        public async Task<RenderedPdfPage?> RenderPdfPageAsync(string pdfPath, int pageNumber, int rotation = 0, float scale = 2.0f)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath)) return null;

            string cacheKey = $"{pdfPath}_p{pageNumber}_r{rotation}_s{scale}";
            lock (_cacheLock)
            {
                if (_pageMemoryCache.TryGetValue(cacheKey, out var cached))
                {
                    return cached;
                }
            }

            var rendered = await Task.Run(async () =>
            {
#if ANDROID
                try
                {
                    var file = new Java.IO.File(pdfPath);
                    using var fd = ParcelFileDescriptor.Open(file, ParcelFileMode.ReadOnly);
                    if (fd == null) return null;

                    using var renderer = new PdfRenderer(fd);
                    int pageIndex = Math.Clamp(pageNumber - 1, 0, renderer.PageCount - 1);
                    using var page = renderer.OpenPage(pageIndex);

                    int renderW = Math.Max(1, (int)(page.Width * scale));
                    int renderH = Math.Max(1, (int)(page.Height * scale));

                    using var rawBmp = Bitmap.CreateBitmap(renderW, renderH, Bitmap.Config.Argb8888!);
                    rawBmp.EraseColor(Android.Graphics.Color.White);
                    page.Render(rawBmp, null, null, PdfRenderMode.ForDisplay);

                    Bitmap finalBmp = rawBmp;
                    bool recycled = false;
                    int normRot = (rotation % 360 + 360) % 360;
                    if (normRot != 0)
                    {
                        using var mat = new Android.Graphics.Matrix();
                        mat.PostRotate(normRot);
                        finalBmp = Bitmap.CreateBitmap(rawBmp, 0, 0, rawBmp.Width, rawBmp.Height, mat, true);
                        recycled = true;
                    }

                    using var ms = new MemoryStream();
                    finalBmp.Compress(Bitmap.CompressFormat.Jpeg!, 90, ms);
                    int fw = finalBmp.Width;
                    int fh = finalBmp.Height;

                    if (recycled) finalBmp.Recycle();

                    return new RenderedPdfPage
                    {
                        ImageBytes = ms.ToArray(),
                        Width = fw,
                        Height = fh,
                        PageNumber = pageIndex + 1,
                        IsTwoPages = false
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PdfService] Android RenderPdfPageAsync error: {ex.Message}");
                }
#endif
#if WINDOWS
                try
                {
                    var storageFile = await StorageFile.GetFileFromPathAsync(pdfPath);
                    var pdfDoc = await PdfDocument.LoadFromFileAsync(storageFile);
                    int pageIndex = Math.Clamp(pageNumber - 1, 0, (int)pdfDoc.PageCount - 1);

                    using var page = pdfDoc.GetPage((uint)pageIndex);
                    using var memStream = new InMemoryRandomAccessStream();
                    var options = new PdfPageRenderOptions
                    {
                        DestinationWidth = (uint)Math.Max(1, (int)(page.Size.Width * scale)),
                        DestinationHeight = (uint)Math.Max(1, (int)(page.Size.Height * scale))
                    };
                    await page.RenderToStreamAsync(memStream, options);

                    using var readStream = memStream.AsStreamForRead();
                    using var ms = new MemoryStream();
                    await readStream.CopyToAsync(ms);
                    byte[] bytes = ms.ToArray();
                    int fw = (int)options.DestinationWidth;
                    int fh = (int)options.DestinationHeight;

                    int normRot = (rotation % 360 + 360) % 360;
                    if (normRot != 0)
                    {
                        using var img = SixLabors.ImageSharp.Image.Load(bytes);
                        img.Mutate(ctx => ctx.Rotate(normRot));
                        using var rotMs = new MemoryStream();
                        img.SaveAsJpeg(rotMs, new JpegEncoder { Quality = 90 });
                        bytes = rotMs.ToArray();
                        fw = img.Width;
                        fh = img.Height;
                    }

                    return new RenderedPdfPage
                    {
                        ImageBytes = bytes,
                        Width = fw,
                        Height = fh,
                        PageNumber = pageIndex + 1,
                        IsTwoPages = false
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PdfService] Windows RenderPdfPageAsync error: {ex.Message}");
                }
#endif
#if IOS || MACCATALYST
                try
                {
                    using var url = Foundation.NSUrl.FromFilename(pdfPath);
                    using var pdfDoc = new PdfKit.PdfDocument(url);
                    if (pdfDoc != null)
                    {
                        int pageIndex = Math.Clamp(pageNumber - 1, 0, (int)pdfDoc.PageCount - 1);
                        using var page = pdfDoc.GetPage(pageIndex);
                        if (page != null)
                        {
                            var rect = page.GetBoundsForBox(PdfKit.PdfDisplayBox.Media);
                            var size = new CoreGraphics.CGSize(rect.Width * scale, rect.Height * scale);
                            using var img = page.GetThumbnail(size, PdfKit.PdfDisplayBox.Media);
                            if (img != null)
                            {
                                using var jpegData = img.AsJPEG(0.9f);
                                if (jpegData != null)
                                {
                                    return new RenderedPdfPage
                                    {
                                        ImageBytes = jpegData.ToArray(),
                                        Width = (int)size.Width,
                                        Height = (int)size.Height,
                                        PageNumber = pageIndex + 1,
                                        IsTwoPages = false
                                    };
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PdfService] iOS RenderPdfPageAsync error: {ex.Message}");
                }
#endif
                return null;
            });

            if (rendered != null)
            {
                lock (_cacheLock)
                {
                    if (_pageMemoryCache.Count >= MaxMemoryCacheEntries)
                    {
                        var firstKey = _pageMemoryCache.Keys.FirstOrDefault();
                        if (firstKey != null) _pageMemoryCache.Remove(firstKey);
                    }
                    _pageMemoryCache[cacheKey] = rendered;
                }
            }

            return rendered;
        }

        public async Task<RenderedPdfPage?> RenderPdfTwoPagesAsync(string pdfPath, int leftPage, int rightPage, int leftRot = 0, int rightRot = 0, float scale = 1.5f)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath)) return null;

            string cacheKey = $"{pdfPath}_spread_l{leftPage}_r{rightPage}_lr{leftRot}_rr{rightRot}_s{scale}";
            lock (_cacheLock)
            {
                if (_pageMemoryCache.TryGetValue(cacheKey, out var cached))
                {
                    return cached;
                }
            }

            var rendered = await Task.Run<RenderedPdfPage?>(async () =>
            {
#if ANDROID
                try
                {
                    var file = new Java.IO.File(pdfPath);
                    using var fd = ParcelFileDescriptor.Open(file, ParcelFileMode.ReadOnly);
                    if (fd == null) return null;

                    using var renderer = new PdfRenderer(fd);
                    int totalPages = renderer.PageCount;
                    int leftIdx = Math.Clamp(leftPage - 1, 0, totalPages - 1);
                    int rightIdx = rightPage - 1;
                    bool hasRight = (rightIdx >= 0 && rightIdx < totalPages);

                    Bitmap lBmp;
                    int lNormRot = (leftRot % 360 + 360) % 360;
                    using (var lPage = renderer.OpenPage(leftIdx))
                    {
                        int lW = Math.Max(1, (int)(lPage.Width * scale));
                        int lH = Math.Max(1, (int)(lPage.Height * scale));

                        var lRawBmp = Bitmap.CreateBitmap(lW, lH, Bitmap.Config.Argb8888!);
                        lRawBmp.EraseColor(Android.Graphics.Color.White);
                        lPage.Render(lRawBmp, null, null, PdfRenderMode.ForDisplay);

                        if (lNormRot != 0)
                        {
                            using var mat = new Android.Graphics.Matrix();
                            mat.PostRotate(lNormRot);
                            lBmp = Bitmap.CreateBitmap(lRawBmp, 0, 0, lRawBmp.Width, lRawBmp.Height, mat, true);
                            lRawBmp.Recycle();
                        }
                        else
                        {
                            lBmp = lRawBmp;
                        }
                    }

                    Bitmap? rBmp = null;
                    if (hasRight)
                    {
                        int rNormRot = (rightRot % 360 + 360) % 360;
                        using (var rPage = renderer.OpenPage(rightIdx))
                        {
                            int rW = Math.Max(1, (int)(rPage.Width * scale));
                            int rH = Math.Max(1, (int)(rPage.Height * scale));

                            var rRawBmp = Bitmap.CreateBitmap(rW, rH, Bitmap.Config.Argb8888!);
                            rRawBmp.EraseColor(Android.Graphics.Color.White);
                            rPage.Render(rRawBmp, null, null, PdfRenderMode.ForDisplay);

                            if (rNormRot != 0)
                            {
                                using var mat = new Android.Graphics.Matrix();
                                mat.PostRotate(rNormRot);
                                rBmp = Bitmap.CreateBitmap(rRawBmp, 0, 0, rRawBmp.Width, rRawBmp.Height, mat, true);
                                rRawBmp.Recycle();
                            }
                            else
                            {
                                rBmp = rRawBmp;
                            }
                        }
                    }

                    int totalW = lBmp.Width + (rBmp != null ? rBmp.Width : lBmp.Width);
                    int maxH = Math.Max(lBmp.Height, rBmp != null ? rBmp.Height : lBmp.Height);

                    using var combined = Bitmap.CreateBitmap(totalW, maxH, Bitmap.Config.Argb8888!);
                    combined.EraseColor(Android.Graphics.Color.White);

                    using (var cv = new Android.Graphics.Canvas(combined))
                    {
                        cv.DrawBitmap(lBmp, 0, 0, null);
                        if (rBmp != null)
                        {
                            cv.DrawBitmap(rBmp, lBmp.Width, 0, null);
                        }
                        using var paint = new Android.Graphics.Paint { Color = Android.Graphics.Color.Rgb(60, 60, 60), StrokeWidth = 3 };
                        cv.DrawLine(lBmp.Width, 0, lBmp.Width, maxH, paint);
                    }

                    lBmp.Recycle();
                    if (rBmp != null) rBmp.Recycle();

                    using var ms = new MemoryStream();
                    combined.Compress(Bitmap.CompressFormat.Jpeg!, 90, ms);

                    return new RenderedPdfPage
                    {
                        ImageBytes = ms.ToArray(),
                        Width = totalW,
                        Height = maxH,
                        PageNumber = leftIdx + 1,
                        IsTwoPages = true,
                        LeftPage = leftIdx + 1,
                        RightPage = rightIdx + 1,
                        HasRightPage = hasRight
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PdfService] Android RenderPdfTwoPagesAsync error: {ex.Message}");
                }
#endif
#if WINDOWS
                try
                {
                    var leftSingle = await RenderPdfPageAsync(pdfPath, leftPage, leftRot, scale);
                    if (leftSingle == null) return null;

                    var storageFile = await StorageFile.GetFileFromPathAsync(pdfPath);
                    var pdfDoc = await PdfDocument.LoadFromFileAsync(storageFile);
                    int totalPages = (int)pdfDoc.PageCount;
                    int rightIdx = rightPage - 1;
                    bool hasRight = (rightIdx >= 0 && rightIdx < totalPages);

                    RenderedPdfPage? rightSingle = null;
                    if (hasRight)
                    {
                        rightSingle = await RenderPdfPageAsync(pdfPath, rightPage, rightRot, scale);
                    }

                    using var leftImg = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(leftSingle.ImageBytes);
                    SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>? rightImg = null;
                    if (rightSingle != null)
                    {
                        rightImg = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(rightSingle.ImageBytes);
                    }

                    int totalW = leftImg.Width + (rightImg != null ? rightImg.Width : leftImg.Width);
                    int maxH = Math.Max(leftImg.Height, rightImg != null ? rightImg.Height : leftImg.Height);

                    using var combined = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(totalW, maxH);
                    combined.Mutate(ctx =>
                    {
                        ctx.BackgroundColor(SixLabors.ImageSharp.Color.White);
                        ctx.DrawImage(leftImg, new SixLabors.ImageSharp.Point(0, 0), 1.0f);
                        if (rightImg != null)
                        {
                            ctx.DrawImage(rightImg, new SixLabors.ImageSharp.Point(leftImg.Width, 0), 1.0f);
                        }
                    });
                    rightImg?.Dispose();

                    using var ms = new MemoryStream();
                    combined.SaveAsJpeg(ms, new JpegEncoder { Quality = 90 });

                    return new RenderedPdfPage
                    {
                        ImageBytes = ms.ToArray(),
                        Width = totalW,
                        Height = maxH,
                        PageNumber = leftPage,
                        IsTwoPages = true,
                        LeftPage = leftPage,
                        RightPage = rightPage,
                        HasRightPage = hasRight
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PdfService] Windows RenderPdfTwoPagesAsync error: {ex.Message}");
                }
#endif
#if !WINDOWS && !ANDROID
                await Task.CompletedTask;
#endif
                return (RenderedPdfPage?)null;
            });

            if (rendered != null)
            {
                lock (_cacheLock)
                {
                    if (_pageMemoryCache.Count >= MaxMemoryCacheEntries)
                    {
                        var firstKey = _pageMemoryCache.Keys.FirstOrDefault();
                        if (firstKey != null) _pageMemoryCache.Remove(firstKey);
                    }
                    _pageMemoryCache[cacheKey] = rendered;
                }
            }

            return rendered;
        }

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

        /// <summary>
        /// Extrait les pages d'un document PDF existant sous forme d'images haute netteté pour réassemblage.
        /// </summary>
        public async Task<List<PdfPageItem>> ExtractPdfPagesAsync(string pdfPath, string cacheDir)
        {
            return await Task.Run(() =>
            {
                var list = new List<PdfPageItem>();
                if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath)) return list;

                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

#if ANDROID
                try
                {
                    var file = new Java.IO.File(pdfPath);
                    using var fileDescriptor = ParcelFileDescriptor.Open(file, ParcelFileMode.ReadOnly);
                    if (fileDescriptor != null)
                    {
                        using var renderer = new PdfRenderer(fileDescriptor);
                        int pageCount = renderer.PageCount;

                        for (int i = 0; i < pageCount; i++)
                        {
                            using var page = renderer.OpenPage(i);
                            float scale = 2.0f; // 144 DPI pour un rendu haute netteté
                            int renderWidth = Math.Max(1, (int)(page.Width * scale));
                            int renderHeight = Math.Max(1, (int)(page.Height * scale));

                            using var bitmap = Bitmap.CreateBitmap(renderWidth, renderHeight, Bitmap.Config.Argb8888!);
                            bitmap.EraseColor(Android.Graphics.Color.White);

                            page.Render(bitmap, null, null, PdfRenderMode.ForDisplay);

                            string pageImagePath = Path.Combine(cacheDir, $"pdf_extracted_{Guid.NewGuid()}_{i + 1}.jpg");
                            using (var stream = File.Create(pageImagePath))
                            {
                                bitmap.Compress(Bitmap.CompressFormat.Jpeg!, 90, stream);
                            }

                            list.Add(new PdfPageItem
                            {
                                ImagePath = pageImagePath,
                                DisplayName = $"Page {i + 1}",
                                ThumbnailSource = ImageSource.FromFile(pageImagePath),
                                PageNumber = i + 1,
                                Rotation = 0
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PdfService] Android ExtractPdfPagesAsync error: {ex.Message}");
                }
#endif
#if WINDOWS
                try
                {
                    var storageFile = StorageFile.GetFileFromPathAsync(pdfPath).AsTask().GetAwaiter().GetResult();
                    var pdfDoc = PdfDocument.LoadFromFileAsync(storageFile).AsTask().GetAwaiter().GetResult();
                    uint pageCount = pdfDoc.PageCount;

                    for (uint i = 0; i < pageCount; i++)
                    {
                        using var page = pdfDoc.GetPage(i);
                        string pageImagePath = Path.Combine(cacheDir, $"pdf_extracted_{Guid.NewGuid()}_{i + 1}.jpg");

                        using var memStream = new InMemoryRandomAccessStream();
                        var renderOptions = new PdfPageRenderOptions
                        {
                            DestinationWidth = (uint)Math.Max(1, (int)(page.Size.Width * 2)),
                            DestinationHeight = (uint)Math.Max(1, (int)(page.Size.Height * 2))
                        };

                        page.RenderToStreamAsync(memStream, renderOptions).AsTask().GetAwaiter().GetResult();

                        using var readStream = memStream.AsStreamForRead();
                        using var fileStream = File.Create(pageImagePath);
                        readStream.CopyTo(fileStream);

                        list.Add(new PdfPageItem
                        {
                            ImagePath = pageImagePath,
                            DisplayName = $"Page {i + 1}",
                            ThumbnailSource = ImageSource.FromFile(pageImagePath),
                            PageNumber = (int)(i + 1),
                            Rotation = 0
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PdfService] Windows ExtractPdfPagesAsync error: {ex.Message}");
                }
#endif
#if IOS || MACCATALYST
                try
                {
                    using var url = Foundation.NSUrl.FromFilename(pdfPath);
                    using var pdfDoc = new PdfKit.PdfDocument(url);
                    if (pdfDoc != null)
                    {
                        nint pageCount = pdfDoc.PageCount;
                        for (nint i = 0; i < pageCount; i++)
                        {
                            using var page = pdfDoc.GetPage(i);
                            if (page != null)
                            {
                                var rect = page.GetBoundsForBox(PdfKit.PdfDisplayBox.Media);
                                var size = new CoreGraphics.CGSize(rect.Width * 2, rect.Height * 2);
                                using var img = page.GetThumbnail(size, PdfKit.PdfDisplayBox.Media);
                                if (img != null)
                                {
                                    string pageImagePath = Path.Combine(cacheDir, $"pdf_extracted_{Guid.NewGuid()}_{i + 1}.jpg");
                                    using var jpegData = img.AsJPEG(0.9f);
                                    if (jpegData != null)
                                    {
                                        jpegData.Save(pageImagePath, true);
                                        list.Add(new PdfPageItem
                                        {
                                            ImagePath = pageImagePath,
                                            DisplayName = $"Page {i + 1}",
                                            ThumbnailSource = ImageSource.FromFile(pageImagePath),
                                            PageNumber = (int)(i + 1),
                                            Rotation = 0
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PdfService] iOS ExtractPdfPagesAsync error: {ex.Message}");
                }
#endif
                return list;
            });
        }

        /// <summary>
        /// Génère une image de page blanche (format standard A4) pour insertion dans une partition.
        /// </summary>
        public async Task<PdfPageItem> CreateBlankPageItemAsync(string cacheDir, int pageNumber, int width = 1240, int height = 1754)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                string pageImagePath = Path.Combine(cacheDir, $"pdf_blank_{Guid.NewGuid()}_{pageNumber}.jpg");
                using (var blank = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb24>(width, height))
                {
                    blank.Mutate(ctx => ctx.BackgroundColor(SixLabors.ImageSharp.Color.White));
                    blank.SaveAsJpeg(pageImagePath, new JpegEncoder { Quality = 90 });
                }

                return new PdfPageItem
                {
                    ImagePath = pageImagePath,
                    DisplayName = $"Page Blanche {pageNumber}",
                    ThumbnailSource = ImageSource.FromFile(pageImagePath),
                    PageNumber = pageNumber,
                    Rotation = 0
                };
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
                byte[] jpegBytes;
                int width;
                int height;

                try
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

                    // Limiter la taille maximale à 2480px (équivalent 300 DPI A4) pour économiser la mémoire et accélérer le traitement
                    int maxDimension = 2480;
                    if (sourceImage.Width > maxDimension || sourceImage.Height > maxDimension)
                    {
                        sourceImage.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new SixLabors.ImageSharp.Size(maxDimension, maxDimension),
                            Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max
                        }));
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
                        Quality = 90,
                        ColorType = JpegColorType.Rgb
                    });

                    jpegBytes = ms.ToArray();
                    width = rgbImage.Width;
                    height = rgbImage.Height;
                }
                catch (Exception)
                {
#if ANDROID
                    // Fallback natif Android (supporte HEIC, WebP, etc.)
                    using var bmp = BitmapFactory.DecodeFile(item.ImagePath!);
                    if (bmp == null) throw;

                    int rot = (item.Rotation % 360 + 360) % 360;
                    Bitmap finalBmp = bmp;
                    if (rot != 0)
                    {
                        var matrix = new Matrix();
                        matrix.PostRotate(rot);
                        finalBmp = Bitmap.CreateBitmap(bmp, 0, 0, bmp.Width, bmp.Height, matrix, true);
                    }

                    using var ms = new MemoryStream();
                    finalBmp.Compress(Bitmap.CompressFormat.Jpeg!, 90, ms);
                    jpegBytes = ms.ToArray();
                    width = finalBmp.Width;
                    height = finalBmp.Height;

                    if (finalBmp != bmp) finalBmp.Dispose();
#else
                    throw;
#endif
                }

                encodedPages.Add((jpegBytes, width, height));
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
