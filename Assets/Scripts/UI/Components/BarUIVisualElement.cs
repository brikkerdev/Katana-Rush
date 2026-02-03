using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BarUIVisualElement : MonoBehaviour
{
    protected Image spriteRenderer;

    [SerializeField] protected Material material;

    private float currentValue = 1f;
    private float targetValue = 1f;

    protected virtual void Start()
    {
        spriteRenderer = GetComponent<Image>();
        if (material != null)
        {
            material = Instantiate(material);
            spriteRenderer.material = material;
        }
    }

    public virtual void Init(float maxValue, float value)
    {
        if (material == null) return;

        float normalized = maxValue > 0 ? value / maxValue : 0f;
        currentValue = normalized;
        targetValue = normalized;

        material.SetFloat("_Value", normalized);
        material.SetFloat("_EffectValue", normalized);
    }

    public virtual void SetValue(float maxValue, float value)
    {
        if (material == null) return;

        targetValue = maxValue > 0 ? value / maxValue : 0f;
        material.SetFloat("_Value", targetValue);

        float effectValue = material.GetFloat("_EffectValue");
        if (float.IsNaN(effectValue)) effectValue = targetValue;

        material.SetFloat("_EffectValue", Mathf.Lerp(effectValue, targetValue, Time.deltaTime * 10f));
        currentValue = targetValue;
    }

    public virtual void SetValueAnimated(float maxValue, float value, float duration = 0.3f)
    {
        if (material == null) return;

        targetValue = maxValue > 0 ? value / maxValue : 0f;

        DOTween.To(
            () => material.GetFloat("_Value"),
            x => {
                material.SetFloat("_Value", x);
                material.SetFloat("_EffectValue", x);
            },
            targetValue,
            duration
        ).SetEase(Ease.OutQuad).SetLink(gameObject);

        currentValue = targetValue;
    }

    public virtual void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    public virtual void SetColorAnimated(Color color, float duration = 0.3f)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.DOColor(color, duration).SetLink(gameObject);
        }
    }

    public float GetCurrentValue() => currentValue;
    public float GetTargetValue() => targetValue;

}