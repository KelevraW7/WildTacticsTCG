using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace TcgEngine.Editor
{
    /// <summary>
    /// Configura todos los sprites del UI_KIT_WILDTACTICS de una sola pasada:
    ///   - Tipo Sprite (UI)
    ///   - Sin mipmaps
    ///   - Alpha transparente
    ///   - Bordes 9-slice según el JSON del kit
    ///
    /// Menú: WildTactics → Setup UI Kit Sprites
    /// </summary>
    public static class WildTacticsUIKitSetup
    {
        private const string KIT_PATH = "Assets/UI_KIT_WILDTACTICS/Sprites";

        // Bordes 9-slice: Vector4(left, bottom, right, top)  ← orden Unity
        // Datos extraídos de Unity_9Slice_Borders.json
        private static readonly Dictionary<string, Vector4> Borders =
            new Dictionary<string, Vector4>
        {
            { "WT_Button_Primary_Normal_9S",   new Vector4(48, 48, 48, 48) },
            { "WT_Button_Primary_Hover_9S",    new Vector4(48, 48, 48, 48) },
            { "WT_Button_Primary_Disabled_9S", new Vector4(48, 48, 48, 48) },
            { "WT_Button_Secondary_Normal_9S", new Vector4(48, 42, 48, 42) },
            { "WT_Button_Secondary_Hover_9S",  new Vector4(48, 42, 48, 42) },
            { "WT_TopTab_Normal_9S",           new Vector4(42, 42, 42, 42) },
            { "WT_TopTab_Active_9S",           new Vector4(42, 42, 42, 42) },
            { "WT_ModeCard_Normal_9S",         new Vector4(70, 70, 70, 70) },
            { "WT_ModeCard_Hover_9S",          new Vector4(70, 70, 70, 70) },
            { "WT_ModeCard_Selected_9S",       new Vector4(70, 70, 70, 70) },
            { "WT_ModeCard_Locked_9S",         new Vector4(70, 70, 70, 70) },
            { "WT_TopBar_Backplate_9S",        new Vector4(60, 60, 60, 60) },
            { "WT_ProfilePanel_9S",            new Vector4(58, 58, 58, 58) },
            { "WT_DifficultyModal_9S",         new Vector4(70, 70, 70, 70) },
            { "WT_DifficultySlot_Normal_9S",   new Vector4(54, 54, 54, 54) },
            { "WT_DifficultySlot_Hover_9S",    new Vector4(54, 54, 54, 54) },
            { "WT_AvatarFrame_9S",             new Vector4(58, 58, 58, 58) },
            { "WT_LevelBadge_9S",              new Vector4(44, 44, 44, 44) },
            { "WT_LogoFrame_9S",               new Vector4(58, 58, 58, 58) },

            // ── Assets v2 (creados en Photoshop) ────────────────────────
            // WT_TopBar_Background: sin 9-slice (se usa como Simple estirado)
            // { "WT_TopBar_Background", ... }  ← no añadir bordes intencionalmente
            { "WT_TopBar_LeftProfileFrame",    new Vector4(50, 50, 50, 50) },  // 170×188
            { "WT_TopTab_ActiveGlow",          new Vector4(12,  0, 12,  0) },  // línea horizontal: solo bordes laterales
            { "WT_TopTab_HoverGlow",           new Vector4(12,  0, 12,  0) },  // ídem
        };

        [MenuItem("WildTactics/Setup UI Kit Sprites")]
        public static void SetupAllSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { KIT_PATH });

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("UI Kit Setup",
                    "No se encontraron texturas en:\n" + KIT_PATH, "OK");
                return;
            }

            int configured = 0;
            int sliced     = 0;

            foreach (string guid in guids)
            {
                string path      = AssetDatabase.GUIDToAssetPath(guid);
                string nameNoExt = Path.GetFileNameWithoutExtension(path);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                // ── Configuración base para sprites de UI ──────────────────
                importer.textureType          = TextureImporterType.Sprite;
                importer.spriteImportMode     = SpriteImportMode.Single;
                importer.spritePixelsPerUnit  = 100;
                importer.mipmapEnabled        = false;
                importer.alphaIsTransparency  = true;
                importer.filterMode           = FilterMode.Bilinear;
                importer.textureCompression   = TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize       = 2048;

                // ── Bordes 9-slice (solo sprites que los tienen definidos) ──
                if (Borders.TryGetValue(nameNoExt, out Vector4 border))
                {
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteBorder = border;
                    importer.SetTextureSettings(settings);
                    sliced++;
                }

                importer.SaveAndReimport();
                configured++;
            }

            AssetDatabase.Refresh();

            string msg = $"Sprites configurados: {configured}\nCon 9-slice: {sliced}";
            Debug.Log("[WildTactics UI Kit] " + msg);
            EditorUtility.DisplayDialog("UI Kit Setup completado", msg, "OK");
        }
    }
}
