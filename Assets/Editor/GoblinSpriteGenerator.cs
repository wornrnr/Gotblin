using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public class GoblinSpriteGenerator
{
    [MenuItem("Tools/Generate Hero Goblin Sprite")]
    public static void GenerateGoblinSprite()
    {
        int frameW = 32;
        int frameH = 32;
        int cols = 4;
        int rows = 3;

        Texture2D tex = new Texture2D(frameW * cols, frameH * rows, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        // Clear transparent
        Color transparent = new Color(0, 0, 0, 0);
        for (int y = 0; y < tex.height; y++)
        {
            for (int x = 0; x < tex.width; x++)
            {
                tex.SetPixel(x, y, transparent);
            }
        }

        // Color Palette
        Color darkOutline = HexToColor("0F1E0F");
        Color darkGreenSkin = HexToColor("235023");
        Color baseGreenSkin = HexToColor("3C8C32");
        Color lightGreenSkin = HexToColor("64C34B");
        Color innerEarMouth = HexToColor("87232D");
        Color nosePink = HexToColor("EB3C3C");
        Color eyeYellow = HexToColor("F5D728");
        Color eyePupil = HexToColor("141414");
        Color leatherDark = HexToColor("412819");
        Color leatherBase = HexToColor("734628");
        Color beltBuckle = HexToColor("AA7341");
        Color whiteAccent = HexToColor("FFFFFF");
        Color shadowCol = new Color(0, 0, 0, 0.35f);
        Color fxBlue = new Color(0.78f, 0.90f, 1.0f, 0.85f);

        // Helper to draw pixel on 32x32 cell
        System.Action<int, int, int, int, Color> drawPx = (cellX, cellY, px, py, c) => {
            int targetX = cellX * 32 + px;
            // Unity Y is bottom-up, invert py for standard top-down 32x32 canvas
            int targetY = cellY * 32 + (31 - py);
            if (targetX >= 0 && targetX < tex.width && targetY >= 0 && targetY < tex.height)
            {
                tex.SetPixel(targetX, targetY, c);
            }
        };

        // Draw Row 0: Idle (cellY = 2: top row in Unity texture coordinate)
        // Draw Row 1: Move (cellY = 1: middle row)
        // Draw Row 2: Attack (cellY = 0: bottom row)

        for (int r = 0; r < 3; r++)
        {
            string pose = (r == 0) ? "idle" : (r == 1) ? "move" : "attack";
            int unityRow = 2 - r; // 0->2(top), 1->1(mid), 2->0(bot)

            for (int f = 0; f < 4; f++)
            {
                int dy = 0;
                int dx = 0;

                if (pose == "idle")
                {
                    dy = (f == 1 || f == 2) ? -1 : 0;
                }
                else if (pose == "move")
                {
                    dy = (f == 1 || f == 3) ? -1 : 0;
                }
                else if (pose == "attack")
                {
                    if (f == 0) dx = -1;
                    else if (f == 1) dx = 1;
                    else if (f == 2) dx = 2;
                    else if (f == 3) dx = 1;
                }

                int cX = f;
                int cY = unityRow;

                // 1. Shadow
                for (int x = 11; x <= 20; x++) drawPx(cX, cY, x, 28, shadowCol);
                for (int x = 10; x <= 21; x++) drawPx(cX, cY, x, 29, shadowCol);

                int hy = 7 + dy;
                int hx = 10 + dx;

                // 2. Ears
                // Left Ear
                drawPx(cX, cY, hx - 1, hy + 4, darkOutline);
                drawPx(cX, cY, hx - 2, hy + 3, darkOutline);
                drawPx(cX, cY, hx - 3, hy + 2, darkOutline);
                drawPx(cX, cY, hx - 4, hy + 1, darkOutline);
                drawPx(cX, cY, hx - 5, hy + 2, darkOutline);
                drawPx(cX, cY, hx - 4, hy + 3, darkOutline);
                drawPx(cX, cY, hx - 3, hy + 4, darkOutline);
                drawPx(cX, cY, hx - 2, hy + 5, darkOutline);
                drawPx(cX, cY, hx - 1, hy + 6, darkOutline);

                drawPx(cX, cY, hx - 2, hy + 4, innerEarMouth);
                drawPx(cX, cY, hx - 3, hy + 3, innerEarMouth);
                drawPx(cX, cY, hx - 4, hy + 2, innerEarMouth);
                drawPx(cX, cY, hx - 1, hy + 5, baseGreenSkin);

                // Right Ear
                drawPx(cX, cY, hx + 11, hy + 4, darkOutline);
                drawPx(cX, cY, hx + 12, hy + 3, darkOutline);
                drawPx(cX, cY, hx + 13, hy + 2, darkOutline);
                drawPx(cX, cY, hx + 14, hy + 1, darkOutline);
                drawPx(cX, cY, hx + 15, hy + 2, darkOutline);
                drawPx(cX, cY, hx + 14, hy + 3, darkOutline);
                drawPx(cX, cY, hx + 13, hy + 4, darkOutline);
                drawPx(cX, cY, hx + 12, hy + 5, darkOutline);
                drawPx(cX, cY, hx + 11, hy + 6, darkOutline);

                drawPx(cX, cY, hx + 12, hy + 4, innerEarMouth);
                drawPx(cX, cY, hx + 13, hy + 3, innerEarMouth);
                drawPx(cX, cY, hx + 14, hy + 2, innerEarMouth);
                drawPx(cX, cY, hx + 11, hy + 5, baseGreenSkin);

                // 3. Head Outline & Fill
                for (int x = hx + 2; x <= hx + 8; x++) drawPx(cX, cY, x, hy, darkOutline);
                drawPx(cX, cY, hx + 1, hy + 1, darkOutline); drawPx(cX, cY, hx + 9, hy + 1, darkOutline);
                drawPx(cX, cY, hx, hy + 2, darkOutline); drawPx(cX, cY, hx + 10, hy + 2, darkOutline);
                for (int y = hy + 3; y <= hy + 8; y++)
                {
                    drawPx(cX, cY, hx - 1, y, darkOutline);
                    drawPx(cX, cY, hx + 11, y, darkOutline);
                }
                drawPx(cX, cY, hx, hy + 9, darkOutline); drawPx(cX, cY, hx + 10, hy + 9, darkOutline);
                for (int x = hx + 1; x <= hx + 9; x++) drawPx(cX, cY, x, hy + 10, darkOutline);

                // Head Fill
                for (int y = hy + 1; y <= hy + 9; y++)
                {
                    for (int x = hx; x <= hx + 10; x++)
                    {
                        if (y <= hy + 3 && x >= hx + 2 && x <= hx + 7)
                            drawPx(cX, cY, x, y, lightGreenSkin);
                        else if (x <= hx + 2 || y >= hy + 8)
                            drawPx(cX, cY, x, y, darkGreenSkin);
                        else
                            drawPx(cX, cY, x, y, baseGreenSkin);
                    }
                }

                // 4. Face Features
                bool blink = (pose == "idle" && f == 2);
                if (blink)
                {
                    drawPx(cX, cY, hx + 2, hy + 5, darkOutline);
                    drawPx(cX, cY, hx + 3, hy + 5, darkOutline);
                    drawPx(cX, cY, hx + 7, hy + 5, darkOutline);
                    drawPx(cX, cY, hx + 8, hy + 5, darkOutline);
                }
                else
                {
                    // Eyes
                    drawPx(cX, cY, hx + 2, hy + 4, eyeYellow); drawPx(cX, cY, hx + 3, hy + 4, eyeYellow);
                    drawPx(cX, cY, hx + 2, hy + 5, eyeYellow); drawPx(cX, cY, hx + 3, hy + 5, eyePupil);
                    drawPx(cX, cY, hx + 1, hy + 4, darkOutline);

                    drawPx(cX, cY, hx + 7, hy + 4, eyeYellow); drawPx(cX, cY, hx + 8, hy + 4, eyeYellow);
                    drawPx(cX, cY, hx + 7, hy + 5, eyePupil); drawPx(cX, cY, hx + 8, hy + 5, eyeYellow);
                    drawPx(cX, cY, hx + 9, hy + 4, darkOutline);
                }

                // Nose
                drawPx(cX, cY, hx + 5, hy + 5, darkGreenSkin);
                drawPx(cX, cY, hx + 5, hy + 6, darkGreenSkin);
                drawPx(cX, cY, hx + 5, hy + 7, darkOutline);

                // Mouth / Fang
                if (pose == "attack" && (f == 1 || f == 2))
                {
                    drawPx(cX, cY, hx + 3, hy + 8, darkOutline);
                    drawPx(cX, cY, hx + 4, hy + 8, innerEarMouth);
                    drawPx(cX, cY, hx + 5, hy + 8, innerEarMouth);
                    drawPx(cX, cY, hx + 6, hy + 8, darkOutline);
                    drawPx(cX, cY, hx + 4, hy + 7, whiteAccent);
                }
                else
                {
                    drawPx(cX, cY, hx + 3, hy + 8, darkOutline);
                    drawPx(cX, cY, hx + 4, hy + 8, darkOutline);
                    drawPx(cX, cY, hx + 5, hy + 8, darkOutline);
                    drawPx(cX, cY, hx + 6, hy + 8, darkOutline);
                    drawPx(cX, cY, hx + 3, hy + 7, whiteAccent);
                }

                // 5. Body & Leather Pants
                int by = hy + 11;
                int bx = hx + 1;

                for (int y = by; y <= by + 3; y++)
                {
                    for (int x = bx + 1; x <= bx + 7; x++) drawPx(cX, cY, x, y, baseGreenSkin);
                    drawPx(cX, cY, bx, y, darkOutline);
                    drawPx(cX, cY, bx + 8, y, darkOutline);
                }
                drawPx(cX, cY, bx + 3, by + 1, lightGreenSkin);
                drawPx(cX, cY, bx + 4, by + 1, lightGreenSkin);

                int py = by + 4;
                for (int y = py; y <= py + 2; y++)
                {
                    for (int x = bx; x <= bx + 8; x++) drawPx(cX, cY, x, y, leatherBase);
                    drawPx(cX, cY, bx - 1, y, darkOutline);
                    drawPx(cX, cY, bx + 9, y, darkOutline);
                }
                for (int x = bx; x <= bx + 8; x++) drawPx(cX, cY, x, py, leatherDark);
                drawPx(cX, cY, bx + 4, py, beltBuckle);

                // 6. Arms
                if (pose == "attack")
                {
                    if (f == 0)
                    {
                        drawPx(cX, cY, bx - 2, by + 1, baseGreenSkin);
                        drawPx(cX, cY, bx - 3, by + 2, baseGreenSkin);
                        drawPx(cX, cY, bx - 3, by + 3, darkOutline);
                        drawPx(cX, cY, bx + 9, by + 1, baseGreenSkin);
                        drawPx(cX, cY, bx + 10, by + 2, baseGreenSkin);
                    }
                    else if (f == 1)
                    {
                        drawPx(cX, cY, bx - 2, by + 1, baseGreenSkin);
                        drawPx(cX, cY, bx + 9, by + 1, baseGreenSkin);
                        drawPx(cX, cY, bx + 10, by + 1, baseGreenSkin);
                        drawPx(cX, cY, bx + 11, by + 1, lightGreenSkin);
                        drawPx(cX, cY, bx + 12, by + 1, darkOutline);
                    }
                    else if (f == 2)
                    {
                        drawPx(cX, cY, bx - 2, by + 1, baseGreenSkin);
                        drawPx(cX, cY, bx + 9, by + 1, baseGreenSkin);
                        drawPx(cX, cY, bx + 10, by + 1, baseGreenSkin);
                        drawPx(cX, cY, bx + 11, by + 1, baseGreenSkin);
                        drawPx(cX, cY, bx + 12, by + 1, lightGreenSkin);
                        drawPx(cX, cY, bx + 13, by, darkOutline);
                        drawPx(cX, cY, bx + 13, by + 1, lightGreenSkin);
                        drawPx(cX, cY, bx + 13, by + 2, darkOutline);

                        // Swing FX
                        drawPx(cX, cY, bx + 14, by - 1, fxBlue);
                        drawPx(cX, cY, bx + 15, by, fxBlue);
                        drawPx(cX, cY, bx + 15, by + 1, fxBlue);
                        drawPx(cX, cY, bx + 15, by + 2, fxBlue);
                        drawPx(cX, cY, bx + 14, by + 3, fxBlue);
                    }
                    else if (f == 3)
                    {
                        drawPx(cX, cY, bx - 2, by + 1, baseGreenSkin);
                        drawPx(cX, cY, bx + 9, by + 1, baseGreenSkin);
                        drawPx(cX, cY, bx + 10, by + 2, baseGreenSkin);
                    }
                }
                else
                {
                    int armL = by + 1;
                    int armR = by + 1;
                    if (pose == "move")
                    {
                        if (f == 0 || f == 2) { armL -= 1; armR += 1; }
                        else { armL += 1; armR -= 1; }
                    }
                    drawPx(cX, cY, bx - 1, armL, baseGreenSkin);
                    drawPx(cX, cY, bx - 2, armL + 1, baseGreenSkin);
                    drawPx(cX, cY, bx - 2, armL + 2, darkOutline);

                    drawPx(cX, cY, bx + 9, armR, baseGreenSkin);
                    drawPx(cX, cY, bx + 10, armR + 1, baseGreenSkin);
                    drawPx(cX, cY, bx + 10, armR + 2, darkOutline);
                }

                // 7. Legs
                int ly = py + 3;
                if (pose == "move")
                {
                    if (f == 0)
                    {
                        drawPx(cX, cY, bx, ly, baseGreenSkin); drawPx(cX, cY, bx - 1, ly + 1, baseGreenSkin); drawPx(cX, cY, bx - 2, ly + 1, darkOutline);
                        drawPx(cX, cY, bx + 7, ly, baseGreenSkin); drawPx(cX, cY, bx + 8, ly + 1, baseGreenSkin); drawPx(cX, cY, bx + 9, ly + 1, darkOutline);
                    }
                    else if (f == 1 || f == 3)
                    {
                        drawPx(cX, cY, bx + 1, ly, baseGreenSkin); drawPx(cX, cY, bx + 1, ly + 1, darkOutline);
                        drawPx(cX, cY, bx + 7, ly, baseGreenSkin); drawPx(cX, cY, bx + 7, ly + 1, darkOutline);
                    }
                    else if (f == 2)
                    {
                        drawPx(cX, cY, bx + 1, ly, baseGreenSkin); drawPx(cX, cY, bx + 2, ly + 1, baseGreenSkin); drawPx(cX, cY, bx + 3, ly + 1, darkOutline);
                        drawPx(cX, cY, bx + 6, ly, baseGreenSkin); drawPx(cX, cY, bx + 5, ly + 1, baseGreenSkin); drawPx(cX, cY, bx + 4, ly + 1, darkOutline);
                    }
                }
                else
                {
                    drawPx(cX, cY, bx + 1, ly, baseGreenSkin); drawPx(cX, cY, bx + 1, ly + 1, baseGreenSkin); drawPx(cX, cY, bx + 1, ly + 2, darkOutline);
                    drawPx(cX, cY, bx + 7, ly, baseGreenSkin); drawPx(cX, cY, bx + 7, ly + 1, baseGreenSkin); drawPx(cX, cY, bx + 7, ly + 2, darkOutline);
                    drawPx(cX, cY, bx, ly + 2, darkOutline); drawPx(cX, cY, bx + 8, ly + 2, darkOutline);
                }
            }
        }

        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string dir = Path.Combine(Application.dataPath, "Resources/Sprite");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string pngPath = Path.Combine(dir, "HeroGoblin_Base_Sheet.png");
        File.WriteAllBytes(pngPath, bytes);

        AssetDatabase.Refresh();

        // Configure Texture Import Settings
        string relativePath = "Assets/Resources/Sprite/HeroGoblin_Base.png";
        if (File.Exists(Path.Combine(Application.dataPath, "Resources/Sprite/HeroGoblin_Base_Sheet.png")))
        {
            relativePath = "Assets/Resources/Sprite/HeroGoblin_Base_Sheet.png";
        }
        
        TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        UnityEngine.Debug.Log("Successfully generated HeroGoblin_Base_Sheet.png at " + relativePath);
    }

    private static Color HexToColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color col);
        return col;
    }
}
