using System.Globalization;
using System.Text;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// Writes recipes as a PDF to print or keep, with no library: text only, on US Letter pages, in Helvetica and
/// Helvetica-Bold, two of the standard fonts every PDF reader has, so nothing is embedded. Those fonts cover
/// Windows-1252 (WinAnsiEncoding): English and western European letters, ½, ¼, °, curly quotes and dashes. Fractions
/// such as ⅓ are spelled "1/3", and any other character, such as Greek or Chinese, prints as "?". Photos are left
/// out. Lines wrap to the page, a heading is never left alone at the foot of a page, each recipe starts on a new
/// page, and every page is numbered. It is a helper of <see cref="TransformationEngine"/>, not an engine.
/// </summary>
internal static class RecipePdfWriter
{
    private const double PageWidth = 612;
    private const double PageHeight = 792;
    private const double Margin = 54;
    private const double TextWidth = PageWidth - (2 * Margin);
    private const double Bottom = Margin + 18; // room for the page number below

    private const double TitleSize = 20;
    private const double HeadingSize = 13;
    private const double BodySize = 11;
    private const double FactsSize = 10;
    private const double ListIndent = 18;

    /// <summary>Characters with no Windows-1252 byte that can still be written in letters that have one.</summary>
    private static readonly Dictionary<char, string> Spelled = new()
    {
        ['⅓'] = "1/3", ['⅔'] = "2/3", ['⅕'] = "1/5", ['⅖'] = "2/5", ['⅗'] = "3/5", ['⅘'] = "4/5", ['⅙'] = "1/6",
        ['⅚'] = "5/6", ['⅛'] = "1/8", ['⅜'] = "3/8", ['⅝'] = "5/8", ['⅞'] = "7/8", ['⁄'] = "/", ['−'] = "-",
        ['‐'] = "-", ['‑'] = "-", [' '] = " ", [' '] = " ", [' '] = " ", [' '] = " ", ['\t'] = " ",
        ['​'] = string.Empty, ['﻿'] = string.Empty, ['­'] = string.Empty,
    };

    /// <summary>The Windows-1252 bytes 0x80–0x9F, which differ from Latin-1; the rest of 0xA0–0xFF are the same.</summary>
    private static readonly Dictionary<char, byte> WinAnsiHigh = new()
    {
        ['€'] = 0x80, ['‚'] = 0x82, ['ƒ'] = 0x83, ['„'] = 0x84, ['…'] = 0x85, ['†'] = 0x86, ['‡'] = 0x87, ['ˆ'] = 0x88,
        ['‰'] = 0x89, ['Š'] = 0x8A, ['‹'] = 0x8B, ['Œ'] = 0x8C, ['Ž'] = 0x8E, ['‘'] = 0x91, ['’'] = 0x92, ['“'] = 0x93,
        ['”'] = 0x94, ['•'] = 0x95, ['–'] = 0x96, ['—'] = 0x97, ['˜'] = 0x98, ['™'] = 0x99, ['š'] = 0x9A, ['›'] = 0x9B,
        ['œ'] = 0x9C, ['ž'] = 0x9E, ['Ÿ'] = 0x9F,
    };

    // Glyph widths in thousandths of the font size, from Adobe's metrics for code points 32–126.
    private static readonly short[] RegularAscii =
    [
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556,
        556, 556, 556, 556, 278, 278, 584, 584, 584, 556, 1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667,
        556, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556, 333, 556,
        556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556, 556, 556, 333, 500, 278, 556, 500, 722,
        500, 500, 500, 334, 260, 334, 584,
    ];

    private static readonly short[] BoldAscii =
    [
        278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556,
        556, 556, 556, 556, 333, 333, 584, 584, 584, 611, 975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722,
        611, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556, 333, 556,
        611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611, 611, 611, 389, 556, 333, 611, 556, 778,
        556, 556, 500, 389, 280, 389, 584,
    ];

