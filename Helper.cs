// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System.Linq;
using System.Xml.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

static class Helper
{
    public static int Random(this double[] weights, double r)
    {
        double sum = 0;
        for (int i = 0; i < weights.Length; i++) sum += weights[i];
        double threshold = r * sum;

        double partialSum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            partialSum += weights[i];
            if (partialSum >= threshold) return i;
        }
        return 0;
    }

    public static long ToPower(this int a, int n)
    {
        long product = 1;
        for (int i = 0; i < n; i++) product *= a;
        return product;
    }

    public static T Get<T>(this XElement xelem, string attribute, T defaultT = default)
    {
        XAttribute a = xelem.Attribute(attribute);
        return a == null ? defaultT : (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(a.Value);
    }

    public static IEnumerable<XElement> Elements(this XElement xelement, params string[] names) => xelement.Elements().Where(e => names.Any(n => n == e.Name));
}

static class BitmapHelper
{
    public static (int[] bitmap, int width, int height) LoadBitmap(string filename)
    {
        Bitmap bitmap = new(filename);
        int width = bitmap.Width, height = bitmap.Height;
        var bits = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        int[] result = new int[bitmap.Width * bitmap.Height];
        System.Runtime.InteropServices.Marshal.Copy(bits.Scan0, result, 0, result.Length);
        bitmap.UnlockBits(bits);
        bitmap.Dispose();
        return (result, width, height);
    }

    public static void SaveBitmap(int[] data, int width, int height, string filename)
    {
        Bitmap result = new(width, height);
        var bits = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(data, 0, bits.Scan0, data.Length);
        result.UnlockBits(bits);
        result.Save(filename);
    }
}
