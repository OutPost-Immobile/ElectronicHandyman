using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicHandyman.Avalonia.Services;
using OpenCvSharp;
using SkiaSharp;
using Svg.Skia;

namespace ElectronicHandyman.Avalonia.ViewModels;

public record ChipResultItem(string ChipName, BoundingBox Box, string? SvgContent, bool IsSuccess, string? Error);

public partial class ResultViewModel : ViewModelBase
{
    private Bitmap? _resultImage;
    private Bitmap? _pinoutImage;
    private string _statusText = "";
    private List<ChipResultItem> _chips = new();
    private ChipResultItem? _selectedChip;
    private readonly Action _goBack;
    private double _zoomLevel = 1.0;
    private int _originalImageWidth;
    private int _originalImageHeight;

    public ResultViewModel(byte[] originalImage, List<BoundingBox> boxes, List<ChipIdentificationResult> results, Action goBack)
    {
        _goBack = goBack;
        BuildChipList(boxes, results);
        RenderResultImage(originalImage);
    }

    public ResultViewModel() : this(Array.Empty<byte>(), new(), new(), () => { }) { }

    public Bitmap? ResultImage
    {
        get => _resultImage;
        private set { var old = _resultImage; if (SetProperty(ref _resultImage, value)) old?.Dispose(); }
    }

    public Bitmap? PinoutImage
    {
        get => _pinoutImage;
        private set { var old = _pinoutImage; if (SetProperty(ref _pinoutImage, value)) old?.Dispose(); }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public List<ChipResultItem> Chips
    {
        get => _chips;
        private set => SetProperty(ref _chips, value);
    }

    public ChipResultItem? SelectedChip
    {
        get => _selectedChip;
        set
        {
            if (SetProperty(ref _selectedChip, value) && value?.SvgContent != null)
            {
                RenderPinoutSvg(value.SvgContent);
            }
        }
    }

    [RelayCommand]
    private void GoBack() => _goBack();

    [RelayCommand]
    private void SelectChip(ChipResultItem chip)
    {
        SelectedChip = chip;
    }

    [RelayCommand]
    private void ZoomIn()
    {
        _zoomLevel = Math.Min(_zoomLevel + 0.25, 4.0);
        OnPropertyChanged(nameof(ImageDisplayWidth));
        OnPropertyChanged(nameof(ImageDisplayHeight));
        OnPropertyChanged(nameof(ZoomText));
    }

    [RelayCommand]
    private void ZoomOut()
    {
        _zoomLevel = Math.Max(_zoomLevel - 0.25, 0.25);
        OnPropertyChanged(nameof(ImageDisplayWidth));
        OnPropertyChanged(nameof(ImageDisplayHeight));
        OnPropertyChanged(nameof(ZoomText));
    }

    [RelayCommand]
    private void ZoomReset()
    {
        _zoomLevel = 1.0;
        OnPropertyChanged(nameof(ImageDisplayWidth));
        OnPropertyChanged(nameof(ImageDisplayHeight));
        OnPropertyChanged(nameof(ZoomText));
    }

    public double ImageDisplayWidth => _originalImageWidth * _zoomLevel;
    public double ImageDisplayHeight => _originalImageHeight * _zoomLevel;
    public string ZoomText => $"{_zoomLevel:P0}";

    private void BuildChipList(List<BoundingBox> boxes, List<ChipIdentificationResult> results)
    {
        var chips = new List<ChipResultItem>();
        for (int i = 0; i < boxes.Count && i < results.Count; i++)
        {
            var r = results[i];
            var displayName = r.IsSuccess
                ? $"#{i + 1} — {r.ChipName}"
                : $"#{i + 1} — Not identified";
            chips.Add(new ChipResultItem(
                displayName,
                boxes[i],
                r.SvgContent,
                r.IsSuccess,
                r.ErrorMessage));
        }
        Chips = chips;

        var identified = chips.Count(c => c.IsSuccess);
        StatusText = $"{identified}/{chips.Count} chips identified";

        SelectedChip = chips.FirstOrDefault(c => c.IsSuccess) ?? chips.FirstOrDefault();
    }

    private void RenderResultImage(byte[] originalImage)
    {
        try
        {
            using var mat = Cv2.ImDecode(originalImage, ImreadModes.Color);
            if (mat.Empty()) return;

            for (int i = 0; i < _chips.Count; i++)
            {
                var chip = _chips[i];
                var color = chip.IsSuccess ? new Scalar(0, 255, 0) : new Scalar(0, 0, 255);
                var rect = new Rect(chip.Box.X, chip.Box.Y, chip.Box.Width, chip.Box.Height);
                Cv2.Rectangle(mat, rect, color, 3);

                // Draw number circle - scale based on image size
                var number = (i + 1).ToString();
                int radius = Math.Max(30, mat.Width / 30);
                var center = new OpenCvSharp.Point(chip.Box.X + radius + 6, chip.Box.Y + radius + 6);
                Cv2.Circle(mat, center, radius, new Scalar(0, 0, 0), -1); // black outline
                Cv2.Circle(mat, center, radius - 3, color, -1); // colored fill
                double fontScale = radius / 20.0;
                var textSize = Cv2.GetTextSize(number, HersheyFonts.HersheySimplex, fontScale, 2, out _);
                Cv2.PutText(mat, number,
                    new OpenCvSharp.Point(center.X - textSize.Width / 2, center.Y + textSize.Height / 2),
                    HersheyFonts.HersheySimplex, fontScale, new Scalar(255, 255, 255), 3);

                // Draw chip name below box
                if (chip.IsSuccess)
                {
                    double nameScale = Math.Max(0.6, mat.Width / 1500.0);
                    Cv2.PutText(mat, chip.ChipName.Replace($"#{i + 1} — ", ""),
                        new OpenCvSharp.Point(chip.Box.X, chip.Box.Y + chip.Box.Height + (int)(25 * nameScale)),
                        HersheyFonts.HersheySimplex, nameScale, color, 2);
                }
            }

            var jpegBytes = mat.ImEncode(".jpg", new ImageEncodingParam(ImwriteFlags.JpegQuality, 90));
            using var stream = new MemoryStream(jpegBytes);
            ResultImage = new Bitmap(stream);
            _originalImageWidth = mat.Width;
            _originalImageHeight = mat.Height;
            OnPropertyChanged(nameof(ImageDisplayWidth));
            OnPropertyChanged(nameof(ImageDisplayHeight));
        }
        catch (Exception ex)
        {
            StatusText = $"Render error: {ex.Message}";
        }
    }

    private void RenderPinoutSvg(string svgContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(svgContent) || !svgContent.TrimStart().StartsWith("<"))
            {
                PinoutImage = null;
                return;
            }

            using var svg = new SKSvg();
            svg.FromSvg(svgContent);
            if (svg.Picture == null) { PinoutImage = null; return; }

            var bounds = svg.Picture.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0) { PinoutImage = null; return; }

            // Scale SVG to be at least 1200px wide for readability
            float targetWidth = 1200f;
            float scale = targetWidth / bounds.Width;
            int w = (int)(bounds.Width * scale);
            int h = (int)(bounds.Height * scale);

            using var surface = SKSurface.Create(new SKImageInfo(w, h));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.Scale(scale);
            canvas.DrawPicture(svg.Picture);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());
            PinoutImage = new Bitmap(stream);
        }
        catch
        {
            PinoutImage = null;
        }
    }
}
