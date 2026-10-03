using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

/// <summary>Binding sống suốt game (RootScope). Gắn trên prefab ProjectScope cùng ContainerScope.</summary>
public class ProjectInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private LevelDatabaseSO levelDatabase;

    public void InstallBindings(ContainerBuilder builder)
    {
        builder.RegisterType(typeof(PlayerPrefsSaveService), new[] { typeof(ISaveService) }, Lifetime.Singleton, Resolution.Lazy);

        if (levelDatabase != null)
        {
            builder.RegisterValue(levelDatabase);
            builder.RegisterType(typeof(LevelService), Lifetime.Singleton, Resolution.Lazy);
        }
        else
        {
            Debug.LogWarning("[ProjectInstaller] Chưa gán LevelDatabaseSO, LevelService sẽ không khả dụng.");
        }
    }
}
