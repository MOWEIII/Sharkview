using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoRenderer.Core.Models;

public partial class SceneObject : ObservableObject
{
    [ObservableProperty]
    private string _name = "Object";

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private ObservableVector3 _position = new();

    [ObservableProperty]
    private ObservableVector3 _rotation = new();

    [ObservableProperty]
    private ObservableVector3 _scale = new(1, 1, 1);

    public ObservableCollection<Modifier> Modifiers { get; set; } = new();
}

public enum EnvironmentType
{
    SolidColor,
    StudioPreset,
    CustomImage
}

public partial class WorldSettings : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSolidColor))]
    [NotifyPropertyChangedFor(nameof(IsPreset))]
    [NotifyPropertyChangedFor(nameof(IsCustom))]
    private EnvironmentType _environmentType = EnvironmentType.SolidColor;

    public bool IsSolidColor => EnvironmentType == EnvironmentType.SolidColor;
    public bool IsPreset => EnvironmentType == EnvironmentType.StudioPreset;
    public bool IsCustom => EnvironmentType == EnvironmentType.CustomImage;

    [ObservableProperty]
    private string _environmentTexturePath = string.Empty;

    [ObservableProperty]
    private bool _showEnvironmentBackground = true;

    [ObservableProperty]
    private string _backgroundColor = "#333333"; // Lighter gray default for visibility

    [ObservableProperty]
    private float _strength = 1.0f;

    [ObservableProperty]
    private bool _autoCamera = false;

    [ObservableProperty]
    private float _cameraDistance = 5.0f;

    [ObservableProperty]
    private float _cameraHeight = 0.0f;
    
    [ObservableProperty]
    private float _cameraAngle = 0.0f;
}

public enum LightType
{
    POINT,
    SUN,
    SPOT,
    AREA
}

public partial class LightObject : SceneObject
{
    [ObservableProperty]
    private LightType _lightType = LightType.POINT;

    [ObservableProperty]
    private float _energy = 1000.0f; // Watts or Blender Unit

    [ObservableProperty]
    private string _color = "#FFFFFF";
    
    public LightObject()
    {
        Name = "Light";
        Scale = new ObservableVector3(1, 1, 1); // Scale usually doesn't affect point lights but needed for transform
    }
}

public partial class ObservableVector3 : ObservableObject
{
    [ObservableProperty]
    private float _x;

    [ObservableProperty]
    private float _y;

    [ObservableProperty]
    private float _z;

    public ObservableVector3(float x = 0, float y = 0, float z = 0)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public abstract class Modifier : ObservableObject
{
    public string Name { get; set; } = "Modifier";
    public abstract string Type { get; }
}

public partial class AutoRotateModifier : Modifier
{
    public override string Type => "AutoRotate";

    [ObservableProperty]
    private string _axis = "Z"; // X, Y, Z

    [ObservableProperty]
    private float _speed = 1.0f;
}
