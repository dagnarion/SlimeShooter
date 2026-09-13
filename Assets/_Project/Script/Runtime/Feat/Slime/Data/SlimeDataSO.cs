using UnityEngine;
[CreateAssetMenu(menuName = "Slime/SlimeDataSO")]
public class SlimeDataSO : ScriptableObject
{
    [field:SerializeField] public Color32 Color { get; private set; }
    [field:SerializeField] public int bulletAmount { get; private set; }
}
