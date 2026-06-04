using UnityEngine;
using UnityEditor;
using System.IO;

namespace TcgEngine.Editor
{
    /// <summary>
    /// Herramienta de editor para crear automáticamente los AvatarData
    /// de los 15 avatares elementales de WildTactics.
    ///
    /// Uso: menú superior → WildTactics → Create Wild Avatars
    ///
    /// Requisito: los sprites deben estar en
    ///   Assets/TcgEngine/Sprites/Avatar/
    /// con los nombres exactos: fire1.png, fire2.png, ..., water3.png, etc.
    ///
    /// Los 5 avatares por defecto (fire1, water1, plant2, dark1, light2)
    /// se crean con unlock_type = Default.
    /// Los 10 restantes se crean con unlock_type = Shop (configúralos en el Inspector).
    /// </summary>
    public static class CreateWildAvatars
    {
        private const string SPRITE_FOLDER  = "Assets/TcgEngine/Sprites/Avatar";
        private const string ASSET_FOLDER   = "Assets/TcgEngine/Resources/Avatars";

        // Avatares disponibles por defecto desde el inicio
        private static readonly string[] DEFAULT_AVATARS = { "fire1", "water1", "plant2", "dark1", "light2" };

        // Todos los avatares que queremos crear (los 15 elementales)
        private static readonly string[] ALL_AVATARS =
        {
            "fire1",  "fire2",  "fire3",
            "water1", "water2", "water3",
            "plant1", "plant2", "plant3",
            "dark1",  "dark2",  "dark3",
            "light1", "light2", "light3",
        };

        [MenuItem("WildTactics/Create Wild Avatars")]
        public static void CreateAvatars()
        {
            // Asegurarse de que existe la carpeta destino
            if (!Directory.Exists(ASSET_FOLDER))
                Directory.CreateDirectory(ASSET_FOLDER);

            int created = 0, skipped = 0;

            for (int i = 0; i < ALL_AVATARS.Length; i++)
            {
                string id       = ALL_AVATARS[i];
                string assetPath= $"{ASSET_FOLDER}/{id}.asset";

                // No sobreescribir si ya existe
                if (File.Exists(assetPath))
                {
                    Debug.Log($"[CreateWildAvatars] Skipped (already exists): {id}");
                    skipped++;
                    continue;
                }

                // Cargar el sprite
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_FOLDER}/{id}.png");
                if (sprite == null)
                {
                    Debug.LogWarning($"[CreateWildAvatars] Sprite no encontrado: {SPRITE_FOLDER}/{id}.png — asset creado sin sprite.");
                }

                // Crear el ScriptableObject
                AvatarData data     = ScriptableObject.CreateInstance<AvatarData>();
                data.id             = id;
                data.avatar         = sprite;
                data.sort_order     = IsDefault(id) ? i : (100 + i);  // defaults primero
                data.unlock_type    = IsDefault(id) ? AvatarUnlockType.Default : AvatarUnlockType.Shop;
                data.unlock_amount  = 0;

                AssetDatabase.CreateAsset(data, assetPath);
                created++;
                Debug.Log($"[CreateWildAvatars] Creado: {id} ({data.unlock_type})");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Wild Avatars creados",
                $"Creados: {created}\nOmitidos (ya existían): {skipped}\n\n" +
                $"Recuerda:\n" +
                $"• Borrar los .asset de los animales viejos en {ASSET_FOLDER}\n" +
                $"• Ajustar unlock_type y unlock_amount de los 10 bloqueados en el Inspector\n" +
                $"• Ampliar el array 'Avatars' en PlayerPanel de 10 a 15 slots",
                "OK");
        }

        private static bool IsDefault(string id)
        {
            foreach (string d in DEFAULT_AVATARS)
                if (d == id) return true;
            return false;
        }
    }
}
