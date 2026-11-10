using System.Drawing;

namespace QuickTheme.Services;

/// <summary>
/// Reproduces the seven accent shades Windows derives from an accent color.
/// The order matches the first seven entries of the Explorer\Accent\AccentPalette
/// registry value: Light3, Light2, Light1, Accent, Dark1, Dark2, Dark3.
/// </summary>
internal static class AccentPaletteBuilder
{
    private const int PaletteSize = 7;

    // Windows uses exact precomputed ramps for its built-in accent colors.
    // Values are 0xRRGGBB; each row is Light3..Dark3.
    private static readonly Dictionary<int, int[]> BuiltInPalettes = new()
    {
        [0xFFB900] = [0xFFE845, 0xFFD52A, 0xFFC20D, 0xFFB900, 0xE19D00, 0x9B5D00, 0x5C2100],
        [0xE74856] = [0xFBB4B7, 0xF38A91, 0xEB5C68, 0xE74856, 0xE32938, 0xA6161E, 0x650D0F],
        [0x0078D4] = [0x99EBFF, 0x4CC2FF, 0x0091F8, 0x0078D4, 0x0067C0, 0x003E92, 0x001A68],
        [0x0078D7] = [0xA6D8FF, 0x76B9ED, 0x429CE3, 0x0078D7, 0x005A9E, 0x004275, 0x002642],
        [0x0099BC] = [0x7AF7FF, 0x30E5FF, 0x00B8DF, 0x0099BC, 0x0085A6, 0x005573, 0x002A45],
        [0x7A7574] = [0xE7E5E4, 0xBDB9B8, 0x8E8988, 0x7A7574, 0x696363, 0x403B3A, 0x1B1515],
        [0x767676] = [0xE6E6E6, 0xBBBBBA, 0x8B8B8B, 0x767676, 0x646464, 0x3B3B3B, 0x151515],
        [0xFF8C00] = [0xFFD155, 0xFFB634, 0xFF9910, 0xFF8C00, 0xE37700, 0xA14600, 0x651900],
        [0xE81123] = [0xFB9D8B, 0xF46762, 0xEF2733, 0xE81123, 0xD20E1E, 0x9E0912, 0x6F0306],
        [0x0063B1] = [0x9CEBFF, 0x40BDFF, 0x007FDC, 0x0063B1, 0x0055A1, 0x00337C, 0x00145A],
        [0x2D7D9A] = [0xA7E8ED, 0x6FC4D5, 0x3598B7, 0x2D7D9A, 0x266C88, 0x17455F, 0x08213A],
        [0x5D5A58] = [0xE2E1DE, 0xAEACAA, 0x767370, 0x5D5A58, 0x504D4B, 0x302D2C, 0x141010],
        [0x4C4A48] = [0xDFDEDC, 0xA6A5A1, 0x686562, 0x4C4A48, 0x413F3D, 0x272524, 0x100D0D],
        [0xF7630C] = [0xFEBD68, 0xFB9A44, 0xF8741D, 0xF7630C, 0xE05307, 0xA13105, 0x691202],
        [0xEA005E] = [0xFF95CA, 0xFF529F, 0xFF0A6F, 0xEA005E, 0xD30050, 0x9E002F, 0x6D0011],
        [0x8E8CD8] = [0xE8E3F8, 0xC5C1EC, 0x9F9CDE, 0x8E8CD8, 0x6D6BCD, 0x36349B, 0x1A1956],
        [0x00B7C3] = [0x69FCFF, 0x29F7FF, 0x00D5E1, 0x00B7C3, 0x009FAA, 0x006770, 0x00343B],
        [0x68768A] = [0xD9E6EA, 0xADBBC5, 0x7C8A9C, 0x68768A, 0x586579, 0x343C51, 0x13172D],
        [0x69797E] = [0xD8E7E8, 0xADBCBF, 0x7C8E92, 0x69797E, 0x59676D, 0x353F45, 0x131920],
        [0xCA5010] = [0xF5C07C, 0xF09346, 0xE46012, 0xCA5010, 0xB6440E, 0x872808, 0x5C0E03],
        [0xC30052] = [0xFF97D1, 0xFF459D, 0xEA0065, 0xC30052, 0xB10046, 0x860029, 0x5F000F],
        [0x6B69D6] = [0xE4D8F8, 0xB5ADEB, 0x817DDC, 0x6B69D6, 0x4F4DCE, 0x2C2B9E, 0x13136A],
        [0x038387] = [0x7CFBFC, 0x21F6FA, 0x04ADB2, 0x038387, 0x037276, 0x024B4E, 0x01282A],
        [0x515C6B] = [0xD7E2E4, 0xA0AEB7, 0x657486, 0x515C6B, 0x454E5E, 0x292F40, 0x0F1224],
        [0x4A5459] = [0xD6E0E1, 0x9EABAE, 0x616F75, 0x4A5459, 0x3F484D, 0x252B32, 0x0D1118],
        [0xDA3B01] = [0xFEB16C, 0xFE7E34, 0xF44801, 0xDA3B01, 0xC53201, 0x931E01, 0x660B00],
        [0xE3008C] = [0xFF94EE, 0xFF4FCB, 0xFF04A4, 0xE3008C, 0xCD007B, 0x990053, 0x69002E],
        [0x8764B8] = [0xE9D4F2, 0xC3A8DC, 0x9979C3, 0x8764B8, 0x744FAC, 0x4B3279, 0x241251],
        [0x00B294] = [0x67FFEE, 0x21FFE0, 0x00D3B1, 0x00B294, 0x009B7E, 0x00664A, 0x00361B],
        [0x567C73] = [0xC5E7E4, 0x9ABEB8, 0x66948A, 0x567C73, 0x496B62, 0x2B443A, 0x0F2015],
        [0x647C64] = [0xD3E7D2, 0xA8BEA7, 0x779177, 0x647C64, 0x556B55, 0x324332, 0x121E12],
        [0xEF6950] = [0xFCC2A3, 0xF7A083, 0xF1795F, 0xEF6950, 0xEC492D, 0xB12210, 0x670E09],
        [0xBF0077] = [0xFF97EE, 0xFF43C8, 0xE60094, 0xBF0077, 0xAD0069, 0x830048, 0x5C0029],
        [0x744DA9] = [0xE6CEF0, 0xBA9CD4, 0x8963B8, 0x744DA9, 0x654199, 0x402775, 0x1F0E54],
        [0x018574] = [0x77FEF7, 0x1AFDE9, 0x01B09C, 0x018574, 0x017463, 0x014E3A, 0x002A15],
        [0x486860] = [0xC7E4E1, 0x94B5AF, 0x5B837A, 0x486860, 0x3D5A52, 0x243930, 0x0D1B11],
        [0x525E54] = [0xD7E2D9, 0xA3AFA5, 0x69786B, 0x525E54, 0x465147, 0x29312A, 0x0F150F],
        [0xD13438] = [0xF7B1A5, 0xE8807A, 0xD84B4C, 0xD13438, 0xBE2B2E, 0x8D1A1C, 0x61090A],
        [0xC239B3] = [0xF4B2F1, 0xE183D9, 0xCB4FBF, 0xC239B3, 0xAE30A0, 0x7F1D75, 0x540A4D],
        [0xB146C2] = [0xF1BBF4, 0xD88DE1, 0xBD5BCB, 0xB146C2, 0x9E3AB0, 0x6F2382, 0x460D5A],
        [0x00CC6A] = [0x5FFFA5, 0x26FF8E, 0x00E775, 0x00CC6A, 0x00B25A, 0x007635, 0x003F13],
        [0x498205] = [0xC1F96C, 0x99F618, 0x61A907, 0x498205, 0x3E7204, 0x254B03, 0x0D2801],
        [0x847545] = [0xE9E5B0, 0xC2B986, 0x9D8D52, 0x847545, 0x74633B, 0x4E3B23, 0x2B150C],
        [0xFF4343] = [0xFFAB9B, 0xFF8279, 0xFF5653, 0xFF4343, 0xFF1F1F, 0xC90000, 0x7C0000],
        [0x9A0089] = [0xFF97FA, 0xFF35EE, 0xC800B3, 0x9A0089, 0x8C007C, 0x6B005E, 0x4C0042],
        [0x881798] = [0xEFACF2, 0xD95BE6, 0xAB1DBE, 0x881798, 0x7B148B, 0x5B0C6D, 0x3F0451],
        [0x10893E] = [0x89F1A5, 0x3AE872, 0x14AE4C, 0x10893E, 0x0E7835, 0x08521F, 0x032E0B],
        [0x107C10] = [0x95EF81, 0x45E532, 0x19A115, 0x107C10, 0x0E6D0E, 0x084B08, 0x032B03],
        [0x7E735F] = [0xE8E5CE, 0xBFB8A3, 0x948971, 0x7E735F, 0x6D6251, 0x473A30, 0x241511],
    };

