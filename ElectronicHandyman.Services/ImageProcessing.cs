using OpenCvSharp;
using RapidOcrNet;
using SkiaSharp;

namespace Services;

public class ImageProcessing
{
    private static readonly string ProcessedDir = "/home/kollibroman/Studia/ElectronicHandyman/output/processed";
    private static int _counter;

    private static RapidOcr? _ocrEngine;
    private static readonly object _lock = new();

    private static RapidOcr EnsureEngineInitialized()
    {
        if (_ocrEngine is not null)
            return _ocrEngine;

        lock (_lock)
        {
            if (_ocrEngine is not null)
                return _ocrEngine;

            string modelDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");

            _ocrEngine = new RapidOcr();
            _ocrEngine.InitModels(
                Path.Combine(modelDir, "en_PP-OCRv3_det_infer.onnx"),
                Path.Combine(modelDir, "ch_ppocr_mobile_v2.0_cls_infer.onnx"),
                Path.Combine(modelDir, "en_PP-OCRv3_rec_infer.onnx"),
                Path.Combine(modelDir, "ppocr_keys_v1.txt"),
                8
            );

            return _ocrEngine;
        }
    }

    public static string ProcessImage(byte[] imageBytes, string? saveProcessedPath = null)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return string.Empty;

        Console.WriteLine("|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||");

        // 1. Dekodowanie do skali szarości
        using var src = Cv2.ImDecode(imageBytes, ImreadModes.Grayscale);
        if (src.Empty())
            return string.Empty;

        // 2. CLAHE - Wyciąga jasne punkty graweru laserowego z ciemnego tła
        using var clahe = Cv2.CreateCLAHE(clipLimit: 3.0, tileGridSize: new Size(8, 8));
        using var claheResult = new Mat();
        clahe.Apply(src, claheResult);

        // 3. Powiększenie x3 (optymalne dla czytelności pojedynczych liter)
        using var resized = new Mat();
        Cv2.Resize(claheResult, resized, new Size(0, 0), 3.0, 3.0, InterpolationFlags.Cubic);

        // 4. Określenie typu układu scalonego (ciemny vs jasny)
        var mean = Cv2.Mean(resized);
        bool isDarkBackground = mean.Val0 < 128;

        // 5. INWERSJA - zamiana jasnego tekstu na ciemny tekst na jasnym tle
        using var inverted = new Mat();
        if (isDarkBackground)
        {
            Cv2.BitwiseNot(resized, inverted);
        }
        else
        {
            resized.CopyTo(inverted);
        }

        // 6. Gaussian Blur - miękkie łączenie punktów wypalonych laserem
        using var blurred = new Mat();
        Cv2.GaussianBlur(inverted, blurred, new Size(5, 5), 0);

        // 7. Hojny biały margines - "powietrze" ułatwiające detekcję brzegową OCR
        using var padded = new Mat();
        Cv2.CopyMakeBorder(blurred, padded, 40, 40, 40, 40, BorderTypes.Constant, new Scalar(255));

        // Zapis obrazu kontrolnego
        try
        {
            Directory.CreateDirectory(ProcessedDir);
            var filename = saveProcessedPath ??
                           Path.Combine(ProcessedDir, $"processed_{Interlocked.Increment(ref _counter):D4}.png");

            Cv2.ImWrite(filename, padded);
            Console.WriteLine($"Saved processed image: {filename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save processed image: {ex.Message}");
        }

        // 8. Test 4 rotacji z nowym systemem inteligentnej oceny tokenów (słów)
        string bestText = "";
        float bestConf = -1;
        int bestRotationScore = -999;
        List<string> bestTokens = new();

        for (int rotation = 0; rotation < 4; rotation++)
        {
            using var rotatedMat = rotation == 0 ? padded.Clone() : RotateImage(padded, rotation);

            var processedBytes = rotatedMat.ImEncode(".png");
            using var skBitmap = SKBitmap.Decode(processedBytes);

            var (text, conf) = RunOcrOnImage(skBitmap);
            
            var tokens = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Punktujemy każde słowo, przekazując również jego INDEKS (pozycję w odczytanym tekście)
            var scoredTokens = tokens
                .Select((t, index) => new { Token = t, Index = index, Score = ScoreToken(t, index) })
                .Where(x => x.Score > 20) // Podniesiony próg, żeby odciąć absolutne śmieci
                .OrderByDescending(x => x.Score)
                .ToList();

            // Bierzemy 2 słowa o najwyższym stopniu "układowości"
            var top2 = scoredTokens.Take(2).ToList();
            int rotationScore = top2.Sum(x => x.Score);

            Console.WriteLine($"  Rotation {rotation * 90}°: [{text.Replace('\n', ' ')}] conf={conf:F2} score={rotationScore}");

            if (rotationScore > bestRotationScore || (rotationScore == bestRotationScore && conf > bestConf))
            {
                bestRotationScore = rotationScore;
                bestConf = conf;
                bestText = text;
                
                // KLUCZOWE: Przywracamy oryginalną kolejność słów z fizycznego nadruku (od góry do dołu)
                bestTokens = top2.OrderBy(x => x.Index).Select(x => x.Token).ToList();
            }
        }

