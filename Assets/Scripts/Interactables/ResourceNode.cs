// Assets/Scripts/Interactables/ResourceNode.cs
using Assets.Scripts.Audio;
using Assets.Scripts.Effects;
using Assets.Scripts.InventorySystem;
using Assets.Scripts.Items;
using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.Interactables
{
    // [RequireComponent(typeof(Collider))]
    public class ResourceNode : MonoBehaviour, IInteractable, IImpactSoundProvider
    {
        // === Типы взаимодействия ===
        [Header("Gather - Сбор руками по E; Harvest - Добыча инструментом по ЛКМ")]
        public bool allowGather = true;   // Сбор руками по E
        public bool allowHarvest = true;  // Добыча инструментом по ЛКМ

        [Header("Allowed Tools for Harvest (only matters if allowHarvest = true)")]
        public bool allowFists = true;
        public bool allowAxe = true;
        public bool allowPickaxe = true;
        public bool allowSickle = false;
        // private float destroyTime = 0.5f;
        public bool pitchResourceOnLastHit = false;

        // === Ресурсы ===
        [System.Serializable]
        public struct ResourceDrop
        {
            public Item item;
            public int totalAmount; // Общее количество при полной добыче/сборе
        }

        [Header("Resource Drops")]
        public ResourceDrop[] drops;

        [Header("Limits")]
        public int maxGatherActions = 3; // По умолчанию 3, не 0
        public int maxHarvestHits = 3;   // По умолчанию 3, не 0

        [Header("Visual Feedback (Optional)")]
        public Animator animator;
        public ParticleSystem breakEffect;
        // public int breakEffectScale = 1;
        public bool destroyAfterDepleted = true;

        public bool enableBreakDarkeningEffect;

        public Shatterer shatterer;
        public HitDecaler hitDecaler;

        [SerializeField] private ImpactType _impactType = ImpactType.Wood;
        public ImpactType GetImpactType() => _impactType;

        [Header("Final Destruction Audio")]
        [Tooltip("Звук(и), воспроизводимые при полном разрушении ресурса")]
        public AudioClip[] finalDestructionClips;

        [Tooltip("Двухчастный звук: сначала разрушение, затем удар о землю (для деревьев)")]
        public bool isTwoPartDestruction;

        [Tooltip("Звук удара о землю (для двухчастных звуков)")]
        public AudioClip groundImpactClip;

        [Range(0f, 2f)]
        [Tooltip("Задержка перед звуком удара о землю")]
        public float groundImpactDelay = 0.5f;

        [Range(0f, 1f)]
        public float finalSoundVolume = 1f;

        // === Внутреннее состояние ===
        private int _gatherCount = 0;
        private int _harvestHits = 0;
        private int[] _remainingAmounts;
        private bool _isDepleted = false;

        public ChestInventory GetInventory() => null;

        void Awake()
        {
            // Если shatterer не назначен в инспекторе, ищем его на том же GameObject
            if (shatterer == null)
            {
                shatterer = GetComponent<Shatterer>();
            }
            // Если hitDecaler не назначен в инспекторе, ищем его на том же GameObject
            if (hitDecaler == null)
            {
                hitDecaler = GetComponent<HitDecaler>();
            }
            if (drops.Length > 0)
            {
                _remainingAmounts = new int[drops.Length];
                for (int i = 0; i < drops.Length; i++)
                {
                    _remainingAmounts[i] = drops[i].totalAmount;
                }
            }

            // Защита от 0 действий
            if (maxGatherActions <= 0) maxGatherActions = 1;
            if (maxHarvestHits <= 0) maxHarvestHits = 1;


            if (shatterer != null)
            {
                shatterer.SetPitchOnLastHit(pitchResourceOnLastHit);
            }

        }

        public InteractType GetInteractType()
        {
            return allowGather ? InteractType.Gather : InteractType.Harvest;
        }

        public bool HasInventory() => false;
        public bool ShouldDetachAfterInteract() => _isDepleted;

        public void Interact(InteractContext context)
        {
            if (_isDepleted) return;

            Vector3 targetHitPosition = context.PlayerInteraction.GetTargetHitPosition();
            Vector3 targetHitNormal = context.PlayerInteraction.GetTargetHitNormal();

            if (context.IsAttack)
            {
                if (!allowHarvest) return;

                AttackAnimationType tool = context.Tool;
                bool toolAllowed = tool switch
                {
                    AttackAnimationType.Fists => allowFists,
                    AttackAnimationType.Axe => allowAxe,
                    AttackAnimationType.Pickaxe => allowPickaxe,
                    AttackAnimationType.Sickle => allowSickle,
                    _ => false
                };

                // Вызываем эффект удара (щепки, искры) и если разрешено оружие, эффект осколков
                PlayBreakEffect(targetHitPosition, targetHitNormal, toolAllowed);

                if (!toolAllowed)
                {
                    Debug.Log($"Нельзя добывать этим инструментом: {tool}");
                    return;
                }

                if (_harvestHits >= maxHarvestHits) return;
                _harvestHits++;

                DistributeResources(isHarvest: true, tool: tool, targetHitPosition);

                PlayHitFeedback();

                // ❌ Deplete() вызывается внутри DistributeResources
            }
            else
            {
                if (!allowGather) return;
                if (_gatherCount >= maxGatherActions) return;

                _gatherCount++;
                DistributeResources(isHarvest: false, tool: AttackAnimationType.Fists, targetHitPosition);
                // ❌ Deplete() вызывается внутри DistributeResources
            }
        }

        void DistributeResources(bool isHarvest, AttackAnimationType tool, Vector3 targetHitPosition)
        {
            int currentAction = isHarvest ? _harvestHits : _gatherCount;
            int maxActions = isHarvest ? maxHarvestHits : maxGatherActions;

            // bool gaveSomething = false;

            for (int i = 0; i < drops.Length; i++)
            {
                if (drops[i].item == null || _remainingAmounts[i] <= 0) continue;

                int remaining = _remainingAmounts[i];
                int actionsLeftIncludingThis = maxActions - currentAction + 1; // включая текущий

                // Среднее количество на оставшиеся действия
                int avg = Mathf.Max(1, Mathf.CeilToInt((float)remaining / actionsLeftIncludingThis));
                // Максимум — не больше чем на 50% выше среднего
                int maxPossibleNow = Mathf.Min(remaining, (int)(avg * 1.5f));
                int minPossibleNow = Mathf.Min(1, remaining);

                if (maxPossibleNow < minPossibleNow) maxPossibleNow = minPossibleNow;

                int give = Random.Range(minPossibleNow, maxPossibleNow + 1);
                // give = 1; //for tests
                // give = remaining; //for tests
                // give = avg; //for tests

                if (give > remaining) give = remaining;

                if (isHarvest)
                {
                    give = ApplyToolBonus(drops[i].item, give, tool);
                }

                if (give > 0)
                {
                    _remainingAmounts[i] -= give;
                    GiveResource(drops[i].item, give);
                }
            }

            // Проверка: всё ли собрано ИЛИ достигнут лимит действий
            bool allDepleted = true;
            foreach (int amount in _remainingAmounts)
            {
                if (amount > 0)
                {
                    allDepleted = false;
                    break;
                }
            }

            int currentActions = isHarvest ? _harvestHits : _gatherCount;
            if (allDepleted || currentActions >= maxActions)
            {
                Deplete(targetHitPosition);
            }
        }

        int ApplyToolBonus(Item item, int baseAmount, AttackAnimationType tool)
        {
            float mult = 1f;
            string name = item.itemName.ToLower();

            if (name.Contains("wood") || name.Contains("дерево") || name.Contains("древесина"))
            {
                if (tool == AttackAnimationType.Axe)
                    mult = 2.0f;
                else if (tool == AttackAnimationType.Pickaxe)
                    mult = 1.0f;
                else if (tool == AttackAnimationType.Fists)
                    mult = 0.3f;
            }
            else if (name.Contains("thatch") || name.Contains("солома"))
            {
                if (tool == AttackAnimationType.Axe)
                    mult = 0.5f;
                else if (tool == AttackAnimationType.Pickaxe)
                    mult = 2.0f;
                else if (tool == AttackAnimationType.Fists)
                    mult = 1.0f;
            }
            else if (name.Contains("stone") || name.Contains("камень"))
            {
                if (tool == AttackAnimationType.Axe)
                    mult = 2.0f;
                else if (tool == AttackAnimationType.Pickaxe)
                    mult = 1.0f;
            }
            else if (name.Contains("flint") || name.Contains("кремень"))
            {
                if (tool == AttackAnimationType.Axe)
                    mult = 0.5f;
                else if (tool == AttackAnimationType.Pickaxe)
                    mult = 2.0f;
            }
            else if (name.Contains("metal") || name.Contains("металл"))
            {
                if (tool == AttackAnimationType.Axe)
                    mult = 0.5f;
                else if (tool == AttackAnimationType.Pickaxe)
                    mult = 2.0f;
            }

            return Mathf.Max(0, Mathf.RoundToInt(baseAmount * mult));

        }

        void GiveResource(Item item, int amount)
        {
            if (amount <= 0 || item == null) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            player?.GetComponent<ItemHandler>()?.PickupItem(item, amount);
        }

        void PlayHitFeedback()
        {
            if (animator != null)
                animator.SetTrigger("Hit");
        }

        // В момент ударов
        void PlayBreakEffect(Vector3 targetHitPosition, Vector3 targetHitNormal, bool isBreakable)
        {
            if (animator != null)
                animator.SetTrigger("Break");

            if (breakEffect != null)
            {
                // Отделяем от родителя, чтобы не удалился вместе с ним
                var effectInstance = Instantiate(breakEffect, targetHitPosition, transform.rotation);

                // Mesh mesh = transform.gameObject.GetComponent<MeshCollider>().sharedMesh;
                // var shape = effectInstance.shape; // Get module
                // shape.shapeType = ParticleSystemShapeType.Mesh; 
                // shape.mesh = mesh; 
                // Vector3 resourceModelScale = transform.localScale;
                // effectInstance.transform.localScale = resourceModelScale;

                effectInstance.Play();

                // Уничтожим частицы после завершения
                var main = effectInstance.main;
                Destroy(effectInstance.gameObject, main.duration + 1.5f); //  + 1.5f
            }

            if (isBreakable)
            {
                // вызов физических осколков
                shatterer?.Shatter();

                hitDecaler?.SpawnHitDecal(targetHitPosition, targetHitNormal);

                // потомнение в процессе разрушения
                if (enableBreakDarkeningEffect)
                    PlayBreakDarkeningEffect();
            }

        }

        // потомнение материала в процессе разрушения
        void PlayBreakDarkeningEffect()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer == null)
                renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Color currentColor = renderer.material.color;

                float destructionСolorStep = 0.02f;
                float destructionСolorFinal = 0.5f;
                float destructionR = currentColor.r - destructionСolorStep;
                float destructionG = currentColor.g - destructionСolorStep;
                float destructionB = currentColor.b - destructionСolorStep;

                if (destructionR < destructionСolorFinal) destructionR = destructionСolorFinal;
                if (destructionG < destructionСolorFinal) destructionG = destructionСolorFinal;
                if (destructionB < destructionСolorFinal) destructionB = destructionСolorFinal;

                Color newColor = new(destructionR, destructionG, destructionB);
                renderer.material.color = newColor;
            }
        }



        // На последнем ударе
        void PlayLastBreakEffect(Vector3 targetHitPosition)
        {
            if (animator != null)
                animator.SetTrigger("BreakLast");

            if (breakEffect != null)
            {
                // Отделяем от родителя, чтобы не удалился вместе с ним
                // var effectInstance = Instantiate(breakEffect, transform.position, transform.rotation);
                var effectInstance = Instantiate(breakEffect, targetHitPosition, transform.rotation);
                effectInstance.Play();

                // Уничтожим частицы после завершения
                var main = effectInstance.main;
                Destroy(effectInstance.gameObject, main.duration + 1.5f);
            }

            // вызов физических осколков
            shatterer?.LastBreak();
            shatterer?.Shatter();
            // Invoke(nameof(shatterer.Shatter), destroyTime);
        }


        void Deplete(Vector3 targetHitPosition)
        {
            if (_isDepleted) return;
            _isDepleted = true;

            // 🎵 Воспроизводим финальный звук разрушения
            PlayFinalDestructionSound(targetHitPosition);
            PlayLastBreakEffect(targetHitPosition);

            if (destroyAfterDepleted)
            {
                // Destroy(gameObject, destroyTime);
                Destroy(gameObject);
            }
        }



        /// <summary>
        /// Воспроизводит финальный звук разрушения ресурса.
        /// Поддерживает двухчастные звуки (например, дерево: ломание → падение).
        /// </summary>
        private void PlayFinalDestructionSound(Vector3 position)
        {
            if (finalDestructionClips == null || finalDestructionClips.Length == 0)
                return;

            // Выбираем случайный клип из массива
            AudioClip clip = finalDestructionClips[Random.Range(0, finalDestructionClips.Length)];
            PlaySpatialSound(clip, position, finalSoundVolume);

            // Двухчастный звук: запуск задержанного удара о землю
            if (isTwoPartDestruction && groundImpactClip != null)
            {
                GameObject soundManager = new GameObject("FinalSoundManager");
                soundManager.transform.position = position;
                var delayedSound = soundManager.AddComponent<DelayedSoundPlayer>();
                delayedSound.Play(groundImpactClip, position, finalSoundVolume * 0.8f, groundImpactDelay);
            }
        }

        /// <summary>
        /// Вспомогательный метод для воспроизведения 3D-звука с учётом глобальной громкости.
        /// </summary>
        private void PlaySpatialSound(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null) return;

            float globalVolume = AudioManager.Instance?.masterVolume ?? 1f;
            float finalVolume = globalVolume * volume;

            GameObject soundObj = new GameObject($"FinalSound_{clip.name}");
            soundObj.transform.position = position;

            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = finalVolume;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.maxDistance = 50f;
            source.Play();

            Destroy(soundObj, clip.length + 0.5f);
        }




    }
}