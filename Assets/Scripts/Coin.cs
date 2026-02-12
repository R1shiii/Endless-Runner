using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] float turnSpeed = 90f;

    private void OnTriggerEnter(Collider other)
    {
        // Only react when the player collides
        if (other.GetComponent<Player_Movement>() != null || other.gameObject.name == "Player")
        {
            // Guard against missing GameManager
            if (GameManager.inst != null)
            {
                GameManager.inst.IncrementScore();
            }
            else
            {
                Debug.LogWarning("GameManager.inst is null — make sure a GameManager exists in the scene and is enabled.");
            }

            Destroy(gameObject);
        }
    }

    private void Update()
    {
        transform.Rotate(0, 0, turnSpeed * Time.deltaTime);
    }
}