    /// <summary>
    /// Widths of the Windows-1252 symbols past ASCII. An accented letter is as wide as its plain letter, and anything
    /// not listed is taken as 1000, wider than it is, so a line is never measured short.
    /// </summary>
    private static readonly Dictionary<byte, short> HighWidths = new()
    {
        [0x80] = 556, [0x82] = 222, [0x84] = 333, [0x85] = 1000, [0x91] = 222, [0x92] = 222, [0x93] = 333, [0x94] = 333,
        [0x95] = 350, [0x96] = 556, [0x97] = 1000, [0x99] = 1000, [0xA0] = 278, [0xA2] = 556, [0xA3] = 556, [0xA9] = 737,
        [0xAE] = 737, [0xB0] = 400, [0xB5] = 556, [0xB7] = 278, [0xBC] = 834, [0xBD] = 834, [0xBE] = 834, [0xD7] = 584,
        [0xDF] = 611, [0xE6] = 889, [0xC6] = 1000, [0xF7] = 584, [0xF8] = 611, [0xD8] = 778,
    };

    /// <summary>The two fonts the pages use.</summary>
    internal enum Font
    {
        Regular,
        Bold,
    }

    /// <summary>The recipes, in the order given, as one PDF document. Each starts on a new page.</summary>
    public static byte[] Write(IReadOnlyList<Recipe> recipes)
    {
        var layout = new Layout();
        foreach (var recipe in recipes)
        {
            if (layout.Pages.Count > 0)
            {
                layout.NewPage();
            }

            Lay(layout, RecipeDocument.From(recipe));
        }

        if (layout.Pages.Count == 0)
        {
            layout.NewPage();
            layout.Paragraph("There are no recipes to show.", Font.Regular, BodySize);
        }

        var title = recipes.Count == 1 ? recipes[0].Name.Trim() : "ChefBot recipes";
        return Document(title, layout.Pages);
    }

    /// <summary>
    /// Text broken into lines no wider than <paramref name="width"/> points: at spaces where it can be, inside a word
    /// only when the word alone is too wide. Line breaks in the text are kept.
    /// </summary>
    public static IReadOnlyList<string> Wrap(string text, Font font, double size, double width)
    {
        var lines = new List<string>();
        foreach (var paragraph in Clean(text).Split('\n'))
        {
            var line = new StringBuilder();
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = line.Length == 0 ? word : $"{line} {word}";
                if (Measure(candidate, font, size) <= width)
                {
                    line.Clear().Append(candidate);
                    continue;
                }

                if (line.Length > 0)
                {
                    lines.Add(line.ToString());
                    line.Clear();
                }

                // A word wider than the line is split where it reaches the edge.
                var rest = word;
                while (Measure(rest, font, size) > width)
                {
                    var fits = 1;
                    while (fits < rest.Length && Measure(rest[..(fits + 1)], font, size) <= width)
                    {
                        fits++;
                    }

                    lines.Add(rest[..fits]);
                    rest = rest[fits..];
                }

                line.Append(rest);
            }

            lines.Add(line.ToString());
        }

