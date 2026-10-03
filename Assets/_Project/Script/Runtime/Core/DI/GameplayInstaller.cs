using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

/// <summary>Binding cho scene gameplay. Gắn cùng GameObject có ContainerScope trong SCN_Gameplay.</summary>
public class GameplayInstaller : MonoBehaviour, IInstaller
{
    #region Config
    [SerializeField] private ConveyorDataSO conveyorData;
    [SerializeField] private BoardConfigSO boardConfig;
    #endregion

    #region Scene refs
    [Tooltip("Tâm của board trong world (board nằm trên mặt phẳng XZ).")]
    [SerializeField] private Transform boardAnchor;
    [SerializeField] private Material pixelBaseMaterial;
    #endregion

    public void InstallBindings(ContainerBuilder builder)
    {
        if (conveyorData != null) builder.RegisterValue(conveyorData);
        builder.RegisterValue(boardConfig);

        builder.RegisterType(typeof(GameSession), Lifetime.Singleton, Resolution.Lazy);
        builder.RegisterType(typeof(TickScheduler), Lifetime.Singleton, Resolution.Lazy);

        RegisterBoard(builder);
    }

    private void RegisterBoard(ContainerBuilder builder)
    {
        builder.RegisterFactory(container =>
        {
            var board = new PixelBoard(container.Resolve<LevelService>().Current, container.Resolve<BoardConfigSO>());
            container.Resolve<TickScheduler>().Register(board);
            return board;
        }, Lifetime.Singleton, Resolution.Lazy);

        Vector3 center = boardAnchor != null ? boardAnchor.position : Vector3.zero;
        builder.RegisterFactory(container =>
        {
            var board = container.Resolve<PixelBoard>();
            return new BoardLayout(center, board.Width, board.Height, container.Resolve<BoardConfigSO>().CellSize);
        }, Lifetime.Singleton, Resolution.Lazy);

        builder.RegisterFactory(container =>
            new PaletteMaterialCache(pixelBaseMaterial, container.Resolve<LevelService>().Current.Palette),
            Lifetime.Singleton, Resolution.Lazy);
    }
}
