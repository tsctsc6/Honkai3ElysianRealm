using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spire.OCR;

namespace BH3浅层乐土
{
    internal class spireOCRpro
    {
        OcrScanner scanner = new OcrScanner();
        public string[] Text { get; private set; } = null;
        public bool Scan(Bitmap img)
        {
            bool rv = false;
            MemoryStream ms = new MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            rv = scanner.Scan(ms, OCRImageFormat.Bmp);
            Text = scanner.Text.ToString().Split('\n');
            Text[Text.Length - 1] = Text[Text.Length - 1].Substring
                (0, Text[Text.Length - 1].LastIndexOf
                ("Evaluation Warning : The version can be used only for evaluation purpose..."));
            return rv;
        }
    }
}
