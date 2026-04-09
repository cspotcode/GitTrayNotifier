#!/usr/bin/env dotnet-script
// Converts Git_icon.svg -> app.ico using SkiaSharp (rasterize) + Magick.NET (ICO assembly).
// Requires: dotnet-script  (dotnet tool install -g dotnet-script)
#r "nuget: SkiaSharp, 2.88.9"
#r "nuget: SkiaSharp.NativeAssets.Win32, 2.88.9"
#r "nuget: Svg.Skia, 1.0.0"
#r "nuget: Magick.NET-Q8-x64, 14.4.0"

using System;
using System.IO;
using System.Runtime.CompilerServices;
using ImageMagick;
using SkiaSharp;
using Svg.Skia;

public static string GetScriptFolder([CallerFilePath] string path = null) => Path.GetDirectoryName(path);
var __SOURCE_DIRECTORY__ = GetScriptFolder();

var svgPath = Path.Combine(__SOURCE_DIRECTORY__, "Git_icon.svg");
var icoPath = Path.Combine(__SOURCE_DIRECTORY__, "app.ico");

Console.WriteLine($"SVG : {svgPath}");
Console.WriteLine($"ICO : {icoPath}");

int[] sizes = new int[] { 16, 24, 32, 48, 64, 128, 256 };

using (var svg = new SKSvg())
using (var collection = new MagickImageCollection())
{
    svg.Load(svgPath);
    var picture = svg.Picture!;
    var srcRect = picture.CullRect;

    foreach (int size in sizes)
    {
        float scale = size / Math.Max(srcRect.Width, srcRect.Height);

        var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var surface = SKSurface.Create(info))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            canvas.Scale(scale);
            canvas.DrawPicture(picture);
            canvas.Flush();

            using (var skImage = surface.Snapshot())
            using (var pngData = skImage.Encode(SKEncodedImageFormat.Png, 100))
            {
                var magickImage = new MagickImage(pngData.AsStream());
                magickImage.Format = MagickFormat.Png32;
                collection.Add(magickImage);
                Console.WriteLine($"  Rasterized {size}x{size}");
            }
        }
    }

    using (var fs = File.OpenWrite(icoPath))
    {
        collection.Write(fs, MagickFormat.Icon);
    }
    Console.WriteLine($"Written {icoPath} ({new FileInfo(icoPath).Length} bytes, sizes: {string.Join(", ", sizes)})");
}
