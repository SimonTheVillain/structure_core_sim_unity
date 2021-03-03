using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/MaterialList", order = 1)]
public class MaterialList : ScriptableObject
{
    [System.Serializable]
    public struct RandomizedMaterial
    {
        public Material material;
        public Vector2 minMaxScale;
        public int relativeProbability;
    }
    public RandomizedMaterial[] materials;

}
