using UnityEngine;

public class EffectManager : MonoBehaviour {
    private static EffectManager m_Instance;
    public static EffectManager Instance {
        get {
            if (m_Instance == null) m_Instance = FindObjectOfType<EffectManager>();
            return m_Instance;
        }
    }

    public enum EffectType {
        Common,
        Flesh
    }

    public ParticleSystem commonHitEffectPrefab;        //일반적인
    public ParticleSystem fleshHitEffectPrefab;         //피부나 살 등

    //순서대로 위치, 이펙트가 바라볼 방향, 움직이는 물체에 맞았을 때 지정할 parent, 타입
    public void PlayHitEffect(Vector3 pos, Vector3 normal, Transform parent = null, EffectType effectType = EffectType.Common) {
        var targetPrefab = commonHitEffectPrefab;
        if (effectType == EffectType.Flesh) {
            targetPrefab = fleshHitEffectPrefab;
        }
        var effect = Instantiate(targetPrefab, pos, Quaternion.LookRotation(normal));

        if (parent != null) {
            effect.transform.SetParent(parent);
        }
        effect.Play();
    }
}