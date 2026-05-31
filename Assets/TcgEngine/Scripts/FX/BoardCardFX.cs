using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using UnityEngine.Events;
using TcgEngine;

namespace TcgEngine.FX
{
    /// <summary>
    /// All FX/anims related to a card on the board
    /// </summary>

    public class BoardCardFX : MonoBehaviour
    {
        public Material kill_mat;
        public string kill_mat_fade = "noise_fade";
        public Card card;
        public CardData icard;

        [Header("Wild Tactics FX")]
        [Tooltip("Assign ElectricFX.prefab — used for DESTROZAR attack animation")]
        public GameObject destrozar_attack_fx;

        [Tooltip("Impact FX played ON THE TARGET when a DESTROZAR hit lands (~0.35s). Try VFX_Critical_01 or VFX_Critical_02.")]
        public GameObject destrozar_hit_fx;

        [Header("Basic Attack — Hit FX on Target")]
        [Tooltip("Hit FX on target when an EMBESTIR creature attacks. Recommended: VFX_Critical_01.prefab")]
        public GameObject embestir_hit_fx;

        [Tooltip("Hit FX on target when a SUMERGIR creature attacks. Recommended: CFXR Water Splash (Smaller).prefab")]
        public GameObject sumergir_hit_fx;

        [Tooltip("Hit FX on target when a VOLAR creature attacks. Recommended: CFXR4 Wind Trails.prefab")]
        public GameObject volar_hit_fx;

        [Tooltip("Hit FX on target when an INTOXICAR creature makes a basic attack. Recommended: VFX_Poison_01.prefab")]
        public GameObject intoxicar_attack_hit_fx;

        [Tooltip("Hit FX on target when a GOLPEAR creature attacks (fires twice — double attack). Recommended: CFXR2 Ground Hit.prefab")]
        public GameObject golpear_hit_fx;

        [Tooltip("VFX played when Shell (Caparazón) absorbs a hit — assign VFX_Crystal_Burst_01")]
        public GameObject shell_break_fx;

        [Tooltip("Persistent lightning bolt while a card is Paralysed — assign SimpleLightningBoltAnimatedPrefab")]
        public GameObject paralysis_lightning_prefab;

        private BoardCard bcard;

        private ParticleSystem exhausted_fx = null;

        // Set in OnAbilityStart (fires BEFORE onCardDamaged in the same sync chain).
        // Consumed in OnCardDamaged to suppress the HitFX on the attacking card.
        private static bool next_damage_is_counter_attack = false;

        // Saved damage value so we can display it with the poison FX instead (at 0.75s)
        private int saved_counter_damage_value = 0;

        private Dictionary<StatusType, GameObject> status_fx_list = new Dictionary<StatusType, GameObject>();

        // ── Parálisis eléctrica ─────────────────────────────────────────────────────
        private bool was_paralysed = false;
        private Coroutine paralysis_spark_coroutine = null;   // fallback si no hay prefab asignado
        private GameObject paralysis_lightning_go = null;     // efecto persistente (ej. vfx_Electricity_01)

        void Awake()
        {
            bcard = GetComponent<BoardCard>();
            bcard.onKill += OnKill;
        }

        void Start()
        {
            GameClient client = GameClient.Get();
            client.onCardMoved += OnMove;
            client.onCardPlayed += OnPlayed;
            client.onCardDamaged += OnCardDamaged;
            client.onAttackStart += OnAttack;
            client.onAbilityStart += OnAbilityStart;
            client.onAbilityTargetCard += OnAbilityEffect;
            client.onAbilityEnd += OnAbilityAfter;

            StartCoroutine(WaitAndSpawn());
        }

        private IEnumerator WaitAndSpawn()
        {
            Debug.Log("🕓 [BoardCardFX] WaitAndSpawn iniciado para: " + gameObject.name);

            int timeout = 50;
            while (card == null && timeout-- > 0)
            {
                card = GetComponent<BoardCard>()?.GetCard();
                yield return null;
            }

            if (card == null)
            {
                Debug.LogWarning("❌ [BoardCardFX] Card sigue siendo null tras esperar: " + gameObject.name);
                yield break;
            }

            // ✅ Nueva lógica más robusta
            bool mustShowFX = bcard != null && bcard.GetCard() != null && bcard.GetCard().revealed;

            if (mustShowFX)
            {
                Debug.Log($"🔥 Ejecutando OnSpawn() para {bcard.GetCard().card_id} (spawn inmediato)");
                OnSpawn();
            }
            else
            {
                Debug.Log($"⏳ No se lanza Spawn aún para: {bcard?.GetCard()?.card_id}");
            }
        }

