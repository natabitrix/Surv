using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Creatures
{
    [CreateAssetMenu(fileName = "CreatureDatabase", menuName = "Game/Creature Database")]
    public class CreatureDatabase : ScriptableObject
    {
        [Tooltip("Список всех существ игры")]
        [SerializeField] private List<CreatureData> _creatures = new();

        private Dictionary<string, CreatureData> _lookup;

        /// <summary>
        /// Словарь для быстрого поиска по creatureId.
        /// Строится лениво при первом обращении.
        /// </summary>
        public Dictionary<string, CreatureData> Lookup
        {
            get
            {
                if (_lookup == null)
                {
                    _lookup = new Dictionary<string, CreatureData>();

                    foreach (var creature in _creatures)
                    {
                        if (creature == null)
                        {
                            Debug.LogWarning("[CreatureDatabase] Найдена пустая запись (null) в списке!");
                            continue;
                        }

                        if (string.IsNullOrEmpty(creature.creatureId))
                        {
                            Debug.LogError($"[CreatureDatabase] У существа '{creature.name}' не задан creatureId!");
                            continue;
                        }

                        if (_lookup.ContainsKey(creature.creatureId))
                        {
                            Debug.LogError($"[CreatureDatabase] Дубликат creatureId: '{creature.creatureId}'!");
                            continue;
                        }

                        _lookup[creature.creatureId] = creature;
                    }
                }

                return _lookup;
            }
        }

        /// <summary>
        /// Получить данные существа по ID.
        /// </summary>
        public CreatureData GetCreature(string creatureId)
        {
            if (string.IsNullOrEmpty(creatureId)) return null;

            return Lookup.TryGetValue(creatureId, out var data) ? data : null;
        }

        /// <summary>
        /// Проверить, существует ли существо с таким ID.
        /// </summary>
        public bool HasCreature(string creatureId)
        {
            return !string.IsNullOrEmpty(creatureId) && Lookup.ContainsKey(creatureId);
        }

        /// <summary>
        /// Получить все данные существ (для редактора).
        /// </summary>
        public IEnumerable<CreatureData> AllCreatures => _creatures;

        // Метод для редактора — очищает кэш при изменении
#if UNITY_EDITOR
        private void OnValidate()
        {
            _lookup = null;
        }
#endif
    }
}