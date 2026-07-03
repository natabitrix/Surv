// Assets/Scripts/InventorySystem/InventoryConfig.cs
using UnityEngine;

namespace Assets.Scripts.InventorySystem
{
    public class InventoryConfig : MonoBehaviour
    {
        // Клавиша для разделения стака (по умолчанию LeftShift)
        // Эту клавишу можно менять в инспекторе или через меню настроек
        public KeyCode splitStackKey = KeyCode.LeftShift;

        private static InventoryConfig _instance;
        public static InventoryConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<InventoryConfig>();
                    if (_instance == null)
                    {
                        var go = new GameObject("InventoryConfig");
                        _instance = go.AddComponent<InventoryConfig>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
    }
}