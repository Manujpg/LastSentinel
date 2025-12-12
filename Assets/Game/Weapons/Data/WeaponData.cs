using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Stats")]
    public float damage = 10f;
    public float fireRate = 4f;      // shots per second
    public float projectileSpeed = 20f;
    public float projectileLifetime = 2f;

    [Header("Laser Settings")]
    public Color projectileColor = Color.cyan;
    public float projectileWidth = 0.1f;

    [Header("Prefabs")]
    public GameObject projectilePrefab;
    public GameObject muzzleFlashPrefab;
}