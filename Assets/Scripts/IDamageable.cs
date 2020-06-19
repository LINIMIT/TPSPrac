public interface IDamageable
{
    //데미지를 받을수있다  IDamageable 상속받는거는  interface로 강제됨
    bool ApplyDamage(DamageMessage damageMessage);
}