using UnityEngine;

public class LateUpdateFollow : MonoBehaviour
{
    public Transform targetToFollow;

    private void LateUpdate()//매번  update가 종료되고나서 실행됨
    {
        transform.position = targetToFollow.position;
        transform.rotation = targetToFollow.rotation;
    }
}