        private void OnDestroy()
        {
            GameClient client = GameClient.Get();
            client.onCardMoved -= OnMove;
            client.onCardPlayed -= OnPlayed;
            client.onCardDamaged -= OnCardDamaged;
            client.onAttackStart -= OnAttack;
            client.onAbilityStart -= OnAbilityStart;
            client.onAbilityTargetCard -= OnAbilityEffect;
            client.onAbilityEnd -= OnAbilityAfter;
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            Card card = bcard.GetCard();
            if (card == null)
            {
                Debug.LogWarning("⚠️ BoardCardFX: card es null para UID: " + bcard.card_uid);
                return;
            }

            //Status FX
            List<CardStatus> status_all = card.GetAllStatus();
            foreach (CardStatus status in status_all)
            {
                StatusData istatus = StatusData.Get(status.type);
                if (istatus != null && !status_fx_list.ContainsKey(status.type) && istatus.status_fx != null)
                {
                    // Never show status FX (Shell, etc.) on face-down cards — wait until revealed
                    if (!card.revealed)
                        continue;

                    GameObject fx = Instantiate(istatus.status_fx, transform);
                    fx.transform.localPosition = Vector3.zero;
                    status_fx_list[istatus.effect] = fx;
                }
            }

            //Remove status FX
            List<StatusType> remove_list = new List<StatusType>();
            foreach (KeyValuePair<StatusType, GameObject> pair in status_fx_list)
            {
                if (!card.HasStatus(pair.Key))
                {
                    remove_list.Add(pair.Key);
                    Destroy(pair.Value);

                    // Shell absorbed a hit — play the break VFX at this card's position
                    // Shell break FX is handled in ChargeInto for proper sync with attack animation
                }
            }

            foreach (StatusType status in remove_list)
                status_fx_list.Remove(status);

            // ── Parálisis eléctrica ─────────────────────────────────────────────────
            // Detecta entrada/salida del estado Paralizado y gestiona:
            //   • desaturación azul-gris del sprite mientras esté paralizado
            //   • coroutine que dispara ElectricFX en loop (~cada 0.75s)
            // Solo se activa cuando la carta está boca arriba (revealed).
            bool isParalysedNow = card.HasStatus(StatusType.Paralysed) && card.revealed;
            if (isParalysedNow != was_paralysed)
            {
                was_paralysed = isParalysedNow;
                if (isParalysedNow)
                    StartParalysedFX();
                else
                    StopParalysedFX();
            }

            //Exhausted add/remove
            if (exhausted_fx != null && !exhausted_fx.isPlaying && card.exhausted)
                exhausted_fx.Play();
            if (exhausted_fx != null && exhausted_fx.isPlaying && !card.exhausted)
                exhausted_fx.Stop();
        }

        public void OnSpawn()
        {
            if (bcard == null)
                bcard = GetComponent<BoardCard>();

            CardData icard = bcard?.GetCardData();

            if (icard == null)
            {
                Debug.LogWarning("❌ BoardCardFX → CardData es null para: " + bcard?.card_uid);
                return;
            }

            // Spawn Audio
            AudioClip audio = icard.spawn_audio != null ? icard.spawn_audio : AssetData.Get().card_spawn_audio;
            AudioTool.Get().PlaySFX("card_spawn", audio);

            // Spawn FX
            GameObject spawn_fx = icard.spawn_fx;
            if (spawn_fx == null)
            {
                Debug.LogWarning($"⚠️ Carta sin spawn_fx asignado: {icard.id}, usando FX por defecto");
                spawn_fx = AssetData.Get().card_spawn_fx;
            }
            if (spawn_fx != null)
                FXTool.DoFX(spawn_fx, transform.position);
            else
                Debug.LogWarning("⚠️ spawn_fx es null para esta carta: " + icard.id);

            // Spawn dissolve fx
            if (GameTool.IsURP())
            {
                SpriteRenderer render = bcard.card_sprite;
                render.material = kill_mat;

                FadeSetVal(bcard.card_sprite, 0f);
                FadeKill(bcard.card_sprite, 1f, 0.5f);
            }

            // Exhausted fx
            if (AssetData.Get().card_exhausted_fx != null)
            {
                GameObject efx = Instantiate(AssetData.Get().card_exhausted_fx, transform);
                efx.transform.localPosition = Vector3.zero;
                exhausted_fx = efx.GetComponent<ParticleSystem>();
            }

            // Idle status
            TimeTool.WaitFor(1f, () =>
            {
                if (icard.idle_fx != null)
                {
                    GameObject fx = Instantiate(icard.idle_fx, transform);
                    fx.transform.localPosition = Vector3.zero;
                }
            });
        }

