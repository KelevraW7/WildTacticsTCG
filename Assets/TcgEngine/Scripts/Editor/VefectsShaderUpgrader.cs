using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Batch-upgrades all Vefects BIRP materials to URP-compatible equivalents.
/// Menu: WildTactics → Upgrade Vefects Shaders to URP
/// To revert: WildTactics → Revert Vefects Shaders (restores shader names from backup log)
/// </summary>
public class VefectsShaderUpgrader : EditorWindow
{
    private const string BackupPath = "Assets/TcgEngine/Scripts/Editor/VefectsShaderBackup.txt";

    // -------------------------------------------------------------------------
    //  UPGRADE
    // -------------------------------------------------------------------------
    [MenuItem("WildTactics/Upgrade Vefects Shaders to URP")]
    static void UpgradeShaders()
    {
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpUnlit == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró 'Universal Render Pipeline/Particles/Unlit'.\n" +
                "Asegúrate de que URP está instalado.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Vefects" });
        var backupLines = new List<string>();
        int upgraded = 0, skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) { skipped++; continue; }

            string shaderName = mat.shader.name;

            // Skip already-URP or non-Vefects materials
            if (shaderName.StartsWith("Universal Render Pipeline") ||
                shaderName.StartsWith("Sprites/") ||
                shaderName == "Hidden/InternalErrorShader")
            {
                skipped++;
                continue;
            }

            if (!shaderName.Contains("BIRP") &&
                !shaderName.Contains("Vefects") &&
                !shaderName.Contains("SH_VFX"))
            {
                skipped++;
                continue;
            }

            // --- Back up original shader name ---------------------------------
            backupLines.Add($"{path}|{shaderName}");

            // --- Read existing properties before shader switch ----------------
            Texture mainTex   = TryGetTex(mat, "_MainTex", "_BaseMap");
            Color   color     = TryGetColor(mat, "_Color", "_TintColor", "_BaseColor");
            float   alpha     = Mathf.Clamp01(color.a > 0f ? color.a : 1f);

            // Additive blend if shader name says so
            bool isAdditive = shaderName.Contains("_Add")    ||
                              shaderName.Contains("Add_")    ||
                              shaderName.Contains("_Glow")   ||
                              shaderName.Contains("_Windup") ||
                              shaderName.Contains("_Energy") ||
                              shaderName.Contains("_Embers") ||
                              shaderName.Contains("_Flare")  ||
                              shaderName.Contains("_Lightning");

            // --- Switch shader ------------------------------------------------
            mat.shader = urpUnlit;

            if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
            mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, alpha));
            mat.SetFloat("_Surface", 1f);   // Transparent
            mat.SetInt("_ZWrite", 0);

            if (isAdditive)
            {
                mat.SetFloat("_Blend", 2f); // Additive
                mat.SetInt("_SrcBlend",   (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend",   (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.Zero);
            }
            else
            {
                mat.SetFloat("_Blend", 0f); // Alpha
                mat.SetInt("_SrcBlend",   (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend",   (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            // Cull off so particles visible from both sides
            mat.SetInt("_Cull", 0);

            EditorUtility.SetDirty(mat);
            upgraded++;
        }

        // Save backup
        File.WriteAllLines(
            Path.Combine(Application.dataPath.Replace("Assets", ""), BackupPath),
            backupLines);
        AssetDatabase.ImportAsset(BackupPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Vefects → URP",
            $"¡Completado!\n\nActualizados: {upgraded} materiales\nOmitidos:     {skipped} materiales\n\nBackup guardado en:\n{BackupPath}",
            "OK");
    }

    // -------------------------------------------------------------------------
    //  REVERT
    // -------------------------------------------------------------------------
    [MenuItem("WildTactics/Revert Vefects Shaders (desde backup)")]
    static void RevertShaders()
    {
        string fullPath = Path.Combine(
            Application.dataPath.Replace("Assets", ""), BackupPath);

        if (!File.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró el archivo de backup.\n" + BackupPath, "OK");
            return;
        }

        string[] lines = File.ReadAllLines(fullPath);
        int reverted = 0, missing = 0;

        foreach (string line in lines)
        {
            string[] parts = line.Split('|');
            if (parts.Length != 2) continue;

            string matPath    = parts[0];
            string shaderName = parts[1];

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { missing++; continue; }

            Shader sh = Shader.Find(shaderName);
            if (sh == null) { missing++; continue; }

            mat.shader = sh;
            EditorUtility.SetDirty(mat);
            reverted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Revert Vefects",
            $"¡Revertido!\n\nRestaurados: {reverted}\nNo encontrados: {missing}", "OK");
    }

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------
    static Texture TryGetTex(Material mat, params string[] props)
    {
        foreach (string p in props)
            if (mat.HasProperty(p)) { var t = mat.GetTexture(p); if (t != null) return t; }
        return null;
    }

    static Color TryGetColor(Material mat, params string[] props)
    {
        foreach (string p in props)
            if (mat.HasProperty(p)) return mat.GetColor(p);
        return Color.white;
    }
}
