using UnityEngine;

// Static hazards teach collision danger; moving hazards use a predictable PingPong path.
public class Hazard : MonoBehaviour
{
    public enum HazardMode { Static, Moving }
    public HazardMode mode = HazardMode.Static;
    public Vector3 pointA;
    public Vector3 pointB;
    public float moveSpeed = 2f;
    private GameManager gameManager;

    private void Start() { gameManager = FindObjectOfType<GameManager>(); }

    private void Update()
    {
        if (mode == HazardMode.Moving)
        {
            float t = Mathf.PingPong(Time.time * moveSpeed, 1f);
            transform.position = Vector3.Lerp(pointA, pointB, t);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && gameManager != null) gameManager.Lose();
    }
}