    public static Color[] Build(Color accent)
    {
        int rgb = (accent.R << 16) | (accent.G << 8) | accent.B;

        if (BuiltInPalettes.TryGetValue(rgb, out int[]? builtIn))
        {
            return builtIn.Select(FromRgb24).ToArray();
        }

        Hsl hsl = RgbToHsl(accent);

        // Very light, very dark, and near-gray accents use the older HSL ramp.
        if (hsl.L < 0.25 || hsl.L > 0.75 || hsl.S < 0.15)
        {
            return BuildLegacyHslPalette(hsl);
        }

        return BuildLabPalette(accent);
    }

    private static Color[] BuildLegacyHslPalette(Hsl input)
    {
        double lightness = Math.Clamp(input.L, 0.25, 0.75);
        double scale = 4.0 * Math.Abs(0.5 - lightness);
        float[] deltas = [0.17f, 0.10f, 0.05f, 0.0f, -0.05f, -0.10f, -0.17f];
        double[] offsets = new double[PaletteSize];

        for (int index = 0; index < PaletteSize; index++)
        {
            offsets[index] = scale * deltas[index];
        }

        double shift = 0.0;
        if (lightness + offsets[0] > 1.0)
        {
            shift = lightness + offsets[0] - 1.0;
        }
        else if (lightness + offsets[6] <= 0.0)
        {
            shift = lightness + offsets[6];
        }

        Color[] palette = new Color[PaletteSize];
        for (int index = 0; index < PaletteSize; index++)
        {
            double value = Math.Clamp(lightness + offsets[index] - shift, 0.0, 1.0);
            palette[index] = HslToColor(input.H, input.S, value, round: false);
        }

        return palette;
    }

