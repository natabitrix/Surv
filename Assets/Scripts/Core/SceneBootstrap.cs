using System.Collections;
using UnityEngine;
using Assets.Scripts.Core;
using Assets.Scripts.Corpses;
using Assets.Scripts.Loot;
using Assets.Scripts.UI;

namespace Assets.Scripts.Core
{
    /// <summary>
    /// Инициализация сцены: регистрирует все задачи загрузки и запускает их.
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        private bool _tasksRegistered = false;

        private IEnumerator Start()
        {
            if (_tasksRegistered) yield break;
            _tasksRegistered = true;

            // Ждем PlayerProgress
            while (PlayerProgress.Instance == null)
                yield return null;

            // Ждем PlayerController
            while (PlayerProgress.Instance.playerController == null)
                yield return null;

            yield return null; // Даем другим Start() выполниться

            var lsm = LoadingScreenManager.Instance;
            if (lsm == null)
            {
                Debug.LogError("[SceneBootstrap] LoadingScreenManager не найден!");
                yield break;
            }

            var playerPos = PlayerProgress.Instance.playerController.transform.position;

            // Регистрируем задачи
            if (CorpseManager.Instance != null)
            {
                lsm.RegisterTask("Загрузка трупов...", CorpseManager.Instance.LoadAllCorpsesAsync());
            }

            if (LootBagManager.Instance != null)
            {
                lsm.RegisterTask("Загрузка сумок...", LootBagManager.Instance.LoadAllLootBagsAsync());
            }

            if (WorldManager.Instance != null)
            {
                lsm.RegisterTask("Загрузка мира...", WorldManager.Instance.LoadWorldAsync(playerPos));
            }

            yield return null;

            // Запускаем загрузку
            lsm.StartLoading();
        }
    }
}