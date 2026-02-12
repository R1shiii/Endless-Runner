using UnityEngine;
using UnityEngine.SceneManagement;

public class Player_Movement : MonoBehaviour {

    bool alive = true;

    public float Speed = 5;
    [SerializeField] Rigidbody rb;

    float horizontalInput;
    [SerializeField] float horizontalMultiplier = 2;

    public float speedIncreasePerPoint = 0.5f;

    [SerializeField] float jumpForce = 400f;
    [SerializeField] LayerMask groundMask; 

    public void FixedUpdate()
    {
        if (!alive) return;

        Vector3 forwardMove = transform.forward * Speed * Time.fixedDeltaTime;
        Vector3 horizontalMove = transform.right * horizontalInput * Speed * Time.fixedDeltaTime * horizontalMultiplier;
        rb.MovePosition(rb.position + forwardMove + horizontalMove);
    }

    public void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jump();
        }

        if (transform.position.y < -5)
        {
            Die();
        }
    }
    public void Die()
    {
        alive = false;
        Invoke("Restart", 2);

    } 
    public void Restart ()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void jump ()
    {
        float height = GetComponent<Collider>().bounds.size.y;
        bool isGrounded = (Physics.Raycast(transform.position, Vector3.down, height / 2 + 0.1f, groundMask));

        if(!isGrounded) return; 

        rb.AddForce(Vector3.up * jumpForce);      
    }
}
