using System.Text;
using UnityEngine;
using SD = System.Drawing;
using SD2 = System.Drawing.Drawing2D;

namespace Capy.Engine.DevTools.Features;

public class PixelArtMarkup {
    private const string BlockChar = "█";

    public string GenerateMarkup(Stream imageStream, Vector2 imageSize) {
        using MemoryStream ms = new();
        imageStream.Position = 0;
        imageStream.CopyTo(ms);
        ms.Position = 0;

        using SD.Image img = SD.Image.FromStream(ms);
        using SD.Bitmap bmp = new((int)imageSize.x, (int)imageSize.y);
        using (SD.Graphics g = SD.Graphics.FromImage(bmp)) {
            g.InterpolationMode = SD2.InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, 0, 0, (int)imageSize.x, (int)imageSize.y);
        }

        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"<mspace=1em>");

        for (int y = 0; y < bmp.Height; y++) {
            this.AppendRowMarkup(bmp, y, stringBuilder);
            stringBuilder.AppendLine();
        }

        stringBuilder.Append("</mspace>");
        return stringBuilder.ToString();
    }

    public List<string> GenerateRows(Stream imageStream, Vector2 imageSize) {
        using MemoryStream ms = new();
        imageStream.Position = 0;
        imageStream.CopyTo(ms);
        ms.Position = 0;

        using SD.Image img = SD.Image.FromStream(ms);
        using SD.Bitmap bmp = new((int)imageSize.x, (int)imageSize.y);
        using (SD.Graphics g = SD.Graphics.FromImage(bmp)) {
            g.InterpolationMode = SD2.InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, 0, 0, (int)imageSize.x, (int)imageSize.y);
        }

        List<string> rows = [];
        for (int y = 0; y < bmp.Height; y++) {
            StringBuilder stringBuilder = new();
            stringBuilder.Append($"<mspace=1em>");
            this.AppendRowMarkup(bmp, y, stringBuilder);
            stringBuilder.Append("</mspace>");
            rows.Add(stringBuilder.ToString());
        }

        return rows;
    }

    private void AppendRowMarkup(SD.Bitmap image, int y, StringBuilder sb) {
        sb.Append("<color=#00000000>\u200B</color>");
        int num = 0;
        string? lastHex = null;
        int count = 0;

        for (int i = 0; i < image.Width; i++) {
            SD.Color val = image.GetPixel(i, y);
            if (val.A < 128) {
                this.FlushColorBlock(sb, ref lastHex, ref count);
                num += 1;
                continue;
            }

            string text = $"#{val.R:X2}{val.G:X2}{val.B:X2}";
            if (num > 0) {
                sb.Append($"<space={num}em>");
                num = 0;
            }

            if (text == lastHex) {
                count++;
                continue;
            }

            this.FlushColorBlock(sb, ref lastHex, ref count);
            lastHex = text;
            count = 1;
        }

        this.FlushColorBlock(sb, ref lastHex, ref count);
        sb.Append("<color=#00000000>\u200B</color>");
    }

    private void FlushColorBlock(StringBuilder sb, ref string? lastHex, ref int count) {
        if (lastHex != null && count > 0) {
            sb.Append("<color=" + lastHex + ">");
            for (int i = 0; i < count; i++) sb.Append(BlockChar);
            sb.Append("</color>");
        }

        lastHex = null;
        count = 0;
    }
}
