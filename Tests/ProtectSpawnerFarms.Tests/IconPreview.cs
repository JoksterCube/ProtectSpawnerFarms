using System;
using System.IO;
using JoksterCube.ProtectSpawnerFarms.Domain;
using UnityEngine;
using Xunit;

namespace JoksterCube.ProtectSpawnerFarms.Tests;

// Renders the inventory buttons' and the statuses' icons with the mod's own drawing code. Runs only when PROTECTSPAWNERFARMS_ICON_OUT is
// set; otherwise it does nothing.
// .rgba: width and height (int32), then width x height RGBA bytes, row 0 at the top.
public class IconPreview
{
    [Fact]
    public void Render_icons()
    {
        string? output = Environment.GetEnvironmentVariable("PROTECTSPAWNERFARMS_ICON_OUT");
        if (string.IsNullOrEmpty(output)) return;
        Directory.CreateDirectory(output);
        Write(Path.Combine(output, "Shield.rgba"), ButtonIcons.RenderShield());
        Write(Path.Combine(output, "Eye.rgba"), ButtonIcons.RenderEye());
        Write(Path.Combine(output, "Protecting.rgba"), StatusIcons.RenderProtecting());
        Write(Path.Combine(output, "ServerProtected.rgba"), StatusIcons.RenderServerProtected());
        Write(Path.Combine(output, "Highlighting.rgba"), StatusIcons.RenderHighlighting());
    }

    private static void Write(string path, Color[] colors, int width = 64, int height = 64)
    {
        byte[] bytes = new byte[colors.Length * 4];
        for (int i = 0; i < colors.Length; i++)
        {
            bytes[i * 4] = Byte(colors[i].r);
            bytes[i * 4 + 1] = Byte(colors[i].g);
            bytes[i * 4 + 2] = Byte(colors[i].b);
            bytes[i * 4 + 3] = Byte(colors[i].a);
        }
        using BinaryWriter writer = new(File.Create(path));
        writer.Write(width);
        writer.Write(height);
        writer.Write(bytes);
    }

    private static byte Byte(float value) => (byte)Math.Round(Math.Clamp(value, 0f, 1f) * 255f);
}
