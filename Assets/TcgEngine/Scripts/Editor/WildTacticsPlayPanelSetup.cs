using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TcgEngine.UI;

namespace TcgEngine.Editor
{
    /// <summary>
    /// Crea los 3 paneles de modo de juego (Desafío, Solo, Online) en Menu.unity.
    ///
    /// Jerarquía generada bajo PlayPanel:
    ///   PlayPanelZone                  ← HorizontalLayoutGroup
    ///     ├── Panel_Desafio            ← PlayPanelCard + CanvasGroup
    ///     │     ├── Illustration       ← Image (arte de fondo)
    ///     │     ├── Frame              ← Image (marco, cambia con el estado)
    ///     │     └── Title              ← TMP_Text
    ///     ├── Panel_Solo
    ///     │     └── (ídem)
    ///     └── Panel_Online
    ///           └── (ídem)
    ///
    /// Menú: WildTactics → Setup Play Panels
    /// </summary>
    public static class WildTacticsPlayPanelSetup
    {
        private const string KIT_FRAMES  = "Assets/UI_KIT_WILDTACTICS/Sprites/Frames";
        private const string KIT_PANELS  = "Assets/UI_KIT_WILDTACTICS/Sprites/Panels";

        // Datos de cada panel
        private static readonly (string id, string title, string illustration)[] PANELS =
        {
            ("Panel_Desafio", "DESAFÍO", "WT_PlayPanel_Illustration_Desafio"),
            ("Panel_Solo",    "SOLO",    "WT_PlayPanel_Illustration_SOLO"),
            ("Panel_Online",  "ONLINE",  "WT_PlayPanel_Illustration_Online"),
        };

        [MenuItem("WildTactics/Setup Play Panels")]
        public static void SetupPlayPanels()
        {
            // ── 1. Buscar PlayPanel en escena ─────────────────────────────────
            GameObject playPanelGO = GameObject.Find("PlayPanel");
            if (playPanelGO == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "No se encontró 'PlayPanel' en la escena.\nAsegúrate de tener Menu.unity abierto.", "OK");
                return;
            }

            int layer = playPanelGO.layer;

            // ── 2. Crear / encontrar PlayPanelZone ────────────────────────────
            Transform zone = playPanelGO.transform.Find("PlayPanelZone");
            if (zone == null)
            {
                GameObject zoneGO = new GameObject("PlayPanelZone");
                zoneGO.layer = layer;
                zoneGO.transform.SetParent(playPanelGO.transform, false);

                // Añadir RectTransform ANTES de guardar la referencia de transform,
                // porque AddComponent<RectTransform> destruye el Transform original.
                RectTransform zoneRt = zoneGO.AddComponent<RectTransform>();
                zone = zoneGO.transform;   // ahora apunta al RectTransform válido
                zoneRt.anchorMin        = new Vector2(0f, 0f);
                zoneRt.anchorMax        = new Vector2(1f, 1f);
                zoneRt.pivot            = new Vector2(0.5f, 0.5f);
                zoneRt.anchoredPosition = Vector2.zero;
                zoneRt.sizeDelta        = Vector2.zero;

                // HorizontalLayoutGroup centrado
                HorizontalLayoutGroup hlg = zoneGO.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment        = TextAnchor.MiddleCenter;
                hlg.spacing               = 40f;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight= false;
                hlg.childControlWidth     = false;
                hlg.childControlHeight    = false;

                Debug.Log("[PlayPanel Setup] ✓ PlayPanelZone creado");
            }
            else
            {
                Debug.Log("[PlayPanel Setup] PlayPanelZone ya existe — se reutiliza");
            }

            // ── 3. Cargar sprites compartidos ─────────────────────────────────
            Sprite sprNormal = LoadSprite("WT_PlayPanel_Normal",  KIT_FRAMES);
            Sprite sprHover  = LoadSprite("WT_PlayPanel_Hover",   KIT_FRAMES);
            Sprite sprActive = LoadSprite("WT_PlayPanel_Active",  KIT_FRAMES);

            if (sprNormal == null)
                Debug.LogWarning("[PlayPanel Setup] WT_PlayPanel_Normal no encontrado — asígnalo manualmente");
            if (sprHover == null)
                Debug.LogWarning("[PlayPanel Setup] WT_PlayPanel_Hover no encontrado — asígnalo manualmente");
            if (sprActive == null)
                Debug.LogWarning("[PlayPanel Setup] WT_PlayPanel_Active no encontrado — asígnalo manualmente");