        private void OnKill()
        {
            StartCoroutine(KillRoutine());
        }

        private IEnumerator KillRoutine()
        {
            yield return new WaitForSeconds(0.5f);

            CardData icard = bcard.GetCardData();

            //Death FX
            GameObject death_fx = icard.death_fx != null ? icard.death_fx : AssetData.Get().card_destroy_fx;
            FXTool.DoFX(death_fx, transform.position);

            //Death audio
            AudioClip audio = icard?.death_audio != null ? icard.death_audio : AssetData.Get().card_destroy_audio;
            AudioTool.Get().PlaySFX("card_spawn", audio);

            //Death dissolve fx
            if (GameTool.IsURP())
            {
                FadeKill(bcard.card_sprite, 0f, 0.5f);
            }
        }

        private void FadeSetVal(SpriteRenderer render, float val)
        {
            render.material = kill_mat;
            render.material.SetFloat(kill_mat_fade, val);
        }

        private void FadeKill(SpriteRenderer render, float val, float duration)
        {
            AnimMatFX anim = AnimMatFX.Create(render.gameObject, render.material);
            anim.SetFloat(kill_mat_fade, val, duration);
        }

        private void OnMove(Card card, Slot slot)
        {
            AudioTool.Get().PlaySFX("card_move", AssetData.Get().card_move_audio);
        }

        private void OnPlayed(Card card, Slot slot)
        {
            //Playing equipment
            Card ecard = bcard?.GetEquipCard();
            if (ecard != null && card.uid == ecard.uid && transform != null)
            {
                FXTool.DoFX(ecard.CardData.spawn_fx, transform.position);
                AudioTool.Get().PlaySFX("card_spawn", ecard.CardData.spawn_audio);
            }
        }

        private void OnCardDamaged(Card target, int damage)
        {
            Card card = bcard?.GetCard();
            if (card == null) return;

            if (card.uid == target.uid && damage > 0)
            {
                // INTOXICAR (OnBeforeDefend): suppress HitFX — will show at 0.75s with poison FX
                if (next_damage_is_counter_attack)
                {
                    next_damage_is_counter_attack = false;
                    saved_counter_damage_value = damage;
                    return;
                }

                // Número central eliminado: el daño ya aparece junto al corazón (BoardCard.ShowDamageFX)
            }
        }

