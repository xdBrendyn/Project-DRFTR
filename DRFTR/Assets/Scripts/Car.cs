using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Car", menuName = "Resources/Car")]
public class Car : ScriptableObject
{
    public enum Tier
    {
        E,
        D,
        C,
        B,
        A,
        S
    }

    public enum Type {
        Sedan,
        Offroad,
        Sport,
        Super
    }

    public Tier tier;
    public Type type;
    public string modelName;
    [Range(0, 10)] public float zeroToSixty;
    public int topSpeed;
    public GameObject carModel;
}
