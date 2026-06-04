using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TcgEngine;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Pestaña "Avatares" dentro de PackPanel.
    /// Muestra en cuadrícula todos los AvatarData con unlock_type = Shop.
    /// Al seleccionar uno aparece el panel derecho con previsualización y el botón COMPRAR
    /// (que se posiciona en el mismo lugar que el botón ABRIR de PACKS).
    ///
    /// Jerarquía esperada en Unity (hijo de PackPanel):
    ///
    ///   AvatarShopPanel   (UIPanel + CanvasGroup + este script, Stretch-Stretch)
    ///   ├── Background            (Image — mismo estilo que Scroll View)
    ///   ├── LeftPanel             (RectTransform — anchors 0,0 → 0.60,1, padding bottom 60)
    ///   │   └── AvatarGrid        (GridLayout, ContentSizeFitter)
    ///   │       ├── Avatar1 … AvatarN
    ///   ├── BuySection            (RectTransform — anchors 0.62,0 → 1,1, padding bottom 60)
    ///   │   ├── AvatarPreview     (Image + AvatarUI)
    ///   │   ├── NameText          (TMP_Text)
    ///   │   ├── PriceText         (TMP_Text)
    ///   │   └── FeedbackText      (TMP_Text)
    ///   └── Btn_Comprar           (Button — anchored igual que OpenPack/ABRIR)
    /// </summary>
    public class AvatarShopPanel : UIPanel
    {
        [Header("Cuadrícula de avatares")]
        [Tooltip("Un AvatarUI por cada avatar de tienda, en orden de sort_order.")]
        public AvatarUI[] avatar_slots;

        [Header("Panel derecho (se activa al seleccionar)")]
        public GameObject buy_section;    // BuySection — se activa al seleccionar un slot
        public AvatarUI   preview;        // imagen grande del avatar seleccionado
        public TMP_Text   name_text;
        public TMP_Text   price_text;
        public TMP_Text   feedback_text;

        [Header("Botones")]
        [Tooltip("El botón COMPRAR, posicionado donde está ABRIR.")]
        public Button     btn_comprar;
        [Tooltip("El GameObject OpenPack (botón ABRIR) en PackPanel — se oculta en la pestaña AVATARES.")]
        public GameObject open_pack_button;

        // ── Estado ────────────────────────────────────────────────────────────
        private AvatarData selected;

        private static AvatarShopPanel _instance;
        public static AvatarShopPanel Get() => _instance;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _instance = this;

            foreach (AvatarUI slot in avatar_slots)
                if (slot != null)
                    slot.onClick += OnClickSlot;
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            selected = null;
            if (buy_section != null)      buy_section.SetActive(false);
            if (feedback_text != null)    feedback_text.text = "";
            if (open_pack_button != null) open_pack_button.SetActive(false);
            if (btn_comprar != null)      btn_comprar.interactable = false;
            RefreshGrid();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            if (open_pack_button != null) open_pack_button.SetActive(true);
            if (btn_comprar != null)      btn_comprar.interactable = false;
        }

        // ── Populate cuadrícula ───────────────────────────────────────────────

        private void RefreshGrid()
        {
            foreach (AvatarUI slot in avatar_slots)
                if (slot != null) slot.Hide();

            UserData udata = Authenticator.Get()?.UserData;

            int index = 0;
            foreach (AvatarData adata in AvatarData.GetAll())
            {
                if (adata.unlock_type != AvatarUnlockType.Shop) continue;
                if (index >= avatar_slots.Length) break;

                AvatarUI slot = avatar_slots[index];
                if (slot == null) { index++; continue; }

                slot.SetAvatar(adata);
                bool owned = udata != null && udata.HasAvatar(adata.id);
                slot.SetLocked(owned);
                index++;
            }
        }

        // ── Selección ─────────────────────────────────────────────────────────

        private void OnClickSlot(AvatarData adata)
        {
            if (adata == null) return;
            selected = adata;

            UserData udata = Authenticator.Get()?.UserData;
            bool owned = udata != null && udata.HasAvatar(adata.id);

            if (preview    != null) preview.SetAvatar(adata);
            if (name_text  != null) name_text.text  = FormatName(adata.id);
            if (price_text != null) price_text.text = owned ? "Ya poseído" : $"{adata.unlock_amount} WC";
            if (feedback_text != null) feedback_text.text = "";
            if (buy_section != null) buy_section.SetActive(true);
            if (btn_comprar != null) btn_comprar.interactable = !owned;
        }

        // ── Compra (Btn_Comprar → OnClickBuy) ────────────────────────────────

        public async void OnClickBuy()
        {
            if (selected == null) return;

            UserData udata = Authenticator.Get()?.UserData;
            if (udata == null) return;

            if (udata.HasAvatar(selected.id))
            {
                ShowFeedback("Ya tienes este avatar.", success: false);
                return;
            }

            if (udata.coins < selected.unlock_amount)
            {
                ShowFeedback($"WildCoins insuficientes (necesitas {selected.unlock_amount} WC).", success: false);
                return;
            }

            udata.coins -= selected.unlock_amount;
            udata.AddAvatar(selected.id);

            await Authenticator.Get().SaveUserData();
            MainMenu.Get()?.RefreshUserData();

            ShowFeedback("¡Avatar desbloqueado!", success: true);
            RefreshGrid();

            if (price_text  != null) price_text.text = "Ya poseído";
            if (btn_comprar != null) btn_comprar.interactable = false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ShowFeedback(string msg, bool success)
        {
            if (feedback_text == null) return;
            feedback_text.text  = msg;
            feedback_text.color = success
                ? new Color(0.2f, 0.8f, 0.2f)
                : new Color(0.9f, 0.3f, 0.3f);
        }

        /// <summary>"fire1" → "Fire 1"</summary>
        private static string FormatName(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            int split = id.Length - 1;
            while (split > 0 && char.IsDigit(id[split - 1])) split--;
            string prefix = id.Substring(0, split);
            string number = id.Substring(split);
            string display = char.ToUpper(prefix[0]) + prefix.Substring(1);
            return number.Length > 0 ? $"{display} {number}" : display;
        }
    }
}
