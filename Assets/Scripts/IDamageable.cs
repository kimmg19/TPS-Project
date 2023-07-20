//공격 받을 수 있는 모든 물체에 상속. 피격 된 물체가 iDamagable 가지고 있는지 확인.
//만약 있다면 ApplyDamage를 실행
public interface IDamageable {
    //bool로 공격 성공 여부 체크.
    bool ApplyDamage(DamageMessage damageMessage);
}