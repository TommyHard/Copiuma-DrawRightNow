namespace DrawRightNow.Core.Models;

public readonly record struct ColorHsv(double H, double S, double V, double A = 1.0)
{
    public ColorRgba ToRgba()
    {
        double r = 0, g = 0, b = 0;

        if (S == 0)
        {
            r = V; g = V; b = V;
        }
        else
        {
            int i = (int)Math.Floor(H / 60) % 6;
            double f = (H / 60) - Math.Floor(H / 60);
            double p = V * (1 - S);
            double q = V * (1 - f * S);
            double t = V * (1 - (1 - f) * S);

            switch (i)
            {
                case 0: r = V; g = t; b = p; break;
                case 1: r = q; g = V; b = p; break;
                case 2: r = p; g = V; b = t; break;
                case 3: r = p; g = q; b = V; break;
                case 4: r = t; g = p; b = V; break;
                case 5: r = V; g = p; b = q; break;
            }
        }

        return new ColorRgba(
            (byte)Math.Round(r * 255),
            (byte)Math.Round(g * 255),
            (byte)Math.Round(b * 255),
            (byte)Math.Round(A * 255));
    }

    public static ColorHsv FromRgba(ColorRgba c)
    {
        double r = c.R / 255.0;
        double g = c.G / 255.0;
        double b = c.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));

        double h = 0, s = 0, v = max;
        double d = max - min;
        s = max == 0 ? 0 : d / max;

        if (max == min) h = 0;
        else
        {
            if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / d + 2;
            else if (max == b) h = (r - g) / d + 4;
            h /= 6;
        }

        return new ColorHsv(h * 360, s, v, c.A / 255.0);
    }
}