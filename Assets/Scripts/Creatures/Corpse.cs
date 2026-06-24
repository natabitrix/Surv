// Assets/Scripts/Creatures/Corpse.cs
using UnityEngine;
using Assets.Scripts.Interactables;
using Assets.Scripts.Player;
using Assets.Scripts.InventorySystem;

namespace Assets.Scripts.Creatures
{
    public class Corpse : MonoBehaviour, IInteractable
    {
        public bool IsDragging { get; private set; }

        [Header("Drag Target")]
        [Tooltip("Кость, за которую тянем (Torso/Hips). Остальные кости потянутся через суставы рэгдолла.")]
        [SerializeField] private Rigidbody _dragRigidbody;

        [Header("Spring Settings")]
        public float dragDistance = 0f;
        public float springForce = 500f;
        public float springDamper = 0f;
        public float maxSpringDistance = 0.3f;

        // private SpringJoint _joint;
        private CharacterJoint _joint;
        private Transform _anchor;
        private Rigidbody _anchorRb;

        private Creature creature;

        // Кэш оригинального затухания
        private float _origLinDamp;
        private float _origAngDamp;

        public InteractType GetInteractType() => InteractType.Pickup;
        public ChestInventory GetInventory() => null;
        public bool HasInventory() => false;
        public bool ShouldDetachAfterInteract() => false;

        public void Interact(InteractContext context)
        {
            if (IsDragging) StopDragging(context.PlayerInteraction);
            else StartDragging(context.PlayerInteraction);
        }

        private void Awake()
        {
            creature = GetComponent<Creature>();
        }

        public void StartDragging(PlayerInteraction player)
        {
            if (IsDragging || _dragRigidbody == null) return;
            IsDragging = true;

            // 🔥 Сообщаем игроку, что начали тащить ЭТО тело
            player.RegisterDraggingCorpse(this);

            creature.ActivateRagdoll();

            // Якорь следует за точкой хвата игрока
            var equip = player.GetComponent<PlayerEquipment>();
            Transform grabPoint = equip ? equip.corpseDragAnchor : player.transform;
            if (grabPoint == null) grabPoint = player.transform;

            _anchor = new GameObject("CorpseDragAnchor").transform;
            _anchor.SetParent(grabPoint);
            _anchor.localPosition = new Vector3(0, 0, dragDistance);
            _anchorRb = _anchor.gameObject.AddComponent<Rigidbody>();
            _anchorRb.isKinematic = true;

            // Пружинный джоинт вешаем ИМЕННО на кость Torso/Hips
            _joint = _dragRigidbody.gameObject.AddComponent<CharacterJoint>();
            _joint.connectedBody = _anchorRb;
            _joint.enablePreprocessing = true;
            _joint.enableCollision = false;
        }

        public void StopDragging(PlayerInteraction player)
        {
            if (!IsDragging) return;
            IsDragging = false;

            // 🔥 Сообщаем игроку, что отпустили тело
            player.UnregisterDraggingCorpse(this);

            if (_joint != null) { Destroy(_joint); _joint = null; }
            if (_anchor != null) { Destroy(_anchor.gameObject); _anchor = null; }

            // Восстанавливаем затухание, чтобы тело "засыпало" на земле
            if (_dragRigidbody != null)
            {
                _dragRigidbody.linearVelocity *= 0.2f; // Гасим инерцию
                _dragRigidbody.angularVelocity *= 0.2f;
            }

            creature.DeactivateRagdoll();
        }

        public void InterruptByAttack(PlayerInteraction player) => StopDragging(player);
    }
}