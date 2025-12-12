using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private Transform firePoint;

    private float cooldown;

    void Update()
    {
        if (cooldown > 0f)
            cooldown -= Time.deltaTime;

        // Linksklick zum Schießen
        if (Input.GetMouseButton(0))
        {
            TryShoot();
        }
    }

    void TryShoot()
    {
        if (cooldown > 0f) return;
        if (weaponData == null || firePoint == null) return;

        cooldown = 1f / weaponData.fireRate;
        Shoot();
    }

    void Shoot()
    {
        GameObject proj = Instantiate(
            weaponData.projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        // Richtung basiert auf Spieler-Orientierung
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        LaserProjectile laser = proj.GetComponent<LaserProjectile>();
        laser.Fire(direction);
    }


    // Zum späteren Waffenwechsel
    public void SetWeapon(WeaponData newWeapon)
    {
        weaponData = newWeapon;
    }
}
