using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

/// <summary>Binding sống suốt game (RootScope). Gắn trên prefab ProjectScope cùng ContainerScope.</summary>
public class ProjectInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.RegisterType(typeof(PlayerPrefsSaveService), new[] { typeof(ISaveService) }, Lifetime.Singleton, Resolution.Lazy);
    }
}
