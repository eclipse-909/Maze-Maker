using UnityEngine;

public class MainCamera : MonoBehaviour
{
    Vector3 touchStart;
    Vector3 mousePos;
    public int zoomOutMin;
    public int zoomOutMax;

    // Update is called once per frame
    void Update()
    {
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))//1 for mouse use, 0 for d-pad use
            touchStart = mousePos;
        if (Input.GetMouseButton(0))//1 for mouse use, 0 for d-pad use
            Camera.main.transform.position += touchStart - mousePos;
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - (100 * Input.GetAxis("Mouse ScrollWheel")), zoomOutMin, zoomOutMax);
    }
}