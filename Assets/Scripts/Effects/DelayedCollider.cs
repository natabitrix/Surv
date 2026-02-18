// Assets/Scripts/Effects/DelayedCollider.cs
using UnityEngine;
using System.Collections;

public class DelayedCollider : MonoBehaviour
{
    public float delay = 2f;                 // Через сколько секунд добавить коллайдер
    public ColliderType colliderType = ColliderType.Box; // Тип коллайдера (можно расширить)
    public Vector3 center = Vector3.zero;
    public Vector3 size = Vector3.one;       // Используется для Box и Sphere (как радиус)

    public enum ColliderType
    {
        Box,
        Sphere,
        Capsule
        // MeshCollider можно добавить, но он сложнее (требует MeshFilter)
    }

    IEnumerator Start()
    {
        // Ждём заданное время
        yield return new WaitForSeconds(delay);

        // Добавляем нужный коллайдер
        switch (colliderType)
        {
            case ColliderType.Box:
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = center;
                box.size = size;
                break;

            case ColliderType.Sphere:
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.center = center;
                sphere.radius = size.x; // используем x как радиус
                break;

            case ColliderType.Capsule:
                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.center = center;
                capsule.radius = size.x;
                capsule.height = size.y;
                break;
        }
    }
}