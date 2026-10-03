using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Tools/SlimeShooter/Level Generator: ảnh pixel -> LevelSO (lưới color id + cột shooter cân bằng ammo).</summary>
public class LevelGeneratorWindow : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/_Project/Resource/SO/Levels";

    private Texture2D _texture;
    private PaletteSO _palette;
    private LevelSO _target;
    private string _newLevelName = "Level_001";
    private int _columnCount = 4;
    private string _ammoSteps = "10,20,30";
    private int _seed = 1;
    private int _conveyorSlots = 5;
    private int _cacheSlots = 5;
    private int _alphaThreshold = 128;

    private LevelAssetBuilder.Preview? _preview;
    private Vector2 _scroll;

    [MenuItem("Tools/SlimeShooter/Level Generator")]
    public static void Open() => GetWindow<LevelGeneratorWindow>("Level Generator");

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawTargetSection();
        EditorGUILayout.Space();
        DrawSettingsSection();
        EditorGUILayout.Space();
        DrawActions();

        if (_preview.HasValue)
        {
            EditorGUILayout.Space();
            DrawPreview(_preview.Value);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawTargetSection()
    {
        EditorGUILayout.LabelField("Đích", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _target = (LevelSO)EditorGUILayout.ObjectField("Level có sẵn", _target, typeof(LevelSO), false);
        if (EditorGUI.EndChangeCheck() && _target != null) LoadFromTarget();

        using (new EditorGUI.DisabledScope(_target != null))
        {
            _newLevelName = EditorGUILayout.TextField("Tên level mới", _newLevelName);
            EditorGUILayout.LabelField("Thư mục", DefaultOutputFolder, EditorStyles.miniLabel);
        }
    }

    private void DrawSettingsSection()
    {
        EditorGUILayout.LabelField("Nguồn & tham số", EditorStyles.boldLabel);
        _texture = (Texture2D)EditorGUILayout.ObjectField("Ảnh nguồn", _texture, typeof(Texture2D), false);
        _palette = (PaletteSO)EditorGUILayout.ObjectField("Palette", _palette, typeof(PaletteSO), false);
        _columnCount = Mathf.Max(1, EditorGUILayout.IntField("Số cột shooter", _columnCount));
        _ammoSteps = EditorGUILayout.TextField(new GUIContent("Bậc ammo", "Các mức ammo, cách nhau bởi dấu phẩy"), _ammoSteps);
        _seed = EditorGUILayout.IntField("Seed", _seed);
        _conveyorSlots = Mathf.Max(1, EditorGUILayout.IntField("Slot băng chuyền", _conveyorSlots));
        _cacheSlots = Mathf.Max(1, EditorGUILayout.IntField("Ô khay chờ", _cacheSlots));
        _alphaThreshold = EditorGUILayout.IntSlider("Ngưỡng alpha", _alphaThreshold, 1, 255);
    }

    private void DrawActions()
    {
        bool ready = _texture != null && _palette != null && ParseSteps().Length > 0;
        if (!ready) EditorGUILayout.HelpBox("Cần ảnh nguồn, palette và ít nhất một bậc ammo > 0.", MessageType.Info);

        using (new EditorGUI.DisabledScope(!ready))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview")) _preview = LevelAssetBuilder.BuildPreview(_texture, BuildOptions());
            if (GUILayout.Button("Seed ngẫu nhiên + Preview"))
            {
                _seed = Random.Range(1, 100000);
                _preview = LevelAssetBuilder.BuildPreview(_texture, BuildOptions());
            }
            if (GUILayout.Button(_target != null ? "Ghi vào level" : "Tạo level")) Generate();
        }

        using (new EditorGUI.DisabledScope(_target == null))
        {
            if (GUILayout.Button("Validate level có sẵn"))
            {
                var result = LevelValidator.Validate(_target);
                EditorUtility.DisplayDialog("Validate", $"{_target.name}\n\n{result}", "OK");
            }
        }
    }

    private void Generate()
    {
        var options = BuildOptions();
        if (_target != null)
        {
            var preview = LevelAssetBuilder.BuildPreview(_texture, options);
            LevelAssetBuilder.Apply(_target, _texture, options, preview);
            AssetDatabase.SaveAssets();
            _preview = preview;
        }
        else
        {
            string path = $"{DefaultOutputFolder}/{_newLevelName}.asset";
            _target = LevelAssetBuilder.CreateOrUpdate(path, _texture, options, out var preview);
            _preview = preview;
        }

        EditorGUIUtility.PingObject(_target);
        if (!_preview.Value.Validation.IsValid) Debug.LogError($"[LevelGenerator] {_target.name} không hợp lệ:\n{_preview.Value.Validation}");
        else Debug.Log($"[LevelGenerator] Đã ghi {_target.name}: {_preview.Value.Columns.Sum(c => c.shooters.Count)} shooter, {_preview.Value.Columns.Count} cột");
    }

    private void LoadFromTarget()
    {
        _palette = _target.Palette != null ? _target.Palette : _palette;
        if (!string.IsNullOrEmpty(_target.SourceTexturePath))
            _texture = AssetDatabase.LoadAssetAtPath<Texture2D>(_target.SourceTexturePath);
        _columnCount = _target.GeneratorColumnCount;
        _ammoSteps = string.Join(",", _target.GeneratorAmmoSteps);
        _seed = _target.Seed;
        _conveyorSlots = _target.ConveyorSlots;
        _cacheSlots = _target.CacheSlots;
        _preview = null;
    }

    private LevelAssetBuilder.Options BuildOptions() => new LevelAssetBuilder.Options
    {
        Palette = _palette,
        ColumnCount = _columnCount,
        AmmoSteps = ParseSteps(),
        Seed = _seed,
        ConveyorSlots = _conveyorSlots,
        CacheSlots = _cacheSlots,
        AlphaThreshold = (byte)_alphaThreshold
    };

    private int[] ParseSteps()
    {
        return _ammoSteps.Split(',')
            .Select(s => int.TryParse(s.Trim(), out int v) ? v : 0)
            .Where(v => v > 0)
            .ToArray();
    }

    #region Preview drawing
    private void DrawPreview(LevelAssetBuilder.Preview preview)
    {
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(preview.Validation.IsValid ? "Validator: OK (Σammo = Σpixel cho mọi màu)" : preview.Validation.ToString(),
            preview.Validation.IsValid ? MessageType.Info : MessageType.Error);
        if (preview.Bake.MaxColorDistance > 60f)
            EditorGUILayout.HelpBox($"Có màu trong ảnh lệch xa palette (distance {preview.Bake.MaxColorDistance:0}). Kiểm tra lại palette.", MessageType.Warning);

        DrawBoard(preview);
        EditorGUILayout.Space();
        DrawColorCounts(preview);
        EditorGUILayout.Space();
        DrawColumns(preview);
    }

    private void DrawBoard(LevelAssetBuilder.Preview preview)
    {
        float cell = Mathf.Clamp((position.width - 40f) / preview.Width, 4f, 20f);
        Rect area = GUILayoutUtility.GetRect(cell * preview.Width, cell * preview.Height, GUILayout.ExpandWidth(false));
        EditorGUI.DrawRect(area, new Color(0.15f, 0.15f, 0.15f));

        for (int y = 0; y < preview.Height; y++)
        for (int x = 0; x < preview.Width; x++)
        {
            int id = preview.Bake.Cells[y * preview.Width + x];
            if (id == LevelSO.EmptyCell) continue;
            // y = 0 là hàng dưới cùng -> vẽ ở đáy.
            var rect = new Rect(area.x + x * cell, area.y + (preview.Height - 1 - y) * cell, cell - 1f, cell - 1f);
            EditorGUI.DrawRect(rect, _palette.GetColor(id));
        }
    }

    private void DrawColorCounts(LevelAssetBuilder.Preview preview)
    {
        var pixels = LevelBaker.CountPerColor(preview.Bake.Cells);
        var ammo = new Dictionary<int, int>();
        foreach (var spec in preview.Columns.SelectMany(c => c.shooters))
        {
            ammo.TryGetValue(spec.colorId, out int sum);
            ammo[spec.colorId] = sum + spec.ammo;
        }

        EditorGUILayout.LabelField($"Màu ({pixels.Count}) — pixel / ammo", EditorStyles.boldLabel);
        foreach (var pair in pixels.OrderByDescending(p => p.Value))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSwatch(pair.Key);
                _palette.TryGetEntry(pair.Key, out var entry);
                ammo.TryGetValue(pair.Key, out int ammoCount);
                EditorGUILayout.LabelField($"{pair.Key} {entry.name}", GUILayout.Width(140));
                EditorGUILayout.LabelField($"{pair.Value} / {ammoCount}");
            }
        }
    }

    private void DrawColumns(LevelAssetBuilder.Preview preview)
    {
        EditorGUILayout.LabelField($"Cột shooter ({preview.Columns.Count}) — trên cùng là shooter đứng đầu", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (var column in preview.Columns)
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(70)))
                {
                    foreach (var spec in column.shooters)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            DrawSwatch(spec.colorId);
                            EditorGUILayout.LabelField(spec.ammo.ToString(), GUILayout.Width(40));
                        }
                    }
                }
            }
        }
    }

    private void DrawSwatch(int colorId)
    {
        Rect rect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
        EditorGUI.DrawRect(rect, _palette.GetColor(colorId));
    }
    #endregion
}
