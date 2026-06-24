using Assets.Scripts.Creatures;
using UnityEngine;

[RequireComponent(typeof(BaseLivingEntity))]
public class HealthMaterialBlender : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string blendParameter = "_DamageBlend";
    [SerializeField] private string intensityParameter = "_DamageIntensity";
    
    [Header("Визуал")]
    [Tooltip("Усиление эффекта повреждений (1 = как в текстуре, 2 = вдвое ярче)")]
    [SerializeField] private float damageIntensity = 1.5f;
    
    [Tooltip("Как часто проверять здоровье (0.05 = 20 раз в сек, достаточно)")]
    [SerializeField] private float checkInterval = 0.05f;

    private BaseLivingEntity _entity;
    private MaterialPropertyBlock _block;
    private int _blendID;
    private int _intensityID;
    private float _nextCheckTime;
    private float _lastRatio = -1f;

    private void Awake()
    {
        _entity = GetComponent<BaseLivingEntity>();
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();

        _block = new MaterialPropertyBlock();
        _blendID = Shader.PropertyToID(blendParameter);
        _intensityID = Shader.PropertyToID(intensityParameter);
        
        // Применяем интенсивность один раз при старте
        _block.SetFloat(_intensityID, damageIntensity);
        targetRenderer.SetPropertyBlock(_block);
        
        _nextCheckTime = Time.time;
    }

    private void Update()
    {
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + checkInterval;

        float max = _entity.GetMaxHealth();
        float cur = _entity.GetHealth();
        if (max <= 0f) return;

        float ratio = Mathf.Clamp01(cur / max);

        // Обновляем материал только при реальном изменении здоровья
        if (Mathf.Abs(ratio - _lastRatio) > 0.005f)
        {
            _lastRatio = ratio;
            float blend = 1f - ratio; // 🔥 Прямая линейная зависимость

            _block.SetFloat(_blendID, blend);
            targetRenderer.SetPropertyBlock(_block);
        }
    }
}