            // ── 4. Crear cada panel ───────────────────────────────────────────
            foreach (var (id, title, illustrationName) in PANELS)
            {
                if (zone.Find(id) != null)
                {
                    Debug.Log($"[PlayPanel Setup] {id} ya existe — omitido");
                    continue;
                }

                // Raíz del panel
                GameObject panelGO = new GameObject(id);
                panelGO.layer = layer;
                panelGO.transform.SetParent(zone, false);

                RectTransform panelRt = panelGO.AddComponent<RectTransform>();
                panelRt.sizeDelta = new Vector2(289f, 556f);

                // PlayPanelCard
                PlayPanelCard card = panelGO.AddComponent<PlayPanelCard>();

                // ── Illustration (capa base) ───────────────────────────────
                GameObject illGO = new GameObject("Illustration");
                illGO.layer = layer;
                illGO.transform.SetParent(panelGO.transform, false);

                RectTransform illRt = illGO.AddComponent<RectTransform>();
                illRt.anchorMin        = Vector2.zero;
                illRt.anchorMax        = Vector2.one;
                illRt.anchoredPosition = Vector2.zero;
                illRt.sizeDelta        = Vector2.zero;

                Image illImg = illGO.AddComponent<Image>();
                illImg.raycastTarget = false;

                Sprite sprIll = LoadSprite(illustrationName, KIT_PANELS);
                if (sprIll != null)
                {
                    illImg.sprite         = sprIll;
                    illImg.type           = Image.Type.Simple;
                    illImg.preserveAspect = false;
                }
                else
                    Debug.LogWarning($"[PlayPanel Setup] Ilustración no encontrada: {illustrationName}");

                // ── Frame (encima de la ilustración) ──────────────────────
                GameObject frameGO = new GameObject("Frame");
                frameGO.layer = layer;
                frameGO.transform.SetParent(panelGO.transform, false);

                RectTransform frameRt = frameGO.AddComponent<RectTransform>();
                frameRt.anchorMin        = Vector2.zero;
                frameRt.anchorMax        = Vector2.one;
                frameRt.anchoredPosition = Vector2.zero;
                frameRt.sizeDelta        = Vector2.zero;

                Image frameImg = frameGO.AddComponent<Image>();
                frameImg.raycastTarget = false;
                if (sprNormal != null)
                {
                    frameImg.sprite = sprNormal;
                    frameImg.type   = Image.Type.Simple;
                }

                // ── Title (texto del modo) ────────────────────────────────
                GameObject titleGO = new GameObject("Title");
                titleGO.layer = layer;
                titleGO.transform.SetParent(panelGO.transform, false);

                RectTransform titleRt = titleGO.AddComponent<RectTransform>();
                titleRt.anchorMin        = new Vector2(0f, 0f);
                titleRt.anchorMax        = new Vector2(1f, 0f);
                titleRt.pivot            = new Vector2(0.5f, 0f);
                titleRt.anchoredPosition = new Vector2(0f, 24f);
                titleRt.sizeDelta        = new Vector2(0f, 48f);

                TMP_Text tmp = titleGO.AddComponent<TextMeshProUGUI>();
                tmp.text           = title;
                tmp.alignment      = TextAlignmentOptions.Center;
                tmp.fontSize       = 28f;
                tmp.color          = new Color(0.95f, 0.82f, 0.4f, 1f); // dorado
                tmp.raycastTarget  = false;

                // ── Asignar referencias al PlayPanelCard ──────────────────
                card.frameImage  = frameImg;
                card.titleText   = tmp;
                card.spriteNormal = sprNormal;
                card.spriteHover  = sprHover;
                card.spriteActive = sprActive;

                Debug.Log($"[PlayPanel Setup] ✓ {id} creado");
            }

            // ── Marcar escena modificada ──────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Play Panels creados",
                "Los 3 paneles de modo de juego están listos.\n\n" +
                "Próximos pasos:\n" +
                "1. Guarda la escena (Ctrl+S)\n" +
                "2. Conecta los OnClick de cada panel a MainMenu (OnClickSolo, OnClickDesafio, etc.)\n" +
                "3. Ajusta posición de PlayPanelZone en el Inspector si hace falta",
                "OK");
        }

        private static Sprite LoadSprite(string name, string folder)
        {
            string[] guids = AssetDatabase.FindAssets($"{name} t:Sprite", new[] { folder });
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
