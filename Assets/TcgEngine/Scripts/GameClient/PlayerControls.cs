using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using UnityEngine.Events;
using TcgEngine.UI;
using TcgEngine.FX;


namespace TcgEngine.Client
{
    /// <summary>
    /// Script that contain main controls for clicking on cards, attacking, activating abilities
    /// Holds the currently selected card and will send action to GameClient on click release
    /// </summary>

    public class PlayerControls : MonoBehaviour
    {
        private BoardCard selected_card = null;

        private static PlayerControls instance;

        void Awake()
        {
            instance = this;
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            if (Input.GetMouseButtonDown(1))
                UnselectAll();

            if (selected_card != null)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    ReleaseClick();
                }
            }
        }

        public void SelectCard(BoardCard bcard)
        {
            Game gdata = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Card card = bcard.GetFocusCard();

            Debug.Log("🧪 player_id: " + player.player_id);
            Debug.Log("🧪 current_player: " + gdata.current_player);
            Debug.Log("🧪 phase: " + gdata.phase);
            Debug.Log("🧪 selector: " + gdata.selector);
            Debug.Log("🧪 state: " + gdata.state);

            if (gdata.IsPlayerSelectorTurn(player) && gdata.selector == SelectorType.SelectTarget)
            {
                if (!Tutorial.Get().CanDo(TutoEndTrigger.SelectTarget, card))
                    return;

                //Target selector, select this card
                GameClient.Get().SelectCard(card);
            }
            else if (gdata.IsPlayerActionTurn(player) && card.player_id == player.player_id)
            {
                UnselectAll(); // Limpia cualquier selección anterior
                selected_card = bcard;
                selected_card.SetSelectedVisual(true); // Aplica glow dorado

                Debug.Log("✅ Carta seleccionada correctamente: " + card.card_id);
            }
        }

        public void SelectCardRight(BoardCard card)
        {
            if (!Input.GetMouseButton(0))
            {
                //Nothing on right-click
            }
        }

        private void ReleaseClick()
        {
            bool yourturn = GameClient.Get().IsYourTurn();

            if (yourturn && selected_card != null)
            {
                Debug.Log("🎯 ReleaseClick ejecutado");
                Card card = selected_card.GetCard();
                Debug.Log("📌 Carta seleccionada: " + card.card_id);
                Vector3 wpos = GameBoard.Get().RaycastMouseBoard();
                BSlot tslot = BSlot.GetNearest(wpos);
                Card target = tslot?.GetSlotCard(wpos);
                Debug.Log("📍 Slot objetivo: " + (tslot != null ? tslot.ToString() : "null"));
                Debug.Log("🏹 Carta objetivo: " + (target != null ? target.card_id : "null"));

                AbilityButton ability = AbilityButton.GetFocus(wpos, 1f);

                if (ability != null && ability.IsInteractable())
                {
                    if (!Tutorial.Get().CanDo(TutoEndTrigger.CastAbility, card))
                        return;

                    GameClient.Get().CastAbility(card, ability.GetAbility());
                }
                else if (target != null && target.uid != card.uid && target.player_id != card.player_id)
                {
                    Debug.Log("🛡️ target.player_id: " + target.player_id + " vs card.player_id: " + card.player_id);

                    if (GameClient.Get().GetGameData().has_attacked_this_turn)
                    {
                        WarningText.ShowText("⚠️ Ya has atacado este turno");
                        return;
                    }

                    if (!Tutorial.Get().CanDo(TutoEndTrigger.Attack, card) && !Tutorial.Get().CanDo(TutoEndTrigger.Attack, target))
                        return;

                    if (card.exhausted)
                        WarningText.ShowExhausted();
                    else
                    {
                        Debug.Log("🗡️ Enviando ataque a GameClient");
                        GameClient.Get().ApplyAttack(card, target);  // Ataca directamente en local
                        Debug.Log($"🗡️ Intentando atacar: {card.card_id} → {target.card_id}");

                        GameManager.instance.GameData.EndTurn(); // Avanza turno tras el ataque

                        Object.FindFirstObjectByType<MouseLineFX>()?.Hide();
                        UnselectAll();               // ✅ Limpia selección visual
                    }
                }
                else if (tslot != null && tslot is BoardSlot)
                {
                    if (!Tutorial.Get().CanDo(TutoEndTrigger.Move, tslot.GetSlot()))
                        return;

                    GameClient.Get().Move(card, tslot.GetSlot());
                }
            }
        }

        public void UnselectAll()
        {
            if (selected_card != null)
                selected_card.SetSelectedVisual(false);

            selected_card = null;
        }

        public BoardCard GetSelected()
        {
            return selected_card;
        }

        public static PlayerControls Get()
        {
            return instance;
        }
    }
}