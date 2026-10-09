using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CyberpunkRED_Generator
{
    public class PdfPageImage
    {
        public byte[] Jpeg;        // готовый JPEG (RGB или оттенки серого)
        public int Width, Height;  // размер в пикселях
        public bool Gray;
    }

    // минимальный PDF: каждая страница — одно изображение JPEG, вписанное в альбомный A4
    public static class PdfWriter
    {
        public const double PageWidth = 841.89;   // A4 альбомная, пункты
        public const double PageHeight = 595.28;
        private const double Margin = 14;

        public static byte[] Build(IList<PdfPageImage> pages, string title)
        {
            var ms = new MemoryStream();
            var offsets = new List<long>();
            int n = pages.Count;

            void Write(string s) { var b = Encoding.ASCII.GetBytes(s); ms.Write(b, 0, b.Length); }
            void Begin(int id) { offsets.Add(ms.Position); Write($"{id} 0 obj\n"); }
            string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

            Write("%PDF-1.4\n%âãÏÓ\n");

            // объекты: 1 каталог, 2 страницы, 3 info, затем на каждую страницу по три (страница, содержимое, картинка)
            Begin(1); Write("<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            var kids = new StringBuilder();
            for (int i = 0; i < n; i++) kids.Append($"{4 + 3 * i} 0 R ");
            Begin(2); Write($"<< /Type /Pages /Kids [ {kids}] /Count {n} >>\nendobj\n");

            Begin(3); Write($"<< /Title <{Utf16Hex(title ?? "")}> /Producer (CyberpunkGen) >>\nendobj\n");

            for (int i = 0; i < n; i++)
            {
                var p = pages[i];
                int pageId = 4 + 3 * i, contentId = pageId + 1, imageId = pageId + 2;

                double availW = PageWidth - 2 * Margin, availH = PageHeight - 2 * Margin;
                double scale = System.Math.Min(availW / p.Width, availH / p.Height);
                double w = p.Width * scale, h = p.Height * scale;
                double x = (PageWidth - w) / 2, y = (PageHeight - h) / 2;

                Begin(pageId);
                Write($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {F(PageWidth)} {F(PageHeight)}] /Resources << /XObject << /Im0 {imageId} 0 R >> >> /Contents {contentId} 0 R >>\nendobj\n");

                string content = $"q {F(w)} 0 0 {F(h)} {F(x)} {F(y)} cm /Im0 Do Q\n";
                Begin(contentId);
                Write($"<< /Length {content.Length} >>\nstream\n{content}endstream\nendobj\n");

                Begin(imageId);
                Write($"<< /Type /XObject /Subtype /Image /Width {p.Width} /Height {p.Height} /ColorSpace /{(p.Gray ? "DeviceGray" : "DeviceRGB")} /BitsPerComponent 8 /Filter /DCTDecode /Length {p.Jpeg.Length} >>\nstream\n");
                ms.Write(p.Jpeg, 0, p.Jpeg.Length);
                Write("\nendstream\nendobj\n");
            }

            long xref = ms.Position;
            int total = offsets.Count + 1;
            Write($"xref\n0 {total}\n0000000000 65535 f \n");
            foreach (long o in offsets) Write(o.ToString("D10") + " 00000 n \n");
            Write($"trailer\n<< /Size {total} /Root 1 0 R /Info 3 0 R >>\nstartxref\n{xref}\n%%EOF\n");
            return ms.ToArray();
        }

        // строка в UTF-16BE с BOM, как требует PDF для текста вне ASCII
        private static string Utf16Hex(string s)
        {
            var sb = new StringBuilder("FEFF");
            foreach (char c in s) sb.Append(((int)c).ToString("X4"));
            return sb.ToString();
        }
    }
}
