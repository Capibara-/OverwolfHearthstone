﻿using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using Tesseract;

namespace SampleOverwolfExtensionLibrary.OCR
{
    class OCREngine : IDisposable
    {
        private static OCREngine m_instance = null;
        private static object SYNC_OBJ = new object();
        private string m_jsonCardsFilePath = null;
        private List<string> m_cards = null;
        private TesseractEngine m_ocrEngine = null;
        private bool m_disposed = false;

        private static readonly ILog logger = LogManager.GetLogger(typeof(OCREngine));

        private OCREngine()
        {
            m_ocrEngine = new TesseractEngine(Configuration.Instance.OCR.TesseractDataPath,
                "eng", EngineMode.TesseractAndCube);
            m_jsonCardsFilePath = Configuration.Instance.JSONCardsFilePath;
            m_cards = new List<string>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Only dispose once:
            if (!m_disposed)
            {
                // Don't dispose if called from finalizer or if m_ocrEngine is null:
                if (disposing && m_ocrEngine != null)
                {
                    m_ocrEngine.Dispose();
                    m_ocrEngine = null;
                }
                m_disposed = true;
            }
        }

        public static OCREngine Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (SYNC_OBJ)
                    {
                        if (m_instance == null)
                        {
                            m_instance = new OCREngine();
                        }
                    }
                }
                return m_instance;
            }
        }

        public string JSONCardsFilePath
        {
            get { return m_jsonCardsFilePath; }
            set { m_jsonCardsFilePath = value; }
        }

        // Parses out the cards data from a JSON file. Only returns cards where predicate(card) == true.
        public List<string> ParseCardsFromJSON(string path, Func<Card, bool> predicate)
        {
            List<string> retVal = new List<string>();
            var dynObj = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(path));
            foreach (JToken token in dynObj.Children())
            {
                if (token is JProperty)
                {
                    var cards = JsonConvert.DeserializeObject<List<Card>>(token.First.ToString());
                    foreach (Card card in cards)
                    {
                        if (predicate(card))
                        {
                            retVal.Add(card.Name);
                        }
                    }
                }
            }
            return retVal;
        }

        // A preidcate used in ParseCardsFromJSON to only select cards that are selectable for a deck.
        private bool isPlayableCard(Card c)
        {
            return c.ID != null && (c.Type == "Minion" || c.Type == "Spell");
        }

        // Performs OCR on the given image.
        public string PerformOCR(string imagePath, bool isSingleChar)
        {
            string retVal = string.Empty;
            using (var img = Pix.LoadFromFile(imagePath))
            {
                if (isSingleChar)
                {
                    using (var page = m_ocrEngine.Process(img, Tesseract.PageSegMode.SingleChar))
                    {
                        retVal = page.GetText();
                    }
                }
                else
                {
                    using (var page = m_ocrEngine.Process(img))
                    {
                        retVal = page.GetText();
                    }
                }
            }
            return retVal;
        }

        public List<OCRResults> AnalyzeImages(string imagesFolder)
        {
            List<OCRResults> retVal = new List<OCRResults>();
            if (!Directory.Exists(imagesFolder))
            {
                return new List<OCRResults>();
            }

            List<string> names = ParseCardsFromJSON(m_jsonCardsFilePath, isPlayableCard);
            string cardName = string.Empty;
            string cardNumber = string.Empty;
            bool isDouble = false;

            foreach (string fileName in Directory.GetFiles(imagesFolder))
            {
                cardNumber = recognizeStripNumber(fileName);
                // Calculate the card name with the minimum Damerau–Levenshtein distance:
                cardName = recognizeStripName(fileName);
                int index = getMinDamLevIndex(cardName.Trim(new char[] { '\n', '\r' }), names);
                if (!string.IsNullOrEmpty(cardName))
                {
                    isDouble = (string.IsNullOrEmpty(cardNumber) ? false : true);
                    retVal.Add(new OCRResults(fileName, cardName, names[index], isDouble));
                }
            }
            return retVal;
        }
        public List<OCRResults> AnalyzeImages(List<Bitmap> strips)
        {
            List<OCRResults> retVal = new List<OCRResults>();
            List<string> names = ParseCardsFromJSON(m_jsonCardsFilePath, isPlayableCard);
            string cardName = string.Empty;
            string cardNumber = string.Empty;
            bool isDouble = false;

            foreach (Bitmap strip in strips)
            {
                cardNumber = recognizeStripNumber(strip);
                // Calculate the card name with the minimum Damerau–Levenshtein distance:
                cardName = recognizeStripName(strip);
                int index = getMinDamLevIndex(cardName.Trim(new char[] { '\n', '\r' }), names);
                if (!string.IsNullOrEmpty(cardName))
                {
                    isDouble = (string.IsNullOrEmpty(cardNumber) ? false : true);
                    retVal.Add(new OCRResults("In memory.", cardName, names[index], isDouble));
                }
            }
            return retVal;
        }

        // Called for each strip, first cleans the image of noise and then performs OCR on the strip.
        private string recognizeStripName(string fileName, string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ\'\"")
        {
            string noiseFreeImage = Path.Combine(Configuration.Instance.TempFolder, "temp.png");

            // Make image white text on black background:
            cleanImageAndSave(fileName, noiseFreeImage, isNotInWhiteRange);

            // Use only specific characters:
            m_ocrEngine.SetVariable("tessedit_char_whitelist", allowedChars);
            string retVal = PerformOCR(noiseFreeImage, false);
            File.Delete(noiseFreeImage);
            return retVal;
        }
        private string recognizeStripName(Bitmap strip, string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ\'\"")
        {
            string noiseFreeImage = Path.Combine(Configuration.Instance.TempFolder, "temp.png");

            // Make image white text on black background:
            Bitmap cleanStrip = cleanImage(strip, isNotInWhiteRange);
            cleanStrip.Save(noiseFreeImage);

            // Use only specific characters:
            m_ocrEngine.SetVariable("tessedit_char_whitelist", allowedChars);
            string retVal = PerformOCR(noiseFreeImage, false);
            File.Delete(noiseFreeImage);
            return retVal;
        }

        // Called for each strip, first cleans the image of noise and then performs OCR on the strip.
        private string recognizeStripNumber(string stripFileName, string allowedChars = "12")
        {
            if (!File.Exists(stripFileName))
            {
                return string.Empty;
            }
            Bitmap original = Image.FromFile(stripFileName) as Bitmap;
            return recognizeStripNumber(original);
        }
        private string recognizeStripNumber(Bitmap strip, string allowedChars = "12")
        {
            double numberToStripWidthRatio = Configuration.Instance.OCR.recognizeStripNumber.numberToStripWidthRatio;
            string tempImage = Path.Combine(Configuration.Instance.TempFolder, "temp.png");

            // Make image white text on black background:
            Bitmap noiseFreeBitmap = cleanImage(strip, isNotInYellowRange);

            // Crop image and leave only the number:
            int newStartPointForStrip = strip.Width - (int)Math.Round(strip.Width / numberToStripWidthRatio);
            Bitmap croppedImage = CropImage(noiseFreeBitmap, new Rectangle(newStartPointForStrip, 0, strip.Width - newStartPointForStrip, strip.Height));

            // TODO: Extern to config.
            int scaleFactor = 5;

            // Scale image by scaleFactor:
            int newWidth = (int)(croppedImage.Width * scaleFactor);
            int newHeight = (int)(croppedImage.Height * scaleFactor);
            using (var newImage = new Bitmap(newWidth, newHeight))
            {
                using (var graphics = Graphics.FromImage(newImage))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(croppedImage, new Rectangle(0, 0, newWidth, newHeight));
                    newImage.Save(tempImage);
                }
            }

            // Use only specific characters:
            m_ocrEngine.SetVariable("tessedit_char_whitelist", allowedChars);
            string retVal = PerformOCR(tempImage, true);

            // Remove temp files:
            File.Delete(tempImage);
            return retVal;
        }

        // Returns the index in candidates of the string that has the minimum Damerau–Levenshtein distance to input.
        private int getMinDamLevIndex(string input, List<string> candidates)
        {
            int retVal = 0;
            int minVal = int.MaxValue;
            int currentVal = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                currentVal = calculateDamLev(input, candidates[i]);
                if (currentVal < minVal)
                {
                    minVal = currentVal;
                    retVal = i;
                }
            }
            return retVal;
        }

        // Calculates the Damerau–Levenshtein Distance between strings s and t.
        private int calculateDamLev(string s, string t, int maxDistance = int.MaxValue)
        {
            if (String.IsNullOrEmpty(s))
            {
                return (t ?? "").Length;
            }

            if (String.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            // if strings of different lengths, ensure shorter string is in s. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (s.Length > t.Length)
            {
                var temp = s; s = t; t = temp; // swap s and t
            }
            int sLen = s.Length; // this is also the minimun length of the two strings
            int tLen = t.Length;

            // suffix common to both strings can be ignored
            while ((sLen > 0) && (s[sLen - 1] == t[tLen - 1])) { sLen--; tLen--; }

            int start = 0;
            if ((s[0] == t[0]) || (sLen == 0))
            { // if there's a shared prefix, or all s matches t's suffix
              // prefix common to both strings can be ignored
                while ((start < sLen) && (s[start] == t[start])) start++;
                sLen -= start; // length of the part excluding common prefix and suffix
                tLen -= start;

                // if all of shorter string matches prefix and/or suffix of longer string, then
                // edit distance is just the delete of additional characters present in longer string
                if (sLen == 0) return tLen;

                t = t.Substring(start, tLen); // faster than t[start+j] in inner loop below
            }
            int lenDiff = tLen - sLen;
            if ((maxDistance < 0) || (maxDistance > tLen))
            {
                maxDistance = tLen;
            }
            else if (lenDiff > maxDistance) return -1;

            var v0 = new int[tLen];
            var v2 = new int[tLen]; // stores one level further back (offset by +1 position)
            int j;
            for (j = 0; j < maxDistance; j++) v0[j] = j + 1;
            for (; j < tLen; j++) v0[j] = maxDistance + 1;

            int jStartOffset = maxDistance - (tLen - sLen);
            bool haveMax = maxDistance < tLen;
            int jStart = 0;
            int jEnd = maxDistance;
            char sChar = s[0];
            int current = 0;
            for (int i = 0; i < sLen; i++)
            {
                char prevsChar = sChar;
                sChar = s[start + i];
                char tChar = t[0];
                int left = i;
                current = left + 1;
                int nextTransCost = 0;
                // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i - lenDiff)
                // and the upper left diagonal + maxDistance cells (upper left is i)
                jStart += (i > jStartOffset) ? 1 : 0;
                jEnd += (jEnd < tLen) ? 1 : 0;
                for (j = jStart; j < jEnd; j++)
                {
                    int above = current;
                    int thisTransCost = nextTransCost;
                    nextTransCost = v2[j];
                    v2[j] = current = left; // cost of diagonal (substitution)
                    left = v0[j];    // left now equals current cost (which will be diagonal at next iteration)
                    char prevtChar = tChar;
                    tChar = t[j];
                    if (sChar != tChar)
                    {
                        if (left < current) current = left;   // insertion
                        if (above < current) current = above; // deletion
                        current++;
                        if ((i != 0) && (j != 0)
                            && (sChar == prevtChar)
                            && (prevsChar == tChar))
                        {
                            thisTransCost++;
                            if (thisTransCost < current) current = thisTransCost; // transposition
                        }
                    }
                    v0[j] = current;
                }
                if (haveMax && (v0[i + lenDiff] > maxDistance)) return -1;
            }
            return (current <= maxDistance) ? current : -1;
        }

        // Changes the color of each pixel where predicate(pixel) == true to newColor. All other pixels get colored absoluteOriginal.
        private Bitmap changeColor(Bitmap scrBitmap, Color newColor, Color absoluteOriginal, Func<Color, bool> predicate)
        {
            Color actulaColor;
            Bitmap newBitmap = new Bitmap(scrBitmap.Width, scrBitmap.Height);
            for (int i = 0; i < scrBitmap.Width; i++)
            {
                for (int j = 0; j < scrBitmap.Height; j++)
                {
                    actulaColor = scrBitmap.GetPixel(i, j);
                    Color color = (predicate(actulaColor) ? newColor : absoluteOriginal);
                    newBitmap.SetPixel(i, j, color);
                }
            }
            return newBitmap;
        }


        // Checks if color is white with specified variance (minVal, maxDiff, minAlpha).
        private bool isInWhiteRange(Color color, int minVal, int maxDiff, int minAlpha)
        {
            bool retVal = color.R >= minVal && color.G >= minVal && color.B >= minVal;
            retVal = retVal && Math.Abs(color.R - color.G) <= maxDiff && Math.Abs(color.R - color.B) <= maxDiff && Math.Abs(color.G - color.B) <= maxDiff;
            retVal = retVal && color.A > minAlpha;
            return retVal;
        }
        private bool isNotInWhiteRange(Color color)
        {
            int minVal = Configuration.Instance.OCR.isNotWhiteRange.minVal;
            int maxDiff = Configuration.Instance.OCR.isNotWhiteRange.maxDiff;
            int minAlpha = Configuration.Instance.OCR.isNotWhiteRange.minAlpha;
            return !isInWhiteRange(color, minVal, maxDiff, minAlpha);
        }

        // Checks if color is yellow with a given delta.
        private bool isInYellowRange(Color color, int delta)
        {
            bool retval = Math.Abs(color.R - Color.Yellow.R) < delta;
            retval = retval && Math.Abs(color.G - Color.Yellow.G) < delta;
            return retval && Math.Abs(color.B - Color.Yellow.B) < delta;
        }
        private bool isNotInYellowRange(Color color)
        {
            return !isInYellowRange(color, Configuration.Instance.OCR.isNotInYellowRange.delta);
        }

        // Crops the width of the image leaving only the strips and whatever is left to the right of them.
        public void CropWidthAndSave(string originalImage, string outputImage)
        {
            if (File.Exists(originalImage))
            {
                Bitmap src = Image.FromFile(originalImage) as Bitmap;
                double rightSideRatio = Configuration.Instance.OCR.CropWidth.rightSideRatio;//3.6432637571;
                double leftSideRatio = Configuration.Instance.OCR.CropWidth.leftSideRatio;//5.78313253;

                double newStartWidth = Math.Round(src.Width / rightSideRatio);
                double stripWidth = Math.Round(newStartWidth - src.Width / leftSideRatio);
                int stripStartPos = src.Width - (int)newStartWidth;

                CropImageAndSave(originalImage, outputImage, new Rectangle(stripStartPos, 0, (int)stripWidth, src.Height));
            }
        }
        public Bitmap CropWidth(string image)
        {
            if (!File.Exists(image))
            {
                // Return 1x1 empty image to avoid returning null.
                return new Bitmap(1, 1);
            }

            Bitmap src = Image.FromFile(image) as Bitmap;
            return CropWidth(src);
        }
        public Bitmap CropWidth(Bitmap src)
        {
            double rightSideRatio = Configuration.Instance.OCR.CropWidth.rightSideRatio;//3.6432637571;
            double leftSideRatio = Configuration.Instance.OCR.CropWidth.leftSideRatio;//5.78313253;

            double newStartWidth = Math.Round(src.Width / rightSideRatio);
            double stripWidth = Math.Round(newStartWidth - src.Width / leftSideRatio);
            int stripStartPos = src.Width - (int)newStartWidth;
            Rectangle rect = new Rectangle(stripStartPos, 0, (int)stripWidth, src.Height);

            return CropImage(src, rect);
        }


        // Splits the given image/image path to strips in order to send to the OCR engine.
        public void SplitToStripsAndSave(string widthCroppedImage, string outputFolder)
        {
            if (File.Exists(widthCroppedImage))
            {
                double deckNameRatio = Configuration.Instance.OCR.SplitToStrips.deckNameRatio;//9.391304347;
                double stripRatio = Configuration.Instance.OCR.SplitToStrips.stripRatio;//27.0;
                double bottomMenuRatio = Configuration.Instance.OCR.SplitToStrips.bottomMenuRatio;//9.6428571428571;

                Bitmap src = Image.FromFile(widthCroppedImage) as Bitmap;
                int deckNamePixelSize = (int)Math.Round(src.Height / deckNameRatio);
                int stripPixelSize = (int)Math.Round(src.Height / stripRatio);
                int bottomMenuPixelSize = (int)Math.Round(src.Height / bottomMenuRatio);
                int i = 0;

                while (src.Height - deckNamePixelSize - (i + 1) * stripPixelSize > bottomMenuPixelSize)
                {
                    int newY = deckNamePixelSize + i * stripPixelSize;
                    CropImageAndSave(widthCroppedImage, Path.Combine(outputFolder, "OutputStrip" + i + ".png"), new Rectangle(0, newY, src.Width, stripPixelSize));
                    i++;
                }
            }
        }
        public List<Bitmap> SplitToStrips(string widthCroppedImage)
        {
            Bitmap src;
            if (File.Exists(widthCroppedImage))
            {
                src = Image.FromFile(widthCroppedImage) as Bitmap;
            }
            else
            {
                src = new Bitmap(1, 1);
            }
            return SplitToStrips(src);
        }
        public List<Bitmap> SplitToStrips(Bitmap src)
        {
            List<Bitmap> retVal = new List<Bitmap>();
            double deckNameRatio = Configuration.Instance.OCR.SplitToStrips.deckNameRatio;//9.391304347;
            double stripRatio = Configuration.Instance.OCR.SplitToStrips.stripRatio;//27.0;
            double bottomMenuRatio = Configuration.Instance.OCR.SplitToStrips.bottomMenuRatio;//9.6428571428571;

            int deckNamePixelSize = (int)Math.Round(src.Height / deckNameRatio);
            int stripPixelSize = (int)Math.Round(src.Height / stripRatio);
            int bottomMenuPixelSize = (int)Math.Round(src.Height / bottomMenuRatio);
            int i = 0;

            while (src.Height - deckNamePixelSize - (i + 1) * stripPixelSize > bottomMenuPixelSize)
            {
                int newY = deckNamePixelSize + i * stripPixelSize;
                retVal.Add(CropImage(src, new Rectangle(0, newY, src.Width, stripPixelSize)));
                i++;
            }
            return retVal;

        }


        // Returns a black and white Bitmap where all pixels such that predicate(pixel) == true are black and rest are white.
        private Bitmap cleanImage(Bitmap inputBitmap, Func<Color, bool> predicate)
        {
            Bitmap retVal = changeColor(inputBitmap, Color.Black, Color.White, predicate);
            return retVal;
        }
        private void cleanImageAndSave(string inPath, string outPath, Func<Color, bool> predicate)
        {
            Bitmap srcBitmap = Image.FromFile(inPath) as Bitmap;
            Bitmap retVal = cleanImage(srcBitmap, predicate);
            retVal.Save(outPath);
        }


        // Crops a given image/imagePath to the dimentions defined by cropRect.
        public Bitmap CropImage(Bitmap originalImage, Rectangle cropRect)
        {
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(originalImage, new Rectangle(0, 0, target.Width, target.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }
            return target;
        }
        public Bitmap CropImage(string originalImage, Rectangle cropRect)
        {
            Bitmap src = Image.FromFile(originalImage) as Bitmap;
            return CropImage(src, cropRect);
        }
        public void CropImageAndSave(string originalImage, string outputImage, Rectangle cropRect)
        {
            Bitmap src = Image.FromFile(originalImage) as Bitmap;
            Bitmap retVal = CropImage(src, cropRect);
            retVal.Save(outputImage);
        }

        // Currently just a mock function to crop gabi's extra screen before starting OCR.
        private void cropUneededScreenAndSave(string screenshot, string output)
        {
            Rectangle rect = new Rectangle(1920, 160, 1920, 1080);
            CropImageAndSave(screenshot, output, rect);
        }
        private Bitmap cropUneededScreen(string screenshot)
        {
            Rectangle rect = new Rectangle(1920, 160, 1920, 1080);
            return CropImage(screenshot, rect);
        }

        // Entry point for module users.
        // Has 2 ways of operating, one saves all intermediary steps to disk the other only saves before the last step (required to make Tesseract behave well).
        // First crops the width of the images leaving only the strips and whatever is on the left.
        // Then splits the images into inidvidual strips and performs OCR on each strip.
        public List<OCRResults> HandleScreenshot(string screenshot, bool isMultipleScreens = false, bool isSaveToDisk = false)
        {
            Bitmap initialImage;
            List<OCRResults> retVal;

            if (isMultipleScreens)
            {
                // TODO: Decide if we handle multiple screens and if so implement logic that is not shitty.
                initialImage = cropUneededScreen(screenshot);
            }
            else
            {
                initialImage = Image.FromFile(screenshot) as Bitmap;
            }

            if (isSaveToDisk)
            {
                string initialPath = Path.Combine(Configuration.Instance.TempFolder, "initialImage.png");
                string croppedPath = Path.Combine(Configuration.Instance.TempFolder, "croppedImage.png");
                string stripDir = Path.Combine(Configuration.Instance.TempFolder, "strips");
                if (Directory.Exists(stripDir))
                {
                    Directory.Delete(stripDir, true);
                }
                Directory.CreateDirectory(stripDir);
                initialImage.Save(initialPath);



                CropWidthAndSave(initialPath, croppedPath);
                SplitToStripsAndSave(croppedPath, stripDir);
                retVal = AnalyzeImages(stripDir);
            }
            else
            {
                Bitmap croppedImage = CropWidth(initialImage);
                List<Bitmap> strips = SplitToStrips(croppedImage);
                retVal = AnalyzeImages(strips);
            }
            return retVal;
        }
    }
}
