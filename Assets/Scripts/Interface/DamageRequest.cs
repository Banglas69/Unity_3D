using UnityEngine;

[System.Serializable]
public struct DamageRequest
{
    public float amount;
    public GameObject source;
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public Vector3 hitDirection;

    public DamageRequest(
        float amount,
        GameObject source,
        Vector3 hitPoint,
        Vector3 hitNormal,
        Vector3 hitDirection)
    {
        this.amount = amount;
        this.source = source;
        this.hitPoint = hitPoint;
        this.hitNormal = hitNormal;
        this.hitDirection = hitDirection;
    }
}