// Assets/Scripts/UI/CloseOnOutsideClick.cs
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI
{
    public class CloseOnOutsideClick : MonoBehaviour, IPointerClickHandler
    {
        public ContextMenuManager contextMenu;

        bool IsChildOf(GameObject child, GameObject parent)
        {
            while (child != null)
            {
                if (child == parent) return true;
                child = child.transform.parent?.gameObject;
            }
            return false;
        }


        public void OnPointerClick(PointerEventData eventData)
        {
            if (contextMenu == null || !contextMenu.contextMenuCanvas.gameObject.activeSelf)
            {
                Debug.LogError("[CloseOnOutsideClick] OnPointerClick contextMenu is null!");
                return;

            }
                
            // Получаем объект, по которому кликнули
            GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;

            // Если кликнули вне меню — закрыть
            if (clickedObject == null || !IsChildOf(clickedObject, contextMenu.contentPanel.gameObject))
            {
                contextMenu.Hide();
            }
        }





    }
}