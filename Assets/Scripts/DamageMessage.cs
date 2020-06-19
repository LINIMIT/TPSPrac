using UnityEngine;

public struct DamageMessage//필요에 따라 공격을 받는 입장에서 마음대로 수정이 가능 
{
    public GameObject damager;
    public float amount;

    public Vector3 hitPoint;//공격이 가해진 위치
    public Vector3 hitNormal;// 공격이 가해진 방향
}