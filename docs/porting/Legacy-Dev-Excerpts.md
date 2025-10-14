# Legacy V6 vs. Dev Source Excerpts

These minimal snippets capture the concrete behaviors referenced in the comparison note.

## File Storage Roots

```csharp
// Legacy V6 (SourceCode/GPS/Properties/RegistrySettings.cs)
baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AgOpenGPS");
```

```csharp
// Legacy Dev (SourceCode/AOG/Properties/CRegistrySettings.cs)
baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AOG");
vehiclesDirectory = Path.Combine(baseDirectory, "Vehicles");
toolsDirectory = Path.Combine(baseDirectory, "Tools");
fieldsDirectory = Path.Combine(baseDirectory, "Fields");
```

## Settings Model

```csharp
// Legacy V6 (SourceCode/GPS/Properties/Settings.cs)
public sealed class Settings
{
    public Point setWindow_Location = new Point(30, 30);
    public double setVehicle_toolWidth = 4.0;
    public double setVehicle_wheelbase = 3.3;
    public bool setTool_isToolTrailing = true;
    // ... single aggregate covers display, vehicle, and tool state.
}
```

```csharp
// Legacy Dev (SourceCode/AOG/Properties/Settings.cs)
public sealed class Settings
{
    private static UserSettings user_ = new UserSettings();
    public static UserSettings User => user_;
    private static VehicleSettings vehicle_ = new VehicleSettings();
    public static VehicleSettings Vehicle => vehicle_;
    private static ToolSettings tool_ = new ToolSettings();
    public static ToolSettings Tool => tool_;
}
```

## Job Directory Layout

```csharp
// Legacy Dev (SourceCode/AOG/Forms/FormJobNew.cs)
DirectoryInfo dirNewJob = new DirectoryInfo(Path.Combine(RegistrySettings.fieldsDirectory,
    mf.currentFieldDirectory, "Jobs", tboxJobName.Text.Trim()));
dirNewJob.Create();
mf.currentJobDirectory = Path.Combine("Jobs", tboxJobName.Text.Trim());
```

```csharp
// Legacy Dev (SourceCode/AOG/Forms/FormJobPicker.cs)
string directoryName = Path.Combine(RegistrySettings.fieldsDirectory, mf.currentFieldDirectory, "Jobs");
string[] dirs = Directory.GetDirectories(directoryName);
```

## Built-In Rate Control

```csharp
// Legacy Dev (SourceCode/AOG/Forms/Settings/FormNozSettings.cs)
nudSprayRateSet1.Value = Settings.Tool.setNozz.volumePerAreaSet1;
Settings.Tool.setNozz.volumePerAreaSet1 = nudSprayRateSet1.Value;
Settings.Tool.setNozz.rateAlarmPercent = nudRateAlarmPercent.Value * 0.01;
```

## Active Tool Steering

```csharp
// Legacy Dev (SourceCode/AOG/Forms/Settings/FormToolSteer.cs)
Settings.Tool.setToolSteer.gainP = (byte)hsbarPGain_Tool.Value;
Settings.Tool.setToolSteer.maxSteerAngle = (byte)hsbarMaxSteerAngle_Tool.Value;
PGN_231.pgn[PGN_231.maxSteerAngle] = Settings.Tool.setToolSteer.maxSteerAngle;
```