        private void OnAttack(Card attacker, Card target)
        {
            Card card = bcard.GetCard();
            CardData icard = bcard.GetCardData();
            if (attacker == null || target == null)
                return;

            if (card.uid == attacker.uid)
            {
                BoardCard btarget = BoardCard.Get(target.uid);
                if (btarget != null)
                {
                    // Detect all combat abilities up front
                    bool isDestrozar    = card.abilities.Contains("wild_destrozar");
                    bool isEmbestir     = card.abilities.Contains("wild_embestir");
                    bool isSumergir     = card.abilities.Contains("wild_sumergir");
                    bool isVolar        = card.abilities.Contains("wild_volar");
                    bool isIntoxicarAtk = card.abilities.Contains("wild_intoxicar");
                    bool isGolpear      = card.abilities.Contains("wild_golpear");

                    // Select ability-specific hit FX — fires ON THE TARGET at impact time
                    GameObject hitFxPrefab =
                        isDestrozar     ? destrozar_hit_fx       :
                        isEmbestir      ? embestir_hit_fx         :
                        isSumergir      ? sumergir_hit_fx         :
                        isVolar         ? volar_hit_fx            :
                        isIntoxicarAtk  ? intoxicar_attack_hit_fx :
                        isGolpear       ? golpear_hit_fx          :
                                          null;

                    // ── Atacante muerto (INTOXICAR counter-attack) ───────────────────────────
                    // La lógica dispara onAttackStart aunque la carta esté en estado "dying"
                    // para señalar que el ataque sí se produjo. La animación ChargeInto no es
                    // posible con la carta muriendo, así que reproducimos solo el audio y el
                    // hit FX directamente en el objetivo para que el jugador vea el impacto.
                    if (bcard.IsDead())
                    {
                        AudioClip deadAudio = icard?.attack_audio != null ? icard.attack_audio : AssetData.Get().card_attack_audio;
                        AudioTool.Get().PlaySFX("card_attack", deadAudio);
                        GameObject directFx = hitFxPrefab ?? AssetData.Get()?.card_damage_fx;
                        if (directFx != null)
                            FXTool.DoFX(directFx, btarget.transform.position);
                        return;
                    }

                    // Signal BoardCard to skip its generic HitFX (arañazo) on the target —
                    // the ability-specific hit FX fired below handles the visual.
                    if (hitFxPrefab != null)
                        BoardCard.suppress_next_hit_fx = true;

                    // Card charge into target.
                    // Pass suppressDamageFX=true so the generic card_damage_fx is not shown
                    // on top of the ability-specific hit FX we will fire below.
                    ChargeInto(btarget, suppressDamageFX: hitFxPrefab != null);

                    // Attack snap FX on the attacker itself.
                    // Suppress the generic card_attack_fx when an ability-specific hit FX is
                    // assigned — avoids a mismatched slash appearing on (e.g.) the gorilla.
                    // DESTROZAR can still override with destrozar_attack_fx if one is assigned.
                    GameObject snapFx = null;
                    if (isDestrozar && destrozar_attack_fx != null)
                        snapFx = destrozar_attack_fx;
                    else if (hitFxPrefab == null)
                        snapFx = icard.attack_fx != null ? icard.attack_fx : AssetData.Get().card_attack_fx;

                    if (snapFx != null)
                        FXTool.DoSnapFX(snapFx, transform);

                    AudioClip audio = icard?.attack_audio != null ? icard.attack_audio : AssetData.Get().card_attack_audio;
                    AudioTool.Get().PlaySFX("card_attack", audio);

                    // DESTROZAR and GOLPEAR get the heavy 7-step shake; others use FX only
                    bool doHardShake = isDestrozar || isGolpear;

                    if (hitFxPrefab != null || doHardShake)
                    {
                        BoardCard shakeTarget    = btarget;
                        GameObject capturedHitFx = hitFxPrefab;
                        float hitDelay           = doHardShake ? 0.35f : 0.25f;

                        TimeTool.WaitFor(hitDelay, () =>
                        {
                            if (shakeTarget == null || shakeTarget.gameObject == null) return;

                            if (capturedHitFx != null)
                            {
                                GameObject hitFx = FXTool.DoFX(capturedHitFx, shakeTarget.transform.position);
                                if (hitFx != null && shakeTarget.card_sprite != null)
                                {
                                    int order = shakeTarget.card_sprite.sortingOrder + 10;
                                    string layer = shakeTarget.card_sprite.sortingLayerName;
                                    foreach (Renderer r in hitFx.GetComponentsInChildren<Renderer>(true))
                                    {
                                        r.sortingLayerName = layer;
                                        r.sortingOrder = order;
                                    }
                                }
                            }

                            if (doHardShake)
                            {
                                // Shake agresivo (DESTROZAR / GOLPEAR): 7 pasos, amplitud 0.18 con leve vertical
                                Vector3 tp = shakeTarget.transform.position;
                                AnimFX shakeAnim = AnimFX.Create(shakeTarget.gameObject);
                                shakeAnim.MoveTo(tp + Vector3.right * 0.18f,                       0.04f);
                                shakeAnim.MoveTo(tp - Vector3.right * 0.18f,                       0.04f);
                                shakeAnim.MoveTo(tp + Vector3.right * 0.13f + Vector3.up * 0.05f,  0.04f);
                                shakeAnim.MoveTo(tp - Vector3.right * 0.12f,                       0.04f);
                                shakeAnim.MoveTo(tp + Vector3.right * 0.07f,                       0.03f);
                                shakeAnim.MoveTo(tp - Vector3.right * 0.05f,                       0.03f);
                                shakeAnim.MoveTo(tp,                                               0.04f);
                            }
                        });
                    }

                    //Equip FX
                    Card ecard = bcard.GetEquipCard();
                    if (ecard != null)
                    {
                        FXTool.DoFX(ecard.CardData.attack_fx, transform.position);
                        AudioTool.Get().PlaySFX("card_attack_equip", ecard.CardData.attack_audio);
                    }
                }
            }

        }

