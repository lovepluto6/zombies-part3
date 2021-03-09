using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FPController : MonoBehaviour
{
    public GameObject cam;
    public Animator anim;
    public Slider healthBar;
    public Text ammoReserves;
    public AudioSource[] footsteps;
    public AudioSource jump;
    public AudioSource land;
    public AudioSource ammoPickup;
    public AudioSource healthPickup;
    public AudioSource triggerSound;
    public AudioSource reloadSound;

    public Vector3 velocity;

    float speed = 0.1f;
    float Xsensitivity = 2;
    float Ysensitivity = 2;
    float MinimumX = -90;
    float MaximumX = 90;

    Rigidbody rb;
    CapsuleCollider capsule;
    Quaternion cameraRot;
    Quaternion characterRot;

    bool cursorIsLocked = true;
    bool lockCursor = true;

    float x;
    float z;


    //inventory
    int ammo = 0;
    int maxAmmo = 50;

    int health = 80;
    int maxHealth = 100;

    int ammoClip = 0;
    int ammoCLipMax = 10;

    public void TakeHit(float amount)
    {
        health = (int)Mathf.Clamp(health - amount, 0, maxHealth);
        healthBar.value = health;
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        capsule = this.GetComponent<CapsuleCollider>();
        cameraRot = cam.transform.localRotation;
        characterRot = this.transform.localRotation;

        //health == maxHealth;
        healthBar.value = health;

        ammoReserves.text = ammo +"";
    }


    bool IsGrounded()
    {
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, capsule.radius, Vector3.down, out hitInfo,
                (capsule.height / 2f) - capsule.radius + 0.1f))
        {
            return true;
        }
        return false;
    }
    // Update is called once per frame
    void Update()
    {
        
        if (IsGrounded() && velocity.y < 0)
        {
            velocity.y = -2.0f;
        }

        if (Input.GetKeyDown(KeyCode.F))
            anim.SetBool("arm", !anim.GetBool("arm"));

        if (Input.GetMouseButtonDown(0) && !anim.GetBool("fire"))
        {
            if (ammoClip > 0)
            {
                anim.SetTrigger("fire");
                ammoClip--;
                //shot.Play();
            }
            else if(anim.GetBool("arm"))
            {
                triggerSound.Play();
            }
            Debug.Log("Ammo Left in Clip:" + ammoClip);
        }

        if (Input.GetKeyDown(KeyCode.R) && anim.GetBool("arm"))
        {
            anim.SetTrigger("reload");
            reloadSound.Play();
            int amountNeed = ammoCLipMax - ammoClip;
            int ammoAvailable = amountNeed < ammo ? amountNeed : ammo;
            ammo -= ammoAvailable;
            ammoClip += ammoAvailable;

            ammoReserves.text = ammo + "";
            Debug.Log("Ammo Left:" + ammo);
            Debug.Log("Ammo in CLip:" + ammoClip);

        }
            

        if (Mathf.Abs(x) > 0 || Mathf.Abs(z) > 0)
        {
            if (!anim.GetBool("walking"))
            {
                anim.SetBool("walking", true);
                InvokeRepeating("PlayFootStepAudio", 0, 0.4f);
            }
        }
        else if (anim.GetBool("walking"))
        {
            anim.SetBool("walking", false);
            CancelInvoke("PlayFootStepAudio");
        }

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            rb.AddForce(0, 300, 0);
            jump.Play();
            if (anim.GetBool("walking"))
                CancelInvoke("PlayFootStepAudio");
        }


        if (Input.GetButton("Cancel"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }
    }
    void FixedUpdate()
    {
        float yRot = Input.GetAxis("Mouse X") * Ysensitivity;
        float xRot = Input.GetAxis("Mouse Y") * Xsensitivity;

        cameraRot *= Quaternion.Euler(-xRot, 0, 0);
        characterRot *= Quaternion.Euler(0, yRot, 0);

        cameraRot = ClampRotationAroundXAxis(cameraRot);

        this.transform.localRotation = characterRot;
        cam.transform.localRotation = cameraRot;


        x = Input.GetAxis("Horizontal") * speed;
        z = Input.GetAxis("Vertical") * speed;

        transform.position += cam.transform.forward * z + cam.transform.right * x; //new Vector3(x * speed, 0, z * speed);

        UpdateCursorLock();
    }

    void PlayFootStepAudio()
    {
        AudioSource audioSource = new AudioSource();
        int n = Random.Range(1, footsteps.Length);

        audioSource = footsteps[n];
        audioSource.Play();
        footsteps[n] = footsteps[0];
        footsteps[0] = audioSource;
    }


   

    Quaternion ClampRotationAroundXAxis(Quaternion q)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
        angleX = Mathf.Clamp(angleX, MinimumX, MaximumX);
        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

        return q;
    }

   
    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.tag == "Ammo" && ammo < maxAmmo)
        {
            Destroy(col.gameObject);
            ammoPickup.Play();

            ammo = Mathf.Clamp(ammo+10, 0, maxAmmo);
            //Debug.Log("Ammo:" + ammo);
            ammoReserves.text = ammo + "";
        }

        if (col.gameObject.tag == "MedKit" && health < maxHealth)
        {
            Destroy(col.gameObject);
            healthPickup.Play();

            health = Mathf.Clamp(health + 25, 0, maxHealth);
            //Debug.Log("Health:" + health);
            healthBar.value = health;
        }

        if (col.gameObject.tag == "Enemy" )
        {
            //Destroy(col.gameObject);
            //healthPickup.Play();

            health = Mathf.Clamp(health -5 , 0, maxHealth);
            //Debug.Log("Health:" + health);
            healthBar.value = health;
        }


        if (IsGrounded())
        {
            land.Play();
            if(anim.GetBool("walking"))
                InvokeRepeating("PlayFootStepAudio", 0, 0.2f);
        }
    }

    public void SetCursorLock(bool value)
    {
        lockCursor = value;
        if (!lockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void UpdateCursorLock()
    {
        if (lockCursor)
            InternalLockUpdate();
    }

    public void InternalLockUpdate()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
            cursorIsLocked = false;
        else if ( Input.GetMouseButtonUp(0) )
            cursorIsLocked = true;

        if (cursorIsLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (!cursorIsLocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

     

}
