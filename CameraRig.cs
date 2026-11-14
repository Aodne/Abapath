using System.Collections;
using UnityEngine;

public class CameraRig : MonoBehaviour
{
    public Camera Cam { get; private set; }

    void Awake()
    {
        Cam = Camera.main;
        Cam.orthographic = true;
        Cam.orthographicSize = GameConfig.HUD_HALF_HEIGHT;
        Cam.transform.position = new Vector3(0, 0, -10);
        Cam.clearFlags = CameraClearFlags.SolidColor;
        Cam.backgroundColor = new Color(0.10f, 0.10f, 0.20f);



    }

    void Update()
    {
        float needSize = Mathf.Max(GameConfig.HUD_HALF_HEIGHT, GameConfig.HUD_HALF_WIDTH / Cam.aspect);
        if (!Mathf.Approximately(Cam.orthographicSize, needSize)) Cam.orthographicSize = needSize;
    }

    public void Shake()
    {
        StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        for (float t = 0; t < 0.35f; t += Time.deltaTime)
        {
            float m = 0.35f * (1f - t / 0.35f);
            Cam.transform.position = new Vector3(Random.Range(-m, m), Random.Range(-m, m), -10f);
            yield return null;
        }
        Cam.transform.position = new Vector3(0, 0, -10f);
    }
}