        Console.WriteLine($"Raw OCR (best): [{bestText.Replace('\n', ' ')}] conf={bestConf:F2}");

        // 9. Ekstrakcja nazwy układu (bierzemy tokeny o najwyższym stopniu dopasowania)
        string chipName = bestTokens.Count > 0 
            ? string.Join(" ", bestTokens) 
            : ExtractChipName(bestText);

        Console.WriteLine($"Chip name: [{chipName}]");

        // 10. Normalizacja końcowa
        var normalizedText = OcrTextNormalizer.Normalize(chipName);
        Console.WriteLine($"Normalized: [{normalizedText}]");

        return normalizedText;
    }

    private static (string text, float confidence) RunOcrOnImage(SKBitmap skBitmap)
    {
        var engine = EnsureEngineInitialized();

        // Używamy bezpiecznych, stabilnych opcji domyślnych (wyłącza wadliwy moduł 'cls')
        var result = engine.Detect(skBitmap, RapidOcrOptions.Default);

        if (result == null || result.TextBlocks == null || result.TextBlocks.Length == 0)
            return ("", 0f);

        var validBlocks = result.TextBlocks
            .Where(b => b.BoxScore > 0.50f)
            .ToList();

        if (validBlocks.Count == 0)
            return ("", 0f);

        var text = string.Join("\n", validBlocks.Select(b => string.Join("", b.Chars)));
        var avgConfidence = validBlocks.Average(b => b.BoxScore);

        return (text, (float)avgConfidence);
    }

    private static Mat RotateImage(Mat src, int rotationCount)
    {
        var rotated = src.Clone();
        for (int i = 0; i < rotationCount; i++)
        {
            var temp = new Mat();
            Cv2.Rotate(rotated, temp, RotateFlags.Rotate90Clockwise);
            rotated.Dispose();
            rotated = temp;
        }
        return rotated;
    }

    private static int ScoreToken(string token, int index)
    {
        if (string.IsNullOrWhiteSpace(token)) return 0;

        string clean = token.Trim('[', ']', '(', ')', '{', '}', '.', ',', '+', '*', '#', '!', '@', ':', ';');
        if (clean.Length < 4) return 0; // Odrzucamy drobnice laminatowe (U10, TP5)

        int upperCount = clean.Count(char.IsUpper);
        int digitCount = clean.Count(char.IsDigit);
        int lowerCount = clean.Count(char.IsLower);
        int specialCount = clean.Count(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_');

        // Baza: duże litery mnożymy mocniej, małe litery karzemy łagodniej (na wypadek, gdyby OCR pomylił '6' z 'b')
        int score = (upperCount * 2) + digitCount - (lowerCount * 2) - (specialCount * 3);

        bool hasLetters = upperCount > 0;
        bool hasDigits = digitCount > 0;

        if (hasLetters && hasDigits) 
            score += 20;

        // Skrócony wymóg długości: 5 znaków łapie popularne prefiksy (STM32, LM358, NE555)
        if (clean.Length >= 5 && clean.Length <= 16) 
            score += 10;

        // PUŁAPKA NA KODY PRODUKCYJNE (Batch codes):
        // Odcinamy punkty, jeśli wyraz jest w większości zdominowany przez cyfry
        if (digitCount > upperCount * 2)
            score -= 15;

        // BONUS POZYCYJNY: Najważniejsze informacje są na samej górze układu.
        // Jeśli słowo jest przeczytane jako jedno z pierwszych, dostaje ekstra punkty.
        score += Math.Max(0, 15 - (index * 3));

        return score;
    }

    private static string ExtractChipName(string rawOcrText)
    {
        if (string.IsNullOrWhiteSpace(rawOcrText))
            return string.Empty;

        var lines = rawOcrText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Take(2)
            .ToArray();

        return string.Join("", lines);
    }
}