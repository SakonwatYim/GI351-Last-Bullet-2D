using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class fillBullet_area : MonoBehaviour
{
    [SerializeField] private Gun gun;
    float AddTime = 0;
    bool inside = false;
    bool outside = false;
    float ammo = 0.1f;//เวลาในการเพิ่มกระสุน
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // Update is called once per frame
    private void Start()
    {
        gun = FindAnyObjectByType<Gun>();
    }
   
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            inside = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        outside = true;
        inside = false;
    }
    private void Update()
    {
        if (inside == true && Time.time >= AddTime)
        {
            gun.AddAmmo(1);
            AddTime = Time.time + ammo;
        }
        if (inside == false && outside == true)
        {
            Destroy(gameObject);
        }
    }
}
