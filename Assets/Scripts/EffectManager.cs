using UnityEngine;

public class EffectManager : MonoBehaviour
{
    private static EffectManager m_Instance;
    public static EffectManager Instance
    {
        get
        {
            if (m_Instance == null) m_Instance = FindObjectOfType<EffectManager>();
            return m_Instance;
        }
    }

    public enum EffectType
    {
        Common,
        Flesh
    }

    public ParticleSystem commonHitEffectPrefab;//대부분의 경우의 이펙트
    public ParticleSystem fleshHitEffectPrefab;//피가나오는 이펙트

    public void PlayHitEffect(Vector3 pos, Vector3 normal, Transform parent = null, EffectType effectType = EffectType.Common)
    // Vector3 pos = 이펙트를 재생할 위치,Vector3 normal=이펙트가 바라볼 방향, Transform parent=이펙트에게 할당할 부모
    // EffectType effectType = 사용할 이펙트타입
    {
        var targetPrefab = commonHitEffectPrefab;

        if (effectType == EffectType.Flesh)
        {
            targetPrefab = fleshHitEffectPrefab;
        }

        var effect = Instantiate(targetPrefab, pos, Quaternion.LookRotation(normal));

        if (parent != null)
        {
            effect.transform.SetParent(parent);
        }
        effect.Play();
    }
}