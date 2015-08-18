using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        private int m_minVal = 0;
        private int m_maxDiff = 0;
        private int m_minAlpha = 0;

        private OCREngine()
        {
            m_cards = new List<string>();
            string tessdataPath = Configuration.Instance.OCR.TesseractDataPath;
            m_jsonCardsFilePath = Configuration.Instance.JSONCardsFilePath;
            m_minAlpha = Configuration.Instance.OCR.IsWhiteRangeMinAlpha;
            m_maxDiff = Configuration.Instance.OCR.IsWhiteRangeMaxDiff;
            m_minVal = Configuration.Instance.OCR.IsWhiteRangeMinVal;
            m_ocrEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.TesseractAndCube);

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

        public string PerformOCR(string imagePath)
        {
            string retVal = string.Empty;
            using (var img = Pix.LoadFromFile(imagePath))
            {
                using (var page = m_ocrEngine.Process(img))
                {
                    retVal = page.GetText();
                }
            }
            return retVal;
        }

        public List<OCRResults> AnalyzeImages(string imagesFolder)
        {
            List<OCRResults> retVal = new List<OCRResults>();
            if (!Directory.Exists(imagesFolder))
            {
                return retVal;
            }

            string noiseFreeImage = Path.Combine(imagesFolder, "temp.png");
            List<string> names = ParseCardsFromJSON(m_jsonCardsFilePath, isPlayableCard);
            string OCRedText = string.Empty;
            foreach (string fileName in Directory.GetFiles(imagesFolder))
            {
                // Make image white text on black background:
                cleanImage(fileName, noiseFreeImage);

                // Use only specific characters:
                m_ocrEngine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ\'\"");
                OCRedText = PerformOCR(noiseFreeImage);
                File.Delete(noiseFreeImage);
                // Calculate the card name with the minimum Damerau–Levenshtein distance:
                int index = getMinDamLevIndex(OCRedText.Trim(new char[] { '\n', '\r' }), names);
                retVal.Add(new OCRResults(fileName, OCRedText, names[index]));
            }
            return retVal;
        }

        private void cleanImage(string inPath, string outPath)
        {
            Bitmap bmp = (Bitmap)Image.FromFile(inPath);
            Bitmap newBMP = changeColor(bmp, Color.Black, Color.White, isNotInWhiteRange);
            newBMP.Save(outPath);
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

        private bool isNotInWhiteRange(Color color)
        {
            return !isInWhiteRange(color, m_minVal, m_maxDiff, m_minAlpha);
        }

        // Checks if color is white with specified variance (minVal, maxDiff, minAlpha).
        private bool isInWhiteRange(Color color, int minVal, int maxDiff, int minAlpha)
        {
            bool retVal = color.R >= minVal && color.G >= minVal && color.B >= minVal;
            retVal = retVal && Math.Abs(color.R - color.G) <= maxDiff && Math.Abs(color.R - color.B) <= maxDiff && Math.Abs(color.G - color.B) <= maxDiff;
            retVal = retVal && color.A > minAlpha;
            return retVal;
        }

        // A preidcate used in ParseCardsFromJSON to only select cards that are selectable for a deck.
        private bool isPlayableCard(Card c)
        {
            return c.ID != null && (c.Type == "Minion" || c.Type == "Spell");
        }
    }
}
