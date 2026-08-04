using System.IO;
using UnityEditor;
using UnityEngine;

public class BlacksmithVisualSpriteGenerator
{
    [MenuItem("Tools/Generate Blacksmith Visual Assets")]
    public static void GenerateAssets()
    {
        GenerateInteriorBG();
        GenerateAnvil();
        GenerateGoblinBlacksmithSheet();

        AssetDatabase.Refresh();
        Debug.Log("[BlacksmithVisualSpriteGenerator] All Blacksmith Visual Assets generated successfully!");
    }

    private static void GenerateInteriorBG()
    {
        int width = 128;
        int height = 128;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color wallDark = HexToColor("1A1512");
        Color wallBrick = HexToColor("2D231E");
        Color brickLine = HexToColor("14100D");
        Color forgeStone = HexToColor("3D342E");
        Color forgeGlow = HexToColor("E65C00");
        Color forgeYellow = HexToColor("FFB833");
        Color floorDark = HexToColor("221C18");

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Floor
                if (y < 24)
                {
                    Color col = floorDark;
                    if (y == 23) col = HexToColor("382F29");
                    tex.SetPixel(x, y, col);
                }
                // Forge on left (x: 0 to 40, y: 24 to 80)
                else if (x < 44 && y < 84)
                {
                    if (x > 8 && x < 36 && y > 32 && y < 68)
                    {
                        // Forge Fire hole
                        float distFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(22, 50));
                        if (distFromCenter < 10) tex.SetPixel(x, y, forgeYellow);
                        else if (distFromCenter < 16) tex.SetPixel(x, y, forgeGlow);
                        else tex.SetPixel(x, y, forgeStone);
                    }
                    else
                    {
                        tex.SetPixel(x, y, forgeStone);
                    }
                }
                // Brick Wall
                else
                {
                    bool isBrickRow = (y / 8) % 2 == 0;
                    bool isLine = (y % 8 == 0) || ((x + (isBrickRow ? 0 : 8)) % 16 == 0);
                    tex.SetPixel(x, y, isLine ? brickLine : (x % 3 == 0 ? wallBrick : wallDark));
                }
            }
        }

        tex.Apply();
        SaveAndImportTexture(tex, "Assets/Resources/Sprite/Blacksmith_Interior_BG.png", SpriteImportMode.Single);
    }

    private static void GenerateAnvil()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        ClearTransparent(tex);

        Color ironDark = HexToColor("2B303A");
        Color ironBase = HexToColor("4A5260");
        Color ironLight = HexToColor("768296");
        Color woodBase = HexToColor("593C26");

        // Wood Stand (x: 10..22, y: 4..12)
        for (int y = 4; y <= 12; y++)
        {
            for (int x = 10; x <= 22; x++)
            {
                Color col = (x == 10 || x == 22 || y == 4) ? HexToColor("3B2719") : woodBase;
                tex.SetPixel(x, y, col);
            }
        }

        // Anvil Body (x: 8..24, y: 13..24)
        for (int y = 13; y <= 24; y++)
        {
            for (int x = 6; x <= 26; x++)
            {
                if (y >= 20 && x >= 6 && x <= 26) // Top face
                {
                    tex.SetPixel(x, y, y == 24 ? ironLight : ironBase);
                }
                else if (y >= 16 && x >= 11 && x <= 21) // Middle stem
                {
                    tex.SetPixel(x, y, x == 11 ? ironDark : ironBase);
                }
                else if (y < 16 && x >= 9 && x <= 23) // Base
                {
                    tex.SetPixel(x, y, ironDark);
                }
            }
        }

        tex.Apply();
        SaveAndImportTexture(tex, "Assets/Resources/Sprite/Blacksmith_Anvil.png", SpriteImportMode.Single);
    }

    private static void GenerateGoblinBlacksmithSheet()
    {
        int frameW = 64;
        int frameH = 64;
        int cols = 4;
        int rows = 2; // Row 0: Idle (4 frames), Row 1: Hammering (4 frames)

        Texture2D tex = new Texture2D(frameW * cols, frameH * rows, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        ClearTransparent(tex);

        Color darkGreenSkin = HexToColor("235023");
        Color baseGreenSkin = HexToColor("3C8C32");
        Color lightGreenSkin = HexToColor("64C34B");
        Color eyeYellow = HexToColor("F5D728");
        Color eyePupil = HexToColor("141414");
        Color apronColor = HexToColor("734628");
        Color hammerIron = HexToColor("6C7A89");
        Color hammerHandle = HexToColor("4A3525");
        Color sparkYellow = HexToColor("FFEE55");
        Color darkOutline = HexToColor("0F1E0F");

        for (int r = 0; r < rows; r++)
        {
            int unityRow = 1 - r; // Row 0 -> top (unityRow 1), Row 1 -> bot (unityRow 0)

            for (int f = 0; f < cols; f++)
            {
                int offsetX = f * frameW;
                int offsetY = unityRow * frameH;

                int dy = 0;
                int hammerAngle = 0; // 0: resting, 1: up, 2: strike down, 3: recoil

                if (r == 0) // Idle
                {
                    dy = (f == 1 || f == 2) ? 1 : 0;
                    hammerAngle = 0;
                }
                else // Hammering
                {
                    if (f == 0) { dy = 0; hammerAngle = 0; } // Ready
                    else if (f == 1) { dy = 2; hammerAngle = 1; } // Windup High
                    else if (f == 2) { dy = -1; hammerAngle = 2; } // SMASH STRIKE!
                    else if (f == 3) { dy = 0; hammerAngle = 3; } // Recoil
                }

                int baseX = offsetX + 24;
                int baseY = offsetY + 12 + dy;

                // Shadow
                for (int sx = -8; sx <= 16; sx++)
                {
                    for (int sy = -2; sy <= 0; sy++)
                    {
                        tex.SetPixel(baseX + sx, offsetY + 10 + sy, new Color(0, 0, 0, 0.35f));
                    }
                }

                // Legs / Feet
                DrawRect(tex, baseX - 2, baseY, 4, 8, darkOutline);
                DrawRect(tex, baseX + 6, baseY, 4, 8, darkOutline);
                DrawRect(tex, baseX - 1, baseY + 1, 2, 6, baseGreenSkin);
                DrawRect(tex, baseX + 7, baseY + 1, 2, 6, baseGreenSkin);

                // Body / Apron
                DrawRect(tex, baseX - 4, baseY + 8, 16, 16, apronColor);
                DrawRect(tex, baseX - 2, baseY + 10, 12, 14, HexToColor("8C5732")); // Leather detail

                // Head
                int headX = baseX - 2;
                int headY = baseY + 24;
                DrawRect(tex, headX - 4, headY, 16, 14, darkOutline);
                DrawRect(tex, headX - 3, headY + 1, 14, 12, baseGreenSkin);

                // Ears
                DrawRect(tex, headX - 8, headY + 6, 5, 4, baseGreenSkin);
                DrawRect(tex, headX + 11, headY + 6, 5, 4, baseGreenSkin);

                // Eyes & Nose
                DrawRect(tex, headX + 2, headY + 6, 3, 3, eyeYellow);
                DrawRect(tex, headX + 3, headY + 7, 1, 1, eyePupil);
                DrawRect(tex, headX + 7, headY + 6, 3, 3, eyeYellow);
                DrawRect(tex, headX + 8, headY + 7, 1, 1, eyePupil);

                // Hammer & Arms rendering according to hammerAngle
                if (hammerAngle == 0) // Rest
                {
                    DrawRect(tex, baseX + 10, baseY + 12, 10, 3, hammerHandle);
                    DrawRect(tex, baseX + 18, baseY + 10, 6, 7, hammerIron);
                }
                else if (hammerAngle == 1) // Overhead Windup
                {
                    DrawRect(tex, headX + 2, headY + 12, 3, 14, hammerHandle);
                    DrawRect(tex, headX - 1, headY + 24, 9, 8, hammerIron);
                }
                else if (hammerAngle == 2) // STRIKE!
                {
                    DrawRect(tex, baseX + 10, baseY + 6, 14, 4, hammerHandle);
                    DrawRect(tex, baseX + 22, baseY + 2, 9, 10, hammerIron);

                    // Sparks!
                    for (int sp = 0; sp < 6; sp++)
                    {
                        tex.SetPixel(baseX + 28 + sp * 2, baseY + 2 + (sp % 3) * 3, sparkYellow);
                    }
                }
                else if (hammerAngle == 3) // Recoil
                {
                    DrawRect(tex, baseX + 10, baseY + 10, 12, 3, hammerHandle);
                    DrawRect(tex, baseX + 20, baseY + 8, 7, 7, hammerIron);
                }
            }
        }

        tex.Apply();
        SaveAndImportTexture(tex, "Assets/Resources/Sprite/Goblin_Blacksmith_Sheet.png", SpriteImportMode.Multiple);
    }

    private static void SaveAndImportTexture(Texture2D tex, string path, SpriteImportMode mode)
    {
        byte[] bytes = tex.EncodeToPNG();
        string fullPath = Path.Combine(Application.dataPath, path.Replace("Assets/", ""));
        string dir = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        File.WriteAllBytes(fullPath, bytes);
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = mode;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            if (mode == SpriteImportMode.Multiple && path.Contains("Goblin_Blacksmith_Sheet"))
            {
                SpriteMetaData[] metaData = new SpriteMetaData[8];
                for (int i = 0; i < 4; i++)
                {
                    metaData[i] = new SpriteMetaData
                    {
                        name = "Goblin_Idle_" + i,
                        rect = new Rect(i * 64, 64, 64, 64),
                        alignment = (int)SpriteAlignment.Center
                    };
                    metaData[4 + i] = new SpriteMetaData
                    {
                        name = "Goblin_Hammer_" + i,
                        rect = new Rect(i * 64, 0, 64, 64),
                        alignment = (int)SpriteAlignment.Center
                    };
                }
                importer.spritesheet = metaData;
            }

            importer.SaveAndReimport();
        }
    }

    private static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color color)
    {
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                int px = x + i;
                int py = y + j;
                if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                {
                    tex.SetPixel(px, py, color);
                }
            }
        }
    }

    private static void ClearTransparent(Texture2D tex)
    {
        Color trans = new Color(0, 0, 0, 0);
        for (int y = 0; y < tex.height; y++)
        {
            for (int x = 0; x < tex.width; x++)
            {
                tex.SetPixel(x, y, trans);
            }
        }
    }

    private static Color HexToColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color col);
        return col;
    }
}
