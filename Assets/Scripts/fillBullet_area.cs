using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class fillBullet_area : MonoBehaviour
{
    public static event System.Action OnPlayerLeft; // MonsterSpawner listens to this

    [SerializeField] private Gun gun;
    float AddTime = 0;
    bool inside = false;
    bool outside = false;
    float ammo = 0.1f;//����㹡����������ع
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
        if (!collision.gameObject.CompareTag("Player")) return; // bullets leaving the area don't count
        outside = true;
        inside = false;
        OnPlayerLeft?.Invoke();
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