    private static Color[] BuildLabPalette(Color input)
    {
        Lab lab = RgbToLab(input);
        double lightness = Math.Clamp(lab.L, 49.0, 50.0);

        Color baseColor = LabToColor(lightness, lab.A, lab.B);
        Color dark = LabToColor(0.0, lab.A, lab.B);
        Color light = LabToColor(100.0, lab.A, lab.B);

        return
        [
            SaturateMatch(baseColor, Mix(light, 0.82, baseColor, 0.18)),
            SaturateMatch(baseColor, Mix(light, 0.58, baseColor, 0.42)),
            SaturateMatch(baseColor, Mix(baseColor, 0.84, light, 0.16)),
            baseColor,
            SaturateMatch(baseColor, Mix(dark, 0.22, baseColor, 0.78)),
            SaturateMatch(baseColor, Mix(baseColor, 0.50, dark, 0.50)),
            SaturateMatch(baseColor, Mix(baseColor, 0.18, dark, 0.82)),
        ];
    }

    private static Color SaturateMatch(Color baseColor, Color candidate)
    {
        Hsl baseHsl = RgbToHsl(baseColor);
        Hsl candidateHsl = RgbToHsl(candidate);

        if (candidateHsl.S < baseHsl.S)
        {
            candidateHsl = candidateHsl with { S = baseHsl.S };
        }

        return HslToColor(candidateHsl.H, candidateHsl.S, candidateHsl.L, round: true);
    }

    private static Color Mix(Color first, double firstWeight, Color second, double secondWeight) =>
        Color.FromArgb(
            255,
            RoundByte(first.R * firstWeight + second.R * secondWeight),
            RoundByte(first.G * firstWeight + second.G * secondWeight),
            RoundByte(first.B * firstWeight + second.B * secondWeight));

    private static Hsl RgbToHsl(Color color)
    {
        double red = color.R / 255.0;
        double green = color.G / 255.0;
        double blue = color.B / 255.0;

        double maximum = Math.Max(red, Math.Max(green, blue));
        double minimum = Math.Min(red, Math.Min(green, blue));
        double lightness = (maximum + minimum) / 2.0;

        double hue = 0.0;
        double saturation = 0.0;

        if (maximum != minimum)
        {
            double delta = maximum - minimum;
            saturation = lightness > 0.5
                ? delta / (2.0 - maximum - minimum)
                : delta / (maximum + minimum);

            if (maximum == red)
            {
                hue = (green - blue) / delta + (green < blue ? 6.0 : 0.0);
            }
            else if (maximum == green)
            {
                hue = (blue - red) / delta + 2.0;
            }
            else
            {
                hue = (red - green) / delta + 4.0;
            }

            hue *= 60.0;
        }

        return new Hsl(hue, saturation, lightness);
    }