        private void OnAttackPlayer(Card attacker, Player player)
        {
            if (attacker == null || player == null)
                return;

            Card card = bcard.GetCard();
            if (card.uid == attacker.uid)
            {
                bool is_other = player.player_id != GameClient.Get().GetPlayerID();
                CardData icard = bcard.GetCardData();
                BoardSlotPlayer zone = BoardSlotPlayer.Get(is_other);

                ChargeIntoPlayer(zone);

                AudioClip audio = icard?.attack_audio != null ? icard.attack_audio : AssetData.Get().card_attack_audio;
                AudioTool.Get().PlaySFX("card_attack", audio);

                //Equip FX
                Card ecard = bcard.GetEquipCard();
                if (ecard != null)
                {
                    FXTool.DoFX(ecard.CardData.attack_fx, transform.position);
                    AudioTool.Get().PlaySFX("card_attack_equip", ecard.CardData.attack_audio);
                }
            }
        }

        private void DamageFX(Transform target, int value, float delay = 0.5f)
        {
            TimeTool.WaitFor(delay, () =>
            {
                GameObject fx = FXTool.DoFX(AssetData.Get().damage_fx, target.position);
                fx.GetComponent<DamageFX>().SetValue(value);
            });
        }

        private void ChargeInto(BoardCard target, bool suppressDamageFX = false)
        {
            if (target != null)
            {
                ChargeInto(target.gameObject);

                CardData icard = target.GetCardData();
                bool targetHasShell = target.GetCard()?.HasStatus(StatusType.Shell) == true;
                BoardCardFX targetFX = target.GetComponent<BoardCardFX>();
                GameObject shellFXPrefab = targetFX != null ? targetFX.shell_break_fx : null;

                // INTOXICAR: if target has this ability, the attacker (self) gets poisoned on impact
                bool targetHasIntoxicar = target.GetCard()?.abilities.Contains("wild_intoxicar") == true;
                AbilityData intoxicarAbility = targetHasIntoxicar ? AbilityData.Get("wild_intoxicar") : null;

                TimeTool.WaitFor(0.25f, () =>
                {
                    if (targetHasShell)
                    {
                        // Shell absorbs hit — play break FX synchronized with impact
                        if (shellFXPrefab != null)
                        {
                            GameObject fx = FXTool.DoFX(shellFXPrefab, target.transform.position);
                            if (fx != null && target.card_sprite != null)
                            {
                                int order = target.card_sprite.sortingOrder + 10;
                                string layer = target.card_sprite.sortingLayerName;
                                foreach (Renderer r in fx.GetComponentsInChildren<Renderer>(true))
                                {
                                    r.sortingLayerName = layer;
                                    r.sortingOrder = order;
                                }
                            }
                        }
                        return;
                    }

                    if (targetHasIntoxicar)
                        return; // No HitFX on INTOXICAR card — poison FX fires on attacker return

                    // Suppress the generic card_damage_fx when the attacker's ability system
                    // will show its own hit FX on this target (e.g. DESTROZAR, GOLPEAR, VOLAR…)
                    if (suppressDamageFX)
                        return;

                    //Damage fx and audio
                    GameObject prefab = icard.damage_fx ? icard.damage_fx : AssetData.Get().card_damage_fx;
                    AudioClip audio = icard.damage_audio ? icard.damage_audio : AssetData.Get().card_damage_audio;
                    FXTool.DoFX(prefab, target.transform.position);
                    AudioTool.Get().PlaySFX("card_hit", audio);
                });

                // INTOXICAR: poison FX + damage number popup on attacker when they return (~0.75s)
                if (targetHasIntoxicar && intoxicarAbility != null)
                {
                    TimeTool.WaitFor(0.75f, () =>
                    {
                        // Guard: attacker may have been destroyed by INTOXICAR before the delay expires.
                        if (bcard == null || bcard.gameObject == null) return;

                        // Poison visual
                        if (intoxicarAbility.target_fx != null)
                        {
                            GameObject fx = FXTool.DoFX(intoxicarAbility.target_fx, bcard.transform.position);
                            if (fx != null && bcard.card_sprite != null)
                            {
                                int order = bcard.card_sprite.sortingOrder + 10;
                                string layer = bcard.card_sprite.sortingLayerName;
                                foreach (Renderer r in fx.GetComponentsInChildren<Renderer>(true))
                                {
                                    r.sortingLayerName = layer;
                                    r.sortingOrder = order;
                                }
                            }
                        }

                        // Damage number popup — shown here instead of at attack time.
                        // Also calls BoardCard.ShowDamageFX so the HP icon number animates too.
                        if (saved_counter_damage_value > 0)
                        {
                            bcard.ShowDamageFX(saved_counter_damage_value);
                            saved_counter_damage_value = 0;
                        }
                    });
                }
            }
        }

