using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;
using TcgEngine.FX;

namespace TcgEngine.Client
{
    /// <summary>
    /// Visual representation of a card found in a pack (once opened)
    /// The card can be flipped by clicking on it
    /// </summary>

    public class PackCard : MonoBehaviour
    {
        public float move_speed = 5f;
        public float flip_speed = 10f;

        public SpriteRenderer cardback;
        public CardUI card_ui;

        public GameObject new_card;

        [Header("FX")]
        public GameObject card_flip_fx;
        public GameObject card_rare_flip_fx;
        public AudioClip card_flip_audio;
        public AudioClip card_rare_flip_audio;

        [Header("FX carta nueva")]
        public GameObject new_card_fx;          // Efecto de partículas al revelar carta nueva
        public Color      new_card_flash_color  = new Color(1f, 0.85f, 0.25f, 1f); // Dorado
        public float      new_card_flash_in     = 0.12f; // segundos para llegar al dorado
        public float      new_card_flash_out    = 1.4f;  // segundos para volver al color base

        private CardData icard;
        private VariantData variant;

        private Vector3 target;
        private Quaternion rtarget;
        private bool revealed = false;
        private bool removed = false;
        private bool is_new = false;
        private float timer = 0f;

        private static List<PackCard> card_list = new List<PackCard>();

        void Awake()
        {
            card_list.Add(this);
        }

        private void OnDestroy()
        {
            card_list.Remove(this);
        }

        void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, target, move_speed * Time.deltaTime);

            if (revealed)
            {
                timer += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, rtarget, flip_speed * Time.deltaTime);
            }

            if (removed && timer > 4f)
                Destroy(gameObject);
        }

        public void SetCard(PackData pack, CardData card, VariantData variant)
        {
            this.icard = card;
            this.variant = variant;

            if (cardback != null)
                cardback.sprite = pack.cardback_img;

            card_ui.SetCard(card, variant);
            new_card?.SetActive(false);

            UserData udata = Authenticator.Get().GetUserData();
            is_new = !udata.HasCard(icard.id, variant.id);
        }

        public void SetTarget(Vector3 pos)
        {
            target = pos;
            rtarget = Quaternion.Euler(0f, 180f, 0f);
            transform.rotation = rtarget;
        }

        public void Reveal()
        {
            if (revealed)
                return;

            revealed = true;
            rtarget = Quaternion.Euler(0f, 0f, 0f);
            new_card?.SetActive(is_new);

            if (icard != null && icard.rarity.rank >= 3)
            {
                FXTool.DoFX(card_rare_flip_fx, transform.position);
                AudioTool.Get().PlaySFX("pack_open", card_rare_flip_audio);
            }
            else
            {
                FXTool.DoFX(card_flip_fx, transform.position);
                AudioTool.Get().PlaySFX("pack_open", card_flip_audio);
            }

            // Efectos extra para cartas nuevas
            if (is_new)
            {
                if (new_card_fx != null)
                    FXTool.DoFX(new_card_fx, transform.position);

                StartCoroutine(FlashNewCardBorder());
            }
        }

        private IEnumerator FlashNewCardBorder()
        {
            if (card_ui?.frame_image == null) yield break;

            Color original = card_ui.frame_image.color;

            // Flash rápido → dorado
            float t = 0f;
            while (t < new_card_flash_in)
            {
                t += Time.deltaTime;
                card_ui.frame_image.color = Color.Lerp(original, new_card_flash_color, t / new_card_flash_in);
                yield return null;
            }
            card_ui.frame_image.color = new_card_flash_color;

            // Fade suave → color original
            t = 0f;
            while (t < new_card_flash_out)
            {
                t += Time.deltaTime;
                card_ui.frame_image.color = Color.Lerp(new_card_flash_color, original, t / new_card_flash_out);
                yield return null;
            }
            card_ui.frame_image.color = original;
        }

        public void Remove()
        {
            if (removed)
                return;

            removed = true;
            timer = 0f;
            target = Vector3.up * 10f;
        }

        public void OnMouseDown()
        {
            if (!GameUI.IsOverUILayer("UI"))
            {
                Reveal();
            }
        }

        public bool IsRevealed()
        {
            return revealed && timer > 0.5f;
        }

        public static List<PackCard> GetAll()
        {
            return card_list;
        }
    }
}