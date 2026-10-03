using Reflex.Core;
using Reflex.Enums;
using UnityEngine;
using Resolution = Reflex.Enums.Resolution;

/// <summary>Binding cho scene gameplay. Gắn cùng GameObject có ContainerScope trong SCN_Gameplay.</summary>
public class GameplayInstaller : MonoBehaviour, IInstaller
{
    #region Config
    [SerializeField] private BoardConfigSO boardConfig;
    [SerializeField] private ConveyorConfigSO conveyorConfig;
    [SerializeField] private ShootingConfigSO shootingConfig;
    [SerializeField] private BoosterConfigSO boosterConfig;
    #endregion

    #region Scene refs
    [Tooltip("Tâm của board trong world (board nằm trên mặt phẳng XZ).")]
    [SerializeField] private Transform boardAnchor;
    [SerializeField] private Material pixelBaseMaterial;
    #endregion

    [Min(0)] [SerializeField] private int maxRevivesPerLevel = 1;

    public void InstallBindings(ContainerBuilder builder)
    {
        if (boardConfig == null || conveyorConfig == null || shootingConfig == null || boosterConfig == null || pixelBaseMaterial == null)
        {
            Debug.LogError($"[GameplayInstaller] Thiếu config trên '{name}': boardConfig={boardConfig != null}, " +
                           $"conveyorConfig={conveyorConfig != null}, shootingConfig={shootingConfig != null}, " +
                           $"boosterConfig={boosterConfig != null}, pixelBaseMaterial={pixelBaseMaterial != null}", this);
        }

        builder.RegisterValue(boardConfig);
        builder.RegisterValue(conveyorConfig);
        builder.RegisterValue(shootingConfig);

        builder.RegisterType(typeof(GameSession), Lifetime.Singleton, Resolution.Lazy);
        builder.RegisterType(typeof(TickScheduler), Lifetime.Singleton, Resolution.Lazy);

        RegisterBoard(builder);
        RegisterShooters(builder);
        RegisterConveyor(builder);
        RegisterFlow(builder);
    }

    private void RegisterFlow(ContainerBuilder builder)
    {
        builder.RegisterType(typeof(LevelProgressSystem), Lifetime.Singleton, Resolution.Eager);
        builder.RegisterFactory(container => new ReviveService(container.Resolve<GameSession>(),
                container.Resolve<RuleSystem>(), container.Resolve<CacheTray>(), maxRevivesPerLevel),
            Lifetime.Singleton, Resolution.Lazy);
        builder.RegisterType(typeof(GameFlowService), Lifetime.Singleton, Resolution.Lazy);

        if (boosterConfig != null) builder.RegisterValue(boosterConfig);
        builder.RegisterFactory(container => new BoosterService(
                container.Resolve<GameSession>(), container.Resolve<ISaveService>(), container.Resolve<BoosterConfigSO>(),
                container.Resolve<ConveyorModel>(), container.Resolve<ConveyorController>(), container.Resolve<ShooterColumns>(),
                container.Resolve<CacheTray>(), container.Resolve<PixelBoard>(), container.Resolve<ShooterPickService>(),
                container.Resolve<EndRushSystem>(), container.Resolve<LevelProgressSystem>().PlayedLevelNumber,
                container.Resolve<LevelService>().Current.Seed),
            Lifetime.Singleton, Resolution.Lazy);
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

    private void RegisterShooters(ContainerBuilder builder)
    {
        builder.RegisterFactory(container => new ShooterColumns(container.Resolve<LevelService>().Current),
            Lifetime.Singleton, Resolution.Lazy);
        builder.RegisterType(typeof(ShooterPickService), Lifetime.Singleton, Resolution.Lazy);
        builder.RegisterType(typeof(ShooterViewRegistry), Lifetime.Singleton, Resolution.Lazy);
    }

    private void RegisterConveyor(ContainerBuilder builder)
    {
        builder.RegisterFactory<IConveyorPath>(container =>
        {
            var bounds = container.Resolve<BoardLayout>().WorldBounds;
            var config = container.Resolve<ConveyorConfigSO>();
            return new RectConveyorPath(bounds.center, bounds.extents.x + config.Margin, bounds.extents.z + config.Margin, config.CornerRadius);
        }, Lifetime.Singleton, Resolution.Lazy);

        builder.RegisterFactory(container =>
        {
            var conveyor = new ConveyorModel(container.Resolve<IConveyorPath>(), container.Resolve<ConveyorConfigSO>(),
                container.Resolve<LevelService>().Current.ConveyorSlots);
            container.Resolve<TickScheduler>().Register(conveyor);
            return conveyor;
        }, Lifetime.Singleton, Resolution.Lazy);

        builder.RegisterFactory(container => new CacheTray(container.Resolve<LevelService>().Current.CacheSlots),
            Lifetime.Singleton, Resolution.Lazy);

        // Eager: phải tồn tại ngay để subscribe sự kiện chọn shooter / kiểm tra end rush / luật.
        builder.RegisterType(typeof(ConveyorController), Lifetime.Singleton, Resolution.Eager);
        builder.RegisterType(typeof(EndRushSystem), Lifetime.Singleton, Resolution.Eager);
        builder.RegisterType(typeof(RuleSystem), Lifetime.Singleton, Resolution.Eager);

        builder.RegisterFactory(container =>
        {
            // Resolve board + conveyor trước để chúng tick trước ShootingSystem.
            var conveyor = container.Resolve<ConveyorModel>();
            var board = container.Resolve<PixelBoard>();
            var shooting = new ShootingSystem(conveyor, board, container.Resolve<BoardLayout>(),
                container.Resolve<EndRushSystem>(), container.Resolve<ShootingConfigSO>());
            container.Resolve<TickScheduler>().Register(shooting);
            return shooting;
        }, Lifetime.Singleton, Resolution.Eager);
    }
}