        private void ChargeIntoPlayer(BoardSlotPlayer target)
        {
            if (target != null)
            {
                ChargeInto(target.gameObject);

                TimeTool.WaitFor(0.25f, () =>
                {
                    //Damage fx and audio
                    FXTool.DoFX(AssetData.Get().player_damage_fx, target.transform.position);
                    AudioClip audio = AssetData.Get().player_damage_audio;
                    AudioTool.Get().PlaySFX("card_hit", audio);
                });
            }
        }

        private void ChargeInto(GameObject target)
        {
            if (target != null)
            {
                int current_order = bcard.card_sprite.sortingOrder;
                Vector3 dir = target.transform.position - transform.position;
                Vector3 target_pos = target.transform.position - dir.normalized * 1f;
                Vector3 current_pos = transform.position;
                bcard.SetOrder(current_order + 10);

                AnimFX anim = AnimFX.Create(gameObject);
                anim.MoveTo(current_pos - dir.normalized * 0.5f, 0.3f);
                anim.MoveTo(target.transform.position, 0.1f);
                anim.MoveTo(current_pos, 0.3f);
                anim.Callback(0f, () =>
                {
                    if (bcard != null)
                        bcard.SetOrder(current_order);
                });
            }
        }

        private void OnAbilityStart(AbilityData iability, Card caster)
        {
            if (iability != null && caster != null)
            {
                // Counter-attack abilities (OnBeforeDefend + AbilityTriggerer) damage the attacker.
                // This event fires synchronously BEFORE onCardDamaged in the same call chain,
                // so setting the flag here guarantees it is read before OnCardDamaged runs.
                bool isCounterAttack = iability.trigger == AbilityTrigger.OnBeforeDefend
                                       && iability.target == AbilityTarget.AbilityTriggerer;
                if (isCounterAttack)
                    next_damage_is_counter_attack = true;

                if (caster.uid == bcard.GetCardUID())
                {
                    FXTool.DoSnapFX(iability.caster_fx, bcard.transform);
                    AudioTool.Get().PlaySFX("ability", iability.cast_audio);
                }
            }
        }

        private void OnAbilityAfter(AbilityData iability, Card caster)
        {
            if (iability != null && caster != null)
            {
                if (caster.uid == bcard.GetCardUID())
                {

                }
            }
        }

