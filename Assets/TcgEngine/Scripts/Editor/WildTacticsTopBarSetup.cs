using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace TcgEngine.Editor
{
    /// <summary>
    /// Reestructura el TopBar del menú principal con la nueva arquitectura WT:
    ///
    ///   TopBar
    ///     ├── BG                  (ya existe — recibirá WT_TopBar_Background)
    ///     ├── LeftProfileFrame    (NUEVO — recibirá WT_TopBar_LeftProfileFrame)
    ///     ├── RightLogoFrame      (NUEVO — recibirá WT_TopBar_RightLogoFrame)
    ///     ├── Separator_1/2/3     (NUEVOS — recibirán WT_TopBar_Separator)
    ///     ├── TabPlay             (ya existe — Button transparente)
    ///     │     └── ActiveGlow    (NUEVO — recibirá WT_TopTab_ActiveGlow)
    ///     ├── TabCollection
    ///     │     └── ActiveGlow
    ///     ├── TabPacks
    ///     │     └── ActiveGlow
    ///     └── TabLeaderboard
    ///           └── ActiveGlow
    ///
    /// Menú: WildTactics → Setup TopBar Structure
    /// IMPORTANTE: Abre Menu.unity antes de ejecutar.
    /// </summary>
    public static class WildTacticsTopBarSetup
    {
        private static readonly string[] TAB_NAMES =
            { "TabPlay", "TabCollection", "TabPacks", "TabLeaderboard" };

        [MenuItem("WildTactics/Setup TopBar Structure")]
        public static void SetupTopBar()
        {
            GameObject topBarGO = GameObject.Find("TopBar");
            if (topBarGO == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "No se encontró 'TopBar' en la escena.\nAsegúrate de tener Menu.unity abierto.", "OK");
                return;
            }

            Transform topBar = topBarGO.transform;
            int layer = topBarGO.layer;

            // ── 1. ActiveGlow en cada tab ─────────────────────────────────
            foreach (string tabName in TAB_NAMES)
            {
                Transform tab = topBar.Find(tabName);
                if (tab == null)
                {
                    Debug.LogWarning($"[TopBar Setup] Tab no encontrado: {tabName}");
                    continue;
                }

                if (tab.Find("ActiveGlow") != null)
                {
                    Debug.Log($"[TopBar Setup] ActiveGlow ya existe en {tabName} — omitido.");
                    continue;
                }

                GameObject glow = new GameObject("ActiveGlow");
                glow.layer = layer;
                glow.transform.SetParent(tab, false);

                RectTransform rt = glow.AddComponent<RectTransform>();
                // Anclado en la parte inferior del tab, ancho completo
                rt.anchorMin        = new Vector2(0f, 0f);
                rt.anchorMax        = new Vector2(1f, 0f);
                rt.pivot            = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 0f);
                rt.sizeDelta        = new Vector2(0f, 8f);   // línea de 8px de alto

                Image img = glow.AddComponent<Image>();
                img.color          = new Color(1f, 0.78f, 0.1f, 1f);  // dorado placeholder
                img.raycastTarget  = false;

                glow.SetActive(false);   // oculto por defecto; se activa por script al seleccionar tab

                Debug.Log($"[TopBar Setup] ✓ ActiveGlow → {tabName}");
            }

            // ── 2. Separadores entre tabs ─────────────────────────────────
            // Posiciones X calculadas entre los 4 tabs (espaciado aproximado).
            // Tab 1 centrado en ~667, ancho ~320 → borde derecho ≈ 827
            // Ajustar en el Inspector una vez asignados los sprites reales.
            float[] separatorX = { 827f, 1147f, 1467f };

            for (int i = 0; i < 3; i++)
            {
                string sepName = $"Separator_{i + 1}";
                if (topBar.Find(sepName) != null)
                {
                    Debug.Log($"[TopBar Setup] {sepName} ya existe — omitido.");
                    continue;
                }

                GameObject sep = new GameObject(sepName);
                sep.layer = layer;
                sep.transform.SetParent(topBar, false);

                RectTransform rt = sep.AddComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0f, 0f);
                rt.anchorMax        = new Vector2(0f, 1f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(separatorX[i], 0f);
                rt.sizeDelta        = new Vector2(4f, 0f);   // línea fina vertical

                Image img = sep.AddComponent<Image>();
                img.color         = new Color(0.7f, 0.55f, 0.15f, 0.6f);  // dorado semitransparente
                img.raycastTarget = false;

                Debug.Log($"[TopBar Setup] ✓ {sepName} en X={separatorX[i]}");
            }

            // ── 3. LeftProfileFrame ───────────────────────────────────────
            if (topBar.Find("LeftProfileFrame") == null)
            {
                GameObject frame = new GameObject("LeftProfileFrame");
                frame.layer = layer;
                frame.transform.SetParent(topBar, false);
                frame.transform.SetSiblingIndex(0);   // debajo de todo

                RectTransform rt = frame.AddComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0f, 0f);
                rt.anchorMax        = new Vector2(0f, 1f);
                rt.pivot            = new Vector2(0f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta        = new Vector2(320f, 0f);

                Image img = frame.AddComponent<Image>();
                img.color         = new Color(1f, 1f, 1f, 0f);   // transparente hasta asignar sprite
                img.raycastTarget = false;

                Debug.Log("[TopBar Setup] ✓ LeftProfileFrame");
            }
            else
                Debug.Log("[TopBar Setup] LeftProfileFrame ya existe — omitido.");

            // ── 4. RightLogoFrame ─────────────────────────────────────────
            if (topBar.Find("RightLogoFrame") == null)
            {
                GameObject frame = new GameObject("RightLogoFrame");
                frame.layer = layer;
                frame.transform.SetParent(topBar, false);
                frame.transform.SetSiblingIndex(1);   // debajo de todo excepto LeftProfileFrame

                RectTransform rt = frame.AddComponent<RectTransform>();
                rt.anchorMin        = new Vector2(1f, 0f);
                rt.anchorMax        = new Vector2(1f, 1f);
                rt.pivot            = new Vector2(1f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta        = new Vector2(200f, 0f);

                Image img = frame.AddComponent<Image>();
                img.color         = new Color(1f, 1f, 1f, 0f);   // transparente hasta asignar sprite
                img.raycastTarget = false;

                Debug.Log("[TopBar Setup] ✓ RightLogoFrame");
            }
            else
                Debug.Log("[TopBar Setup] RightLogoFrame ya existe — omitido.");

            // ── Marcar escena como modificada ─────────────────────────────
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("TopBar Setup completado",
                "Estructura del TopBar lista.\n\nPróximos pasos:\n" +
                "1. Guarda la escena (Ctrl+S)\n" +
                "2. Ajusta posición de Separators en el Inspector\n" +
                "3. Cuando lleguen los PNGs, asígnalos a cada Image",
                "OK");
        }
    }
}
