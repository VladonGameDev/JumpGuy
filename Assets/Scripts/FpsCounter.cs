using UnityEngine;
using UnityEngine.UI;
public class FPSCounter : MonoBehaviour
{
    public float updateInterval = 0.5f; // Интервал обновления FPS

    private float accum = 0; // Аккумулятор времени
    private int frames = 0; // Количество кадров
    private float timeleft; // Оставшееся время до обновления
    public Text textMesh;

    private void Start()
    {
        Application.targetFrameRate = 999;
        timeleft = updateInterval;
    }

    private void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;

        // Если прошло достаточно времени для обновления
        if (timeleft <= 0.0)
        {
            // Считаем среднее значение FPS за прошедший интервал
            float fps = accum / frames;

            // Выводим результат на текстовое поле TextMesh
            textMesh.text = "FPS: " + fps.ToString("F2");

            // Сбрасываем значения
            timeleft = updateInterval;
            accum = 0;
            frames = 0;
        }
    }
}