        private void OnAbilityEffect(AbilityData iability, Card caster, Card target)
        {
            if (iability != null && caster != null && target != null)
            {
                if (target.uid == bcard.GetCardUID())
                {
                    Card myCard = bcard.GetCard();
                    bool shouldShowFX = true;
                    bool isStartOfTurnSelfHeal = false;

                    // Counter-attack abilities (e.g. INTOXICAR): FX is handled in ChargeInto,
                    // damage number shown at 0.75s with poison effect — skip ability FX here.
                    // NOTE: this check is OUTSIDE the myCard null-guard — it must always suppress,
                    // even if GetCard() hasn't resolved yet.
                    bool isCounterAttack = iability.trigger == AbilityTrigger.OnBeforeDefend
                                           && iability.target == AbilityTarget.AbilityTriggerer;
                    if (isCounterAttack)
                        shouldShowFX = false;

                    if (myCard != null && shouldShowFX)
                    {
                        // StartOfTurn self-heal abilities (e.g. VOLAR): only show FX when the
                        // card is face-up AND has actual HP to recover — never on face-down cards
                        // and never when already at full HP.
                        isStartOfTurnSelfHeal = iability.trigger == AbilityTrigger.StartOfTurn
                                                && iability.target == AbilityTarget.Self;
                        if (isStartOfTurnSelfHeal)
                            shouldShowFX = myCard.revealed && myCard.damage > 0;
                    }

                    if (shouldShowFX)
                    {
                        GameObject fxGo = FXTool.DoSnapFX(iability.target_fx, bcard.transform);
                        FXTool.DoProjectileFX(iability.projectile_fx, GetFXSource(caster), bcard.transform, iability.GetDamage());
                        AudioTool.Get().PlaySFX("ability_effect", iability.target_audio);

                        // VOLAR heal: mueve el FX al frente de la carta y añade feedback visual extra.
                        // 1) Sorting order: todos los Renderers del FX por encima del sprite de la carta.
                        // 2) Pulso de escala: la carta "late" brevemente (×1.12 → ×1.0 en 0.37s).
                        // 3) Flash verde sobre el sprite de la carta durante 0.4s.
                        if (isStartOfTurnSelfHeal)
                        {
                            // Push FX renderers in front of the card sprite
                            if (fxGo != null && bcard.card_sprite != null)
                            {
                                int order = bcard.card_sprite.sortingOrder + 10;
                                string layer = bcard.card_sprite.sortingLayerName;
                                foreach (Renderer r in fxGo.GetComponentsInChildren<Renderer>(true))
                                {
                                    r.sortingLayerName = layer;
                                    r.sortingOrder = order;
                                }
                            }

                            // Heartbeat scale pulse
                            AnimFX healAnim = AnimFX.Create(bcard.gameObject);
                            healAnim.ScaleTo(1.12f, 0.15f);
                            healAnim.ScaleTo(1.0f, 0.22f);

                            // Green tint flash on the card sprite
                            SpriteRenderer sprite = bcard.card_sprite;
                            if (sprite != null)
                            {
                                Color original = sprite.color;
                                sprite.color = new Color(0.45f, 1.0f, 0.50f, 1f);
                                TimeTool.WaitFor(0.40f, () =>
                                {
                                    if (sprite != null) sprite.color = original;
                                });
                            }
                        }

                        // EMBESTIR: sacudida de "aturdido" + flash inicial solo si no hay Orb asignado
                        // Si paralysis_lightning_prefab está asignado, el Orb ya tiene sus propias
                        // partículas de impacto — el ElectricFX extra sería redundante.
                        if (iability.id == "wild_embestir")
                        {
                            // Flash inicial solo cuando no hay Orb (evita doble chispa)
                            if (paralysis_lightning_prefab == null)
                            {
                                if (fxGo != null && bcard.card_sprite != null)
                                {
                                    int order = bcard.card_sprite.sortingOrder + 10;
                                    string layer = bcard.card_sprite.sortingLayerName;
                                    foreach (Renderer r in fxGo.GetComponentsInChildren<Renderer>(true))
                                    {
                                        r.sortingLayerName = layer;
                                        r.sortingOrder = order;
                                    }
                                }
                            }

                            // Sacudida de "aturdido": amplitud decreciente para sensación de atontamiento
                            TimeTool.WaitFor(0.2f, () =>
                            {
                                if (bcard != null && bcard.gameObject != null)
                                {
                                    Vector3 tp = bcard.transform.position;
                                    AnimFX shakeAnim = AnimFX.Create(bcard.gameObject);
                                    shakeAnim.MoveTo(tp + Vector3.right * 0.12f, 0.05f);
                                    shakeAnim.MoveTo(tp - Vector3.right * 0.12f, 0.05f);
                                    shakeAnim.MoveTo(tp + Vector3.right * 0.08f, 0.05f);
                                    shakeAnim.MoveTo(tp - Vector3.right * 0.06f, 0.05f);
                                    shakeAnim.MoveTo(tp, 0.06f);
                                }
                            });
                        }
                    }
                }

                if (caster.uid == bcard.GetCardUID())
                {
                    if (iability.charge_target && caster.CardData.IsBoardCard())
                    {
                        BoardCard btarget = BoardCard.Get(target.uid);
                        ChargeInto(btarget);
                    }
                }
            }
        }

        // ── Parálisis eléctrica: métodos de gestión ─────────────────────────────────

