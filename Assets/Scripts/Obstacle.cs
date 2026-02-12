using UnityEngine;

public class Obstacle : MonoBehaviour
{
    Player_Movement playerMovement;
    private void Start()
    {
        playerMovement = Object.FindFirstObjectByType<Player_Movement>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Player")
        {
            playerMovement.Die();
        }
    }
    // Update is called once per frame
    private void Update()
    {
        
    }
}
