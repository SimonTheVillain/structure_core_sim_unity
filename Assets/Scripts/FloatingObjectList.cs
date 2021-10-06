using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/FloatingObjectList", order = 1)]
public class FloatingObjectList : ScriptableObject
{
    [System.Serializable]
    public struct FloatingObject
    {
        public GameObject prefab;
        public Vector2 minMaxDist;
        public Vector2 minMaxScale;
        public int relativeProbability;
    }
    //FloatingObject test2;
    public FloatingObject[] objects;
}