        /// <summary>
        /// Llama cuando la carta entra en estado Paralizado (y está revelada).
        /// Si hay un prefab de LightningBolt asignado, lo instancia como hijo persistente
        /// y lo configura para que el rayo cruce la carta en diagonal.
        /// Si no hay prefab asignado, usa el fallback de chispas sueltas cada 0.75s.
        /// También aplica un tint azul-gris para transmitir "desactivado".
        /// </summary>
        private void StartParalysedFX()
        {
            // El color desaturado lo gestiona BoardCard.Update() cada frame — no hace falta aquí.

            if (paralysis_lightning_prefab != null && paralysis_lightning_go == null)
            {
                // ── Efecto persistente de parálisis (funciona con cualquier tipo de prefab) ──
                paralysis_lightning_go = Instantiate(paralysis_lightning_prefab, bcard.transform);
                paralysis_lightning_go.transform.localPosition = Vector3.zero;
                paralysis_lightning_go.transform.localScale    = Vector3.one * 0.3f;

                // Soporte para LightningBoltScript: reposicionar los puntos extremos
                Transform startTf = paralysis_lightning_go.transform.Find("LightningStart");
                Transform endTf   = paralysis_lightning_go.transform.Find("LightningEnd");
                if (startTf != null) startTf.localPosition = new Vector3(-0.38f,  0.50f, -0.15f);
                if (endTf   != null) endTf.localPosition   = new Vector3( 0.38f, -0.50f, -0.15f);

                // Push ALL renderers al frente de la carta (funciona con ParticleSystem Y LineRenderer)
                if (bcard.card_sprite != null)
                {
                    int order = bcard.card_sprite.sortingOrder + 10;
                    string layer = bcard.card_sprite.sortingLayerName;
                    foreach (Renderer r in paralysis_lightning_go.GetComponentsInChildren<Renderer>(true))
                    {
                        r.sortingLayerName = layer;
                        r.sortingOrder     = order;
                    }
                }
            }
            else if (paralysis_lightning_prefab == null)
            {
                // Fallback: chispas de partículas cada 0.75s
                if (paralysis_spark_coroutine != null)
                    StopCoroutine(paralysis_spark_coroutine);
                paralysis_spark_coroutine = StartCoroutine(ParalysisSparks());
            }
        }

        /// <summary>
        /// Llama cuando la carta pierde el estado Paralizado.
        /// Destruye el bolt persistente (o detiene el coroutine fallback) y restaura el color.
        /// </summary>
        private void StopParalysedFX()
        {
            // El color se restaura automáticamente en BoardCard.Update() al perder el estado Paralysed.

            if (paralysis_lightning_go != null)
            {
                Destroy(paralysis_lightning_go);
                paralysis_lightning_go = null;
            }

            if (paralysis_spark_coroutine != null)
            {
                StopCoroutine(paralysis_spark_coroutine);
                paralysis_spark_coroutine = null;
            }
        }

        /// <summary>
        /// Fallback: dispara ElectricFX en la carta cada 0.75 s mientras esté paralizada.
        /// Solo se usa cuando paralysis_lightning_prefab no está asignado.
        /// </summary>
        private IEnumerator ParalysisSparks()
        {
            AbilityData embestirData = AbilityData.Get("wild_embestir");
            GameObject electricPrefab = embestirData?.target_fx;

            while (true)
            {
                yield return new WaitForSeconds(0.75f);

                if (electricPrefab == null || bcard == null || bcard.gameObject == null)
                    continue;

                GameObject fx = FXTool.DoSnapFX(electricPrefab, bcard.transform);
                if (fx != null && bcard.card_sprite != null)
                {
                    int order = bcard.card_sprite.sortingOrder + 10;
                    string layer = bcard.card_sprite.sortingLayerName;
                    foreach (Renderer r in fx.GetComponentsInChildren<Renderer>(true))
                    {
                        r.sortingLayerName = layer;
                        r.sortingOrder = order;
                    }
                }
            }
        }

        private Transform GetFXSource(Card caster)
        {
            if (caster.CardData.IsBoardCard())
            {
                BoardCard bcard = BoardCard.Get(caster.uid);
                if (bcard != null)
                    return bcard.transform;
            }
            else
            {
                BoardSlotPlayer slot = BoardSlotPlayer.Get(caster.player_id);
                if (slot != null)
                    return slot.transform;
            }
            return null;
        }
    }
}
