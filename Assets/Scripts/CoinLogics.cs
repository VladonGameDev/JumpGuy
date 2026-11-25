using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CoinLogics : MonoBehaviour
{
    public float animSpeed;
    void Start()
    {
        // Вращение вокруг оси Y на 360 градусов с бесконечным повторением
        transform.DORotate(new Vector3(0, 360, 0), animSpeed, RotateMode.FastBeyond360)
                 .SetEase(Ease.Linear) // Линейная анимация для равномерного вращения
                 .SetLoops(-1, LoopType.Restart); // -1 для бесконечного повторения
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins")+1);
            Destroy(gameObject, 0.1f);
        }
    }
}