        return lines;
    }

    /// <summary>How wide text is in points, set in <paramref name="font"/> at <paramref name="size"/>.</summary>
    public static double Measure(string text, Font font, double size) =>
        Encode(text).Sum(code => (double)Width(code, font)) * size / 1000;

    /// <summary>Text as Windows-1252 bytes, after spelling out the characters that have none; "?" for the rest.</summary>
    public static byte[] Encode(string text)
    {
        var bytes = new List<byte>(text.Length);
        foreach (var c in Clean(text))
        {
            bytes.Add(c switch
            {
                >= ' ' and <= '~' => (byte)c,
                >= ' ' and <= 'ÿ' => (byte)c,
                _ when WinAnsiHigh.TryGetValue(c, out var high) => high,
                _ => (byte)'?',
            });
        }

        return [.. bytes];
    }

    /// <summary>Text composed into whole letters (é, not e and an accent) with the spelled-out characters replaced.</summary>
    private static string Clean(string text)
    {
        var composed = text.Replace("\r\n", "\n", StringComparison.Ordinal).Normalize(NormalizationForm.FormC);
        var clean = new StringBuilder(composed.Length);
        foreach (var c in composed)
        {
            if (Spelled.TryGetValue(c, out var spelled))
            {
                clean.Append(spelled);
            }
            else if (c == '\n' || !char.IsControl(c))
            {
                clean.Append(c);
            }
        }

        return clean.ToString();
    }

    private static short Width(byte code, Font font)
    {
        var ascii = font == Font.Bold ? BoldAscii : RegularAscii;
        if (code is >= 32 and <= 126)
        {
            return ascii[code - 32];
        }

        if (HighWidths.TryGetValue(code, out var width))
        {
            return width;
        }

        // An accented letter (0xC0–0xFF, and Š, Ž, š, ž, Ÿ) is as wide as its plain letter.
        var letter = Encoding.Latin1.GetString([code]).Normalize(NormalizationForm.FormD)[0];
        if (code >= 0xC0 && letter is >= 'A' and <= 'z')
        {
            return ascii[letter - 32];
        }

        return code switch
        {
            0x8A or 0x8E or 0x9F => ascii['S' - 32],
            0x9A or 0x9E => ascii['s' - 32],
            _ => 1000,
        };
    }

    private static void Lay(Layout layout, RecipeDocument recipe)
    {
        layout.Paragraph(recipe.Title, Font.Bold, TitleSize, after: 4);
        layout.Paragraph(recipe.Facts, Font.Regular, FactsSize, gray: true, after: 10);
        if (recipe.Description is { } description)
        {
            layout.Paragraph(description, Font.Regular, BodySize, after: 6);
        }

        layout.Heading("Ingredients");
        foreach (var line in recipe.Ingredients)
        {
            layout.Item("•", line);
        }

        if (recipe.Steps.Count > 0)
        {
            layout.Heading("Steps");
            for (var index = 0; index < recipe.Steps.Count; index++)
            {
                layout.Item($"{index + 1}.", recipe.Steps[index], after: 4);
            }
        }

        if (recipe.Notes is { } notes)
        {
            layout.Heading("Notes");
            layout.Paragraph(notes, Font.Regular, BodySize);
        }

        var credits = new[]
        {
            recipe.YieldText is { } yieldText ? $"Makes {yieldText}" : null,
            recipe.Author is { } author ? $"By {author}" : null,
            recipe.SourceUrl is { } source ? $"Source: {source}" : null,
            recipe.Tags.Count > 0 ? $"Tags: {string.Join(", ", recipe.Tags)}" : null,
        }.OfType<string>().ToList();
        if (credits.Count > 0)
        {
            layout.Space(10);
            foreach (var credit in credits)
            {
                layout.Paragraph(credit, Font.Regular, FactsSize, gray: true);
            }
        }
    }

    /// <summary>The PDF file: its objects, the table of where each starts, and the trailer that points at it.</summary>
    private static byte[] Document(string title, IReadOnlyList<StringBuilder> pages)
    {
        // 1 catalog, 2 page tree, 3 and 4 fonts, 5 information, then a page and its content for each page.
        var kids = string.Join(' ', Enumerable.Range(0, pages.Count).Select(index => $"{6 + (2 * index)} 0 R"));
        byte[] info = [.. Ascii("<< /Title "), .. PdfString(title), .. Ascii(" /Producer (ChefBot) >>")];
        var objects = new List<byte[]>
        {
            Ascii("<< /Type /Catalog /Pages 2 0 R >>"),
            Ascii($"<< /Type /Pages /Kids [{kids}] /Count {pages.Count} >>"),
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"),
            info,
        };
        for (var index = 0; index < pages.Count; index++)
        {
            var content = pages[index];
            var footer = $"Page {index + 1} of {pages.Count}";
            var footerX = (PageWidth - Measure(footer, Font.Regular, 9)) / 2;
            content.Append(CultureInfo.InvariantCulture, $"BT 0.4 g /F1 9 Tf {Number(footerX)} {Number(Margin - 18)} Td ")
                .Append(Latin1(PdfString(footer))).Append(" Tj ET\n");
            var stream = Encoding.Latin1.GetBytes(content.ToString());
            objects.Add(Ascii(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Number(PageWidth)} {Number(PageHeight)}] " +
                $"/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {7 + (2 * index)} 0 R >>"));
            objects.Add([.. Ascii($"<< /Length {stream.Length} >>\nstream\n"), .. stream, .. Ascii("\nendstream")]);
        }

        using var file = new MemoryStream();
        file.Write(Ascii("%PDF-1.4\n%âãÏÓ\n"));
        var offsets = new List<long>();
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(file.Position);
            file.Write(Ascii($"{index + 1} 0 obj\n"));
            file.Write(objects[index]);
            file.Write(Ascii("\nendobj\n"));
        }

        var table = file.Position;
        var xref = new StringBuilder($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            xref.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        xref.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R /Info 5 0 R >>\nstartxref\n{table}\n%%EOF\n");
        file.Write(Ascii(xref.ToString()));
        return file.ToArray();
    }

    /// <summary>Text as a PDF string literal: "(Fish \(and chips\))", with bytes past ASCII as octal escapes.</summary>
    private static byte[] PdfString(string text)
    {
        var literal = new StringBuilder("(");
        foreach (var code in Encode(text))
        {
            literal.Append(code switch
            {
                (byte)'(' or (byte)')' or (byte)'\\' => $"\\{(char)code}",
                < 32 or > 126 => $"\\{Convert.ToString(code, 8).PadLeft(3, '0')}",
                _ => ((char)code).ToString(),
            });
        }

        return Ascii(literal.Append(')').ToString());
    }

    private static byte[] Ascii(string text) => Encoding.Latin1.GetBytes(text);

    private static string Latin1(byte[] bytes) => Encoding.Latin1.GetString(bytes);

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Places lines down the pages, starting a new page when the next line would not fit.</summary>
    private sealed class Layout
    {
        private double y;

        public List<StringBuilder> Pages { get; } = [];

        private StringBuilder Page => Pages[^1];

        public void NewPage()
        {
            Pages.Add(new StringBuilder());
            y = PageHeight - Margin;
        }

        public void Space(double points) => y -= points;

        /// <summary>A section heading, moved to the next page when there is no room for it and a line after it.</summary>
        public void Heading(string text)
        {
            y -= 10;
            Room(Leading(HeadingSize) + Leading(BodySize));
            Line(text, Font.Bold, HeadingSize, Margin, gray: false);
            y -= 2;
        }

        public void Paragraph(string text, Font font, double size, bool gray = false, double after = 0)
        {
            foreach (var line in Wrap(text, font, size, TextWidth))
            {
                Line(line, font, size, Margin, gray);
            }

            y -= after;
        }

        /// <summary>A list item: its marker, then its text, wrapped lines lining up under the text.</summary>
        public void Item(string marker, string text, double after = 0)
        {
            var lines = Wrap(text, Font.Regular, BodySize, TextWidth - ListIndent);
            for (var index = 0; index < lines.Count; index++)
            {
                Room(Leading(BodySize));
                if (index == 0)
                {
                    Draw(marker, Font.Regular, BodySize, Margin, y - BodySize, gray: false);
                }

                Line(lines[index], Font.Regular, BodySize, Margin + ListIndent, gray: false);
            }

            y -= after;
        }

        private static double Leading(double size) => size * 1.3;

        private void Line(string text, Font font, double size, double x, bool gray)
        {
            Room(Leading(size));
            Draw(text, font, size, x, y - size, gray);
            y -= Leading(size);
        }

        private void Room(double height)
        {
            if (Pages.Count == 0 || y - height < Bottom)
            {
                NewPage();
            }
        }

        private void Draw(string text, Font font, double size, double x, double baseline, bool gray)
        {
            var fontName = font == Font.Bold ? "F2" : "F1";
            Page.Append(CultureInfo.InvariantCulture, $"BT {(gray ? "0.35" : "0")} g /{fontName} {Number(size)} Tf {Number(x)} {Number(baseline)} Td ")
                .Append(Latin1(PdfString(text))).Append(" Tj ET\n");
        }
    }
}
