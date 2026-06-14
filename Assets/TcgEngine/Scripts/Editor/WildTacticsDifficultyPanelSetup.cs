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
    /// Reconstruye DifficultyPanel con el UI Kit de WildTactics.
    /// Usa VerticalLayoutGroup + HorizontalLayoutGroup para evitar cálculos manuales de RectTransform.
    /// Menú: WildTactics → Setup Difficulty Panel
    /// </summary>
    public static class WildTacticsDifficultyPanelSetup
    {
        private const string KIT_PANELS = "Assets/UI_KIT_WILDTACTICS/Sprites/Panels";
        private const string KIT_ICONS  = "Assets/UI_KIT_WILDTACTICS/Sprites/Icons";

        private static readonly (string id, string label, string iconName, Color bgColor, Color labelColor)[] ROWS =
        {
            ("Row_Easy",        "FÁCIL",       "WT_Difficulty_I",   new Color(0.20f,0.55f,0.20f,0.80f), new Color(0.40f,0.90f,0.40f)),
            ("Row_Casual",      "CASUAL",      "WT_Difficulty_II",  new Color(0.10f,0.30f,0.60f,0.80f), new Color(0.35f,0.70f,1.00f)),
            ("Row_Competitive", "COMPETITIVO", "WT_Difficulty_III", new Color(0.60f,0.12f,0.12f,0.80f), new Color(1.00f,0.30f,0.30f)),
        };

        [MenuItem("WildTactics/Setup Difficulty Panel")]
        public static void SetupDifficultyPanel()
        {
            GameObject panelGO = GameObject.Find("DifficultyPanel");
            if (panelGO == null)
            {
                EditorUtility.DisplayDialog("Error", "No se encontró 'DifficultyPanel'.\nAbre Menu.unity primero.", "OK");
                return;
            }

            DifficultyPanel script = panelGO.GetComponent<DifficultyPanel>();
            if (script == null)
            {
                EditorUtility.DisplayDialog("Error", "DifficultyPanel no tiene el componente DifficultyPanel.cs.", "OK");
                return;
            }

            bool ok = EditorUtility.DisplayDialog("Setup Difficulty Panel",
                "Se eliminarán los hijos actuales y se reconstruirán. ¿Continuar?",
                "Sí", "Cancelar");
            if (!ok) return;

            // ── Limpiar hijos ─────────────────────────────────────────────────────
            for (int i = panelGO.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(panelGO.transform.GetChild(i).gameObject);

            int layer = panelGO.layer;

            // ── Sprites ───────────────────────────────────────────────────────────
            Sprite sprPanel    = LoadSprite("WT_DifficultyPanel_Frame",       KIT_PANELS);
            Sprite sprRow      = LoadSprite("WT_DifficultyRow_Frame",         KIT_PANELS);
            Sprite sprGrading  = LoadSprite("WT_DifficultyRow_Grading",       KIT_PANELS);
            Sprite sprSep      = LoadSprite("WT_TopBar_Difficulty_Separator", KIT_PANELS);
            Sprite sprCoin     = LoadSprite("WT_WildCoin4",                   KIT_ICONS);
            Sprite sprClose    = LoadSprite("WT_Icon_Close",                  KIT_ICONS);

            // ── PanelFrame (fondo, no bloquea layout) ─────────────────────────────
            if (sprPanel != null)
            {
                var frameRt = MakeChild(panelGO, "PanelFrame", layer);
                Stretch(frameRt);
                var img    = frameRt.gameObject.AddComponent<Image>();
                img.sprite = sprPanel;
                img.type   = Image.Type.Simple;
                img.raycastTarget = false;
            }

            // ── Content (VerticalLayoutGroup sobre el panel entero) ───────────────
            var contentRt = MakeChild(panelGO, "Content", layer);
            Stretch(contentRt);

            var vlg                    = contentRt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.spacing                = 16f;
            vlg.padding                = new RectOffset(20, 20, 20, 20);
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            // ── Header ────────────────────────────────────────────────────────────
            {
                var hRt = MakeChild(contentRt.gameObject, "Header", layer);
                var le  = hRt.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 90f;
                le.flexibleWidth   = 1f;

                var titleRt = MakeChild(hRt.gameObject, "Title", layer);
                Stretch(titleRt);
                titleRt.anchorMin = new Vector2(0f, 0.5f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.sizeDelta = Vector2.zero;
                var title       = titleRt.gameObject.AddComponent<TextMeshProUGUI>();
                title.text      = "SOLO";
                title.alignment = TextAlignmentOptions.Center;
                title.fontSize  = 38f;
                title.fontStyle = FontStyles.Bold;
                title.color     = new Color(0.95f, 0.82f, 0.40f);
                title.raycastTarget = false;

                var subRt  = MakeChild(hRt.gameObject, "Subtitle", layer);
                subRt.anchorMin = new Vector2(0f, 0f);
                subRt.anchorMax = new Vector2(1f, 0.5f);
                subRt.sizeDelta = Vector2.zero;
                var sub       = subRt.gameObject.AddComponent<TextMeshProUGUI>();
                sub.text      = "ELIGE TU DIFICULTAD";
                sub.alignment = TextAlignmentOptions.Center;
                sub.fontSize  = 16f;
                sub.color     = new Color(0.75f, 0.65f, 0.50f);
                sub.raycastTarget = false;

                if (sprSep != null)
                {
                    var sepRt  = MakeChild(hRt.gameObject, "Separator", layer);
                    sepRt.anchorMin        = new Vector2(0.05f, 0f);
                    sepRt.anchorMax        = new Vector2(0.95f, 0f);
                    sepRt.pivot            = new Vector2(0.5f, 1f);
                    sepRt.anchoredPosition = new Vector2(0f, -2f);
                    sepRt.sizeDelta        = new Vector2(0f, 10f);
                    var sepImg             = sepRt.gameObject.AddComponent<Image>();
                    sepImg.sprite          = sprSep;
                    sepImg.type            = Image.Type.Simple;
                    sepImg.preserveAspect  = false;
                    sepImg.raycastTarget   = false;
                }
            }

            // ── Btn_Cancel ────────────────────────────────────────────────────────
            Button btnCancel;
            {
                var bRt = MakeChild(panelGO, "Btn_Cancel", layer);   // hijo de panel, no del VLG
                bRt.anchorMin        = new Vector2(1f, 1f);
                bRt.anchorMax        = new Vector2(1f, 1f);
                bRt.pivot            = new Vector2(1f, 1f);
                bRt.anchoredPosition = new Vector2(-12f, -12f);
                bRt.sizeDelta        = new Vector2(44f, 44f);
                var img              = bRt.gameObject.AddComponent<Image>();
                if (sprClose != null) img.sprite = sprClose;
                img.preserveAspect   = true;
                btnCancel            = bRt.gameObject.AddComponent<Button>();
            }

            // ── Filas ─────────────────────────────────────────────────────────────
            Image[]    bgImgs  = new Image[3];
            Image[]    icons   = new Image[3];
            TMP_Text[] descs   = new TMP_Text[3];
            TMP_Text[] rewards = new TMP_Text[3];
            Button[]   btns    = new Button[3];

            for (int i = 0; i < ROWS.Length; i++)
            {
                var (id, label, iconName, bgColor, labelColor) = ROWS[i];

                // Raíz de la fila
                var rowRt = MakeChild(contentRt.gameObject, id, layer);
                var rowLE = rowRt.gameObject.AddComponent<LayoutElement>();
                rowLE.preferredHeight = 130f;
                rowLE.flexibleWidth   = 1f;

                // Área de click (invisible, cubre toda la fila)
                var hitImg          = rowRt.gameObject.AddComponent<Image>();
                hitImg.color        = new Color(0f, 0f, 0f, 0f);
                var rowBtn          = rowRt.gameObject.AddComponent<Button>();
                btns[i]             = rowBtn;

                // BG (frame de fila, tintado)
                var bgRt   = MakeChild(rowRt.gameObject, "BG", layer);
                Stretch(bgRt);
                var bgImg  = bgRt.gameObject.AddComponent<Image>();
                if (sprRow != null) { bgImg.sprite = sprRow; bgImg.type = Image.Type.Sliced; }
                bgImg.color         = bgColor;
                bgImg.raycastTarget = false;
                bgImgs[i]           = bgImg;

                // Grading (degradado overlay)
                if (sprGrading != null)
                {
                    var gRt  = MakeChild(rowRt.gameObject, "Grading", layer);
                    Stretch(gRt);
                    var gImg = gRt.gameObject.AddComponent<Image>();
                    gImg.sprite        = sprGrading;
                    gImg.preserveAspect = false;
                    gImg.raycastTarget = false;
                }

                // HorizontalLayoutGroup dentro de la fila
                var hlgRt = MakeChild(rowRt.gameObject, "HLayout", layer);
                Stretch(hlgRt);
                var hlg                    = hlgRt.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment         = TextAnchor.MiddleLeft;
                hlg.spacing                = 10f;
                hlg.padding                = new RectOffset(10, 12, 8, 8);
                hlg.childControlWidth      = true;
                hlg.childControlHeight     = true;
                hlg.childForceExpandWidth  = false;
                hlg.childForceExpandHeight = true;

                // Icono
                Sprite sprIcon = LoadSprite(iconName, KIT_ICONS);
                var iconRt     = MakeChild(hlgRt.gameObject, "Icon", layer);
                var iconLE     = iconRt.gameObject.AddComponent<LayoutElement>();
                iconLE.preferredWidth  = 80f;
                iconLE.preferredHeight = 80f;
                var iconImg            = iconRt.gameObject.AddComponent<Image>();
                if (sprIcon != null) iconImg.sprite = sprIcon;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget  = false;
                icons[i]               = iconImg;

                // TextGroup (VerticalLayoutGroup)
                var tgRt = MakeChild(hlgRt.gameObject, "TextGroup", layer);
                var tgLE = tgRt.gameObject.AddComponent<LayoutElement>();
                tgLE.flexibleWidth  = 1f;
                tgLE.preferredHeight = 110f;
                var tvlg                    = tgRt.gameObject.AddComponent<VerticalLayoutGroup>();
                tvlg.childAlignment         = TextAnchor.MiddleLeft;
                tvlg.spacing                = 4f;
                tvlg.childControlWidth      = true;
                tvlg.childControlHeight     = true;
                tvlg.childForceExpandWidth  = true;
                tvlg.childForceExpandHeight = false;

                var lblRt  = MakeChild(tgRt.gameObject, "Label", layer);
                var lblLE  = lblRt.gameObject.AddComponent<LayoutElement>();
                lblLE.preferredHeight = 36f;
                var lbl    = lblRt.gameObject.AddComponent<TextMeshProUGUI>();
                lbl.text   = label;
                lbl.fontSize = 24f;
                lbl.fontStyle = FontStyles.Bold;
                lbl.color  = labelColor;
                lbl.raycastTarget = false;

                var descRt = MakeChild(tgRt.gameObject, "Desc", layer);
                var descLE = descRt.gameObject.AddComponent<LayoutElement>();
                descLE.preferredHeight = 52f;
                var desc   = descRt.gameObject.AddComponent<TextMeshProUGUI>();
                desc.text  = "—";
                desc.fontSize = 13f;
                desc.color = new Color(0.80f, 0.75f, 0.65f);
                desc.raycastTarget = false;
                descs[i]   = desc;

                // RewardGroup (VerticalLayoutGroup)
                var rgRt = MakeChild(hlgRt.gameObject, "RewardGroup", layer);
                var rgLE = rgRt.gameObject.AddComponent<LayoutElement>();
                rgLE.preferredWidth  = 110f;
                rgLE.preferredHeight = 110f;
                var rvlg                    = rgRt.gameObject.AddComponent<VerticalLayoutGroup>();
                rvlg.childAlignment         = TextAnchor.MiddleCenter;
                rvlg.spacing                = 4f;
                rvlg.childControlWidth      = true;
                rvlg.childControlHeight     = true;
                rvlg.childForceExpandWidth  = true;
                rvlg.childForceExpandHeight = false;

                var rewLblRt = MakeChild(rgRt.gameObject, "RewardLabel", layer);
                var rewLblLE = rewLblRt.gameObject.AddComponent<LayoutElement>();
                rewLblLE.preferredHeight = 20f;
                var rewLbl   = rewLblRt.gameObject.AddComponent<TextMeshProUGUI>();
                rewLbl.text  = "RECOMPENSA";
                rewLbl.fontSize = 10f;
                rewLbl.alignment = TextAlignmentOptions.Center;
                rewLbl.color = new Color(0.60f, 0.55f, 0.45f);
                rewLbl.raycastTarget = false;

                // CoinRow
                var crRt = MakeChild(rgRt.gameObject, "CoinRow", layer);
                var crLE = crRt.gameObject.AddComponent<LayoutElement>();
                crLE.preferredHeight = 36f;
                var crHlg                    = crRt.gameObject.AddComponent<HorizontalLayoutGroup>();
                crHlg.childAlignment         = TextAnchor.MiddleCenter;
                crHlg.spacing                = 5f;
                crHlg.childControlWidth      = true;
                crHlg.childControlHeight     = true;
                crHlg.childForceExpandWidth  = false;
                crHlg.childForceExpandHeight = true;

                var coinRt = MakeChild(crRt.gameObject, "CoinIcon", layer);
                var coinLE = coinRt.gameObject.AddComponent<LayoutElement>();
                coinLE.preferredWidth = 28f;
                var coinImg = coinRt.gameObject.AddComponent<Image>();
                if (sprCoin != null) coinImg.sprite = sprCoin;
                coinImg.preserveAspect = true;
                coinImg.raycastTarget  = false;

                var rewRt  = MakeChild(crRt.gameObject, "Reward", layer);
                var rewLE  = rewRt.gameObject.AddComponent<LayoutElement>();
                rewLE.preferredWidth = 70f;
                var rew    = rewRt.gameObject.AddComponent<TextMeshProUGUI>();
                rew.text   = "—";
                rew.fontSize = 20f;
                rew.fontStyle = FontStyles.Bold;
                rew.color  = labelColor;
                rew.raycastTarget = false;
                rewards[i] = rew;
            }

            // ── Auto-asignar referencias ──────────────────────────────────────────
            script.row_easy         = btns[0];
            script.row_casual       = btns[1];
            script.row_competitive  = btns[2];
            script.btn_cancel       = btnCancel;

            script.bg_easy          = bgImgs[0];
            script.bg_casual        = bgImgs[1];
            script.bg_competitive   = bgImgs[2];

            script.icon_easy        = icons[0];
            script.icon_casual      = icons[1];
            script.icon_competitive = icons[2];

            script.desc_easy        = descs[0];
            script.desc_casual      = descs[1];
            script.desc_competitive = descs[2];

            script.reward_easy        = rewards[0];
            script.reward_casual      = rewards[1];
            script.reward_competitive = rewards[2];

            // ── Conectar botones ──────────────────────────────────────────────────
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btns[0].onClick, script.OnClickEasy);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btns[1].onClick, script.OnClickCasual);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btns[2].onClick, script.OnClickCompetitive);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btnCancel.onClick, script.OnClickCancel);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("DifficultyPanel listo",
                "Jerarquía reconstruida con LayoutGroups.\nGuarda la escena (Ctrl+S).", "OK");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Crea un hijo UI con RectTransform añadido ANTES del SetParent para evitar conflictos.</summary>
        private static RectTransform MakeChild(GameObject parent, string name, int layer)
        {
            GameObject go    = new GameObject(name);
            go.layer         = layer;
            RectTransform rt = go.AddComponent<RectTransform>();   // ANTES de SetParent
            go.transform.SetParent(parent.transform, false);
            return rt;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = Vector2.zero;
        }

        private static Sprite LoadSprite(string name, string folder)
        {
            string[] guids = AssetDatabase.FindAssets($"{name} t:Sprite", new[] { folder });
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
