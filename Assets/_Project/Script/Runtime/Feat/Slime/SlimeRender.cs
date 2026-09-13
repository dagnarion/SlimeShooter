using System;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;

public class SlimeRender : MonoBehaviour
{
   [SerializeField] MeshRenderer meshRenderer;
   [SerializeField] private TextMeshProUGUI bulletCount;
   private SlimeLocalEvent slimeEvent;
   
   public void Init(Color32 color,int amount,SlimeLocalEvent slimeEvent)
   {
      meshRenderer.material.color = color;
      this.slimeEvent = slimeEvent;
      bulletCount.text = amount.ToString();
      this.slimeEvent.OnShoot += UpdateBulletCount;
   }

   private void OnDisable()
   {
      slimeEvent.OnShoot -= UpdateBulletCount;
   }


   private void UpdateBulletCount(int amount) => bulletCount.text = amount.ToString();
   
   
   
}