    private static Color HslToColor(double hue, double saturation, double lightness, bool round)
    {
        hue %= 360.0;
        if (hue < 0.0)
        {
            hue += 360.0;
        }

        saturation = Math.Clamp(saturation, 0.0, 1.0);
        lightness = Math.Clamp(lightness, 0.0, 1.0);

        double red;
        double green;
        double blue;

        if (saturation == 0.0)
        {
            red = green = blue = lightness;
        }
        else
        {
            double normalizedHue = hue / 360.0;
            double q = lightness < 0.5
                ? lightness * (1.0 + saturation)
                : lightness + saturation - lightness * saturation;
            double p = 2.0 * lightness - q;

            red = HueToRgb(p, q, normalizedHue + 1.0 / 3.0);
            green = HueToRgb(p, q, normalizedHue);
            blue = HueToRgb(p, q, normalizedHue - 1.0 / 3.0);
        }

        return Color.FromArgb(
            255,
            UnitToByte(red, round),
            UnitToByte(green, round),
            UnitToByte(blue, round));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0.0)
        {
            t += 1.0;
        }

        if (t > 1.0)
        {
            t -= 1.0;
        }

        if (t < 1.0 / 6.0)
        {
            return p + (q - p) * 6.0 * t;
        }

        if (t < 1.0 / 2.0)
        {
            return q;
        }

        if (t < 2.0 / 3.0)
        {
            return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
        }

        return p;
    }

    private static Lab RgbToLab(Color color)
    {
        double red = SrgbToLinear(color.R / 255.0);
        double green = SrgbToLinear(color.G / 255.0);
        double blue = SrgbToLinear(color.B / 255.0);

        double x = (0.4124 * red + 0.3576 * green + 0.1805 * blue) * 100.0;
        double y = (0.2126 * red + 0.7152 * green + 0.0722 * blue) * 100.0;
        double z = (0.0193 * red + 0.1192 * green + 0.9505 * blue) * 100.0;

        double fx = LabF(x / 95.047);
        double fy = LabF(y / 100.000);
        double fz = LabF(z / 108.883);

        return new Lab(
            116.0 * fy - 16.0,
            500.0 * (fx - fy),
            200.0 * (fy - fz));
    }

    private static Color LabToColor(double lightness, double a, double b)
    {
        double fy = (lightness + 16.0) / 116.0;
        double fx = fy + a / 500.0;
        double fz = fy - b / 200.0;

        double x = 95.047 * LabFInverse(fx) / 100.0;
        double y = 100.000 * LabFInverse(fy) / 100.0;
        double z = 108.883 * LabFInverse(fz) / 100.0;

        double red = 3.2406 * x - 1.5372 * y - 0.4986 * z;
        double green = -0.9689 * x + 1.8758 * y + 0.0415 * z;
        double blue = 0.0557 * x - 0.2040 * y + 1.0570 * z;

        return Color.FromArgb(
            255,
            UnitToByte(LinearToSrgb(red), round: true),
            UnitToByte(LinearToSrgb(green), round: true),
            UnitToByte(LinearToSrgb(blue), round: true));
    }

    private static double LabF(double value) =>
        value > 0.008856
            ? Math.Pow(value, 1.0 / 3.0)
            : 7.787 * value + 16.0 / 116.0;

    private static double LabFInverse(double value)
    {
        const double threshold = 6.0 / 29.0;
        return value > threshold
            ? value * value * value
            : (value - 16.0 / 116.0) / 7.787;
    }

    private static double SrgbToLinear(double value) =>
        value <= 0.04045
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);

    private static double LinearToSrgb(double value) =>
        value <= 0.0031308
            ? 12.92 * value
            : 1.055 * Math.Pow(value, 1.0 / 2.4) - 0.055;

    private static Color FromRgb24(int rgb) =>
        Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);

    private static byte RoundByte(double value) =>
        (byte)Math.Floor(Math.Clamp(value, 0.0, 255.0) + 0.5);

    private static byte UnitToByte(double value, bool round)
    {
        double scaled = Math.Clamp(value, 0.0, 1.0) * 255.0;
        return (byte)(round ? Math.Floor(scaled + 0.5) : Math.Floor(scaled));
    }

    private readonly record struct Hsl(double H, double S, double L);

    private readonly record struct Lab(double L, double A, double B);
}
