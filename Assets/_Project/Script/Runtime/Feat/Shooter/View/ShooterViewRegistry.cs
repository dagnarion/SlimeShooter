using System.Collections.Generic;

/// <summary>Tra ShooterView theo ShooterModel cho mọi view (cột, băng chuyền, khay).</summary>
public class ShooterViewRegistry
{
    private readonly Dictionary<ShooterModel, ShooterView> _views = new Dictionary<ShooterModel, ShooterView>();

    public void Register(ShooterModel model, ShooterView view) => _views[model] = view;
    public bool Unregister(ShooterModel model) => _views.Remove(model);
    public bool TryGet(ShooterModel model, out ShooterView view) => _views.TryGetValue(model, out view);
}
