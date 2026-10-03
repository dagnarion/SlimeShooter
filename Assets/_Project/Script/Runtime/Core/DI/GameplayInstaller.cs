using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

/// <summary>Binding cho scene gameplay. Gắn cùng GameObject có ContainerScope trong SCN_Gameplay.</summary>
public class GameplayInstaller : MonoBehaviour, IInstaller
{
    #region Config
    [SerializeField] private ConveyorDataSO conveyorData;
    #endregion

    public void InstallBindings(ContainerBuilder builder)
    {
        if (conveyorData != null) builder.RegisterValue(conveyorData);

        builder.RegisterType(typeof(GameSession), Lifetime.Singleton, Resolution.Lazy);
        builder.RegisterType(typeof(TickScheduler), Lifetime.Singleton, Resolution.Lazy);
    }
}
