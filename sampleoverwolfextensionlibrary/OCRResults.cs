using System;
using System.Collections.Generic;
using System.Text;

namespace SampleOverwolfExtensionLibrary.OCR
{
    public class OCRResults : IEquatable<OCRResults>
    {
        private string m_filename;
        private string m_ocrOutput;
        private string m_result;

        public OCRResults(string filename, string ocrOutput, string result)
        {
            m_filename = filename;
            m_ocrOutput = ocrOutput;
            m_result = result;
        }

        public string Filename
        {
            get { return m_filename; }
            set { m_filename = value; }
        }

        public string OCROutput
        {
            get { return m_ocrOutput; }
            set { m_ocrOutput = value; }
        }

        public string Result
        {
            get { return m_result; }
            set { m_result = value; }
        }

        public bool Equals(OCRResults other)
        {
            return Filename.Equals(other.Filename) && OCROutput.Equals(other.OCROutput) && Result.Equals(other.Result);
        }
    }
}
