using UnityEngine;

public static class TransformUltites
{
   public static void Clear(this Transform transform)
   {
      for (int i = transform.childCount - 1; i >= 0; i--)
      {
         GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
      }
   }
}
