using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace TcgEngine.Editor
{
    /// <summary>
    /// Aplica los sprites del UI Kit al TopBar de Menu.unity.
    ///
    /// Qué hace:
    ///   - TopBar/BG           → WT_TopBar_Background   (Sliced)
    ///   - TopBar/LeftProfileFrame → WT_TopBar_LeftProfileFrame (Sliced)
    ///   - TopBar/RightLogoFrame   → transparente (sin sprite, placeholder)
    ///   - Cada Tab            → Image transparente (el texto queda visible)
    ///   - Cada Tab/ActiveGlow → WT_TopTab_ActiveGlow   (Simple, desactivado)
    ///
    /// Menú: WildTactics → Apply TopBar Sprites
    /// IMPORTANTE: Abre Menu.unity antes de ejecutar.
    /// </summary>
    public static class WildTacticsTopBarApply
    {
        private const string KIT_PATH = "Assets/UI_KIT_WILDTACTICS/Sprites";

        private static readonly string[] TAB_NAMES =
            { "TabPlay", "TabCollection", "TabPacks", "TabLeaderboard" };

        // ── Limpia duplicados y re-aplica desde cero ──────────────────────
        [MenuItem("WildTactics/Reset and Apply TopBar")]
        public static void ResetAndApply()
        {
            GameObject topBarGO = GameObject.Find("TopBar");
            if (topBarGO == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "No se encontró 'TopBar'.\nAsegúrate de tener Menu.unity abierto.", "OK");
                return;
            }

            Transform topBar = topBarGO.transform;

            // ── Eliminar duplicados (objetos creados más de una vez) ───────
            string[] uniqueChildren = {
                "LeftProfileFrame", "RightLogoFrame",
                "Separator_1", "Separator_2", "Separator_3"
            };

            foreach (string childName in uniqueChildren)
                RemoveDuplicates(topBar, childName);

            // Limpiar ActiveGlow duplicados en cada tab
            foreach (string tabName in TAB_NAMES)
            {
                Transform tab = topBar.Find(tabName);
                if (tab != null) RemoveDuplicates(tab, "ActiveGlow");
            }

            Debug.Log("[TopBar Reset] Limpieza de duplicados completada.");

            // ── Re-aplicar sprites ────────────────────────────────────────
            ApplyTopBarSprites();
        }

        private static void RemoveDuplicates(Transform parent, string childName)
        {
            var found = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in parent)
                if (child.name == childName) found.Add(child);

            // Mantener solo el primero, eliminar el resto
            for (int i = 1; i < found.Count; i++)
            {
                Debug.Log($"[TopBar Reset] Eliminando duplicado: {parent.name}/{childName} [{i}]");
                Object.DestroyImmediate(found[i].gameObject);
            }
        }

        [MenuItem("WildTactics/Apply TopBar Sprites")]
        public static void ApplyTopBarSprites()
        {
            GameObject topBarGO = GameObject.Find("TopBar");
            if (topBarGO == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "No se encontró 'TopBar'.\nAsegúrate de tener Menu.unity abierto.", "OK");
                return;
            }

            Transform topBar = topBarGO.transform;
            int applied = 0;

            // ── TopBar BG → WT_TopBar_Background ─────────────────────────
            Sprite bgSprite = LoadSprite("WT_TopBar_Background");
            if (bgSprite != null)
            {
                Transform bg = topBar.Find("BG");
                if (bg != null)
                {
                    Image img = bg.GetComponent<Image>();
                    if (img == null) img = bg.gameObject.AddComponent<Image>();
                    img.sprite         = bgSprite;
                    img.type           = Image.Type.Simple;
                    img.preserveAspect = false;
                    img.color          = Color.white;

                    // BG estirado para cubrir todo el TopBar (ancho y alto completo)
                    RectTransform bgRt = bg.GetComponent<RectTransform>();
                    if (bgRt != null)
                    {
                        bgRt.anchorMin        = new Vector2(0f, 0f);
                        bgRt.anchorMax        = new Vector2(1f, 1f);
                        bgRt.pivot            = new Vector2(0.5f, 0.5f);
                        bgRt.anchoredPosition = Vector2.zero;
                        bgRt.sizeDelta        = Vector2.zero;
                    }
                    Debug.Log("[TopBar Apply] ✓ BG → WT_TopBar_Background");
                    applied++;
                }
                else Debug.LogWarning("[TopBar Apply] No se encontró TopBar/BG");
            }
            else Debug.LogWarning("[TopBar Apply] Sprite no encontrado: WT_TopBar_Background");

            // ── LeftProfileFrame ──────────────────────────────────────────
            Sprite leftFrame = LoadSprite("WT_TopBar_LeftProfileFrame");
            if (leftFrame != null)
            {
                Transform lf = topBar.Find("LeftProfileFrame");
                if (lf != null)
                {
                    Image img = lf.GetComponent<Image>();
                    if (img == null) img = lf.gameObject.AddComponent<Image>();
                    img.sprite         = leftFrame;
                    img.type           = Image.Type.Simple;  // sin 9-slice
                    img.preserveAspect = false;
                    img.color          = Color.white;
                    img.raycastTarget  = false;

                    // Tamaño nativo: 170×188px, anclado a la izquierda del TopBar.
                    // Sobresale arriba/abajo del TopBar (188 > 145) para el efecto de marco.
                    RectTransform lfRt = lf.GetComponent<RectTransform>();
                    if (lfRt != null)
                    {
                        lfRt.anchorMin        = new Vector2(0f, 0.5f);
                        lfRt.anchorMax        = new Vector2(0f, 0.5f);
                        lfRt.pivot            = new Vector2(0f, 0.5f);
                        lfRt.anchoredPosition = Vector2.zero;
                        lfRt.sizeDelta        = new Vector2(170f, 188f); // tamaño nativo del PNG
                    }

                    Debug.Log("[TopBar Apply] ✓ LeftProfileFrame → WT_TopBar_LeftProfileFrame");
                    applied++;
                }
                else Debug.LogWarning("[TopBar Apply] No se encontró TopBar/LeftProfileFrame");
            }
            else Debug.LogWarning("[TopBar Apply] Sprite no encontrado: WT_TopBar_LeftProfileFrame");

            // ── Separadores ───────────────────────────────────────────────
            Sprite sepSprite = LoadSprite("WT_TopBar_Background_Separators");
            if (sepSprite != null)
            {
                string[] sepNames = { "Separator_1", "Separator_2", "Separator_3" };
                foreach (string sepName in sepNames)
                {
                    Transform sep = topBar.Find(sepName);
                    if (sep == null) continue;

                    Image img = sep.GetComponent<Image>();
                    if (img == null) img = sep.gameObject.AddComponent<Image>();
                    img.sprite        = sepSprite;
                    img.type          = Image.Type.Simple;
                    img.preserveAspect = true;   // mantiene las proporciones del separador
                    img.color         = Color.white;
                    img.raycastTarget = false;

                    // Tamaño nativo: 32×102px, centrado verticalmente
                    RectTransform rt = sep.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin        = new Vector2(0f, 0.5f);
                        rt.anchorMax        = new Vector2(0f, 0.5f);
                        rt.pivot            = new Vector2(0.5f, 0.5f);
                        rt.sizeDelta        = new Vector2(32f, 102f);
                    }

                    Debug.Log($"[TopBar Apply] ✓ {sepName} → WT_TopBar_Background_Separators");
                    applied++;
                }
            }
            else Debug.LogWarning("[TopBar Apply] Sprite no encontrado: WT_TopBar_Background_Separators");

            // ── Tabs → transparentes + ActiveGlow ────────────────────────
            Sprite glowSprite = LoadSprite("WT_TopTab_ActiveGlow");

            foreach (string tabName in TAB_NAMES)
            {
                Transform tab = topBar.Find(tabName);
                if (tab == null)
                {
                    Debug.LogWarning($"[TopBar Apply] Tab no encontrado: {tabName}");
                    continue;
                }

                // Tab principal: Image transparente (clickable, sin fondo visual)
                Image tabImg = tab.GetComponent<Image>();
                if (tabImg != null)
                {
                    tabImg.sprite = null;
                    tabImg.color  = new Color(0f, 0f, 0f, 0f);
                }

                // ActiveGlow hijo
                Transform glow = tab.Find("ActiveGlow");
                if (glow != null && glowSprite != null)
                {
                    Image glowImg = glow.GetComponent<Image>();
                    if (glowImg == null) glowImg = glow.gameObject.AddComponent<Image>();
                    glowImg.sprite       = glowSprite;
                    glowImg.type         = Image.Type.Sliced;
                    glowImg.color        = Color.white;
                    glowImg.raycastTarget = false;
                    glow.gameObject.SetActive(false);
                    Debug.Log($"[TopBar Apply] ✓ {tabName}/ActiveGlow → WT_TopTab_ActiveGlow");
                    applied++;
                }
                else if (glow == null)
                    Debug.LogWarning($"[TopBar Apply] {tabName} no tiene hijo ActiveGlow — ejecuta 'Setup TopBar Structure' primero.");
                else
                    Debug.LogWarning("[TopBar Apply] Sprite no encontrado: WT_TopTab_ActiveGlow (pendiente)");
            }

            // ── Guardar escena ────────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("TopBar Apply completado",
                $"Sprites aplicados: {applied}\n\nGuarda la escena (Ctrl+S) y comprueba el resultado.\n" +
                "El ActiveGlow está desactivado — se activará cuando el tab esté seleccionado.",
                "OK");
        }

        // ── Helper: carga un sprite por nombre desde el kit ──────────────
        private static Sprite LoadSprite(string name)
        {
            string[] guids = AssetDatabase.FindAssets($"{name} t:Sprite", new[] { KIT_PATH });
            if (guids.Length == 0) return null;
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
