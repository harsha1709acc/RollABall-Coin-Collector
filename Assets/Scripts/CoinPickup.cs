using UnityEngine;

// Trigger colliders detect collection without pushing the player away from a small coin.
public class CoinPickup : MonoBehaviour
{
    public float rotateSpeed = 90f;
    public float bobHeight = 0.2f;
    public float bobSpeed = 2f;
    private Vector3 startPosition;
    private GameManager gameManager;

    private void Start()
    {
        startPosition = transform.position;
        gameManager = FindObjectOfType<GameManager>();
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (gameManager != null) gameManager.CollectCoin();
        Destroy(gameObject);
    }
}
