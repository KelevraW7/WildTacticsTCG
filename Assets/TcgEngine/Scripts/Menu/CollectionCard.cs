using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// Visual representation of a card in your collection in the Deckbuilder
    /// </summary>

    public class CollectionCard : MonoBehaviour
    {
        public CardUI card_ui;
        public Image quantity_bar;
        public Text quantity;

        [Header("Mat")]
        public Material color_mat;
        public Material grayscale_mat;

        [Header("Pokédex — carta bloqueada")]
        public Image locked_image;   // Image a tamaño completo que cubre la carta (asignar en prefab)

        public UnityAction<CardUI> onClick;
        public UnityAction<CardUI> onClickRight;

        private void Start()
        {
            card_ui.onClick += onClick;
            card_ui.onClickRight += onClickRight;
        }

        public void SetCard(CardData card, VariantData variant, int quantity)
        {
            card_ui.SetCard(card, variant);
            SetQuantity(quantity);
        }

        public void SetQuantity(int quantity)
        {
            if (this.quantity_bar != null)
                this.quantity_bar.enabled = quantity > 0;
            if (this.quantity != null)
                this.quantity.text = quantity.ToString();
            if (this.quantity != null)
                this.quantity.enabled = quantity > 0;
        }

        public void SetGrayscale(bool grayscale)
        {
            if (grayscale)
            {
                quantity_bar.material = grayscale_mat;
                quantity_bar.material = grayscale_mat;
                card_ui.SetMaterial(grayscale_mat);
            }
            else
            {
                quantity_bar.material = color_mat;
                quantity_bar.material = color_mat;
                card_ui.SetMaterial(color_mat);
            }
        }

        /// <summary>
        /// Pokédex: muestra el reverso de la carta si está bloqueada, o la carta normal si está desbloqueada.
        /// Requiere que locked_image esté asignado en el prefab; si no, hace fallback a grayscale.
        /// </summary>
        public void SetLocked(bool locked, Sprite locked_sprite)
        {
            if (locked_image != null)
            {
                if (locked && locked_sprite != null)
                    locked_image.sprite = locked_sprite;

                locked_image.gameObject.SetActive(locked);
                card_ui.gameObject.SetActive(!locked);
            }
            else
            {
                // Fallback si no hay locked_image en el prefab
                card_ui.gameObject.SetActive(true);
                SetGrayscale(locked);
            }

            // En estado bloqueado, ocultar cantidad. En desbloqueado, respetar lo que SetQuantity dejó.
            if (locked)
            {
                if (quantity_bar != null) quantity_bar.enabled = false;
                if (quantity     != null) quantity.enabled     = false;
            }
        }

        public CardData GetCard()
        {
            return card_ui.GetCard();
        }

        public VariantData GetVariant()
        {
            return card_ui.GetVariant();
        }
    }
}