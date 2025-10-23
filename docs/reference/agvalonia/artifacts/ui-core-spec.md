# @nexus/ui-core Component Specification

Generated from `ui-inventory.json`. Each component references inventory ids and suggested complexity.

## Main Field View (app_shell)
* Element type: `panel`
* Complexity: High
* Domain logic: mapping, section-control, autosteer
* Suggested contract: core.app_shell.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:MainFieldView
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.app_shell }" />
```

## OpenGL Map Canvas (map_canvas)
* Element type: `panel`
* Complexity: Medium
* Domain logic: mapping
* Suggested contract: core.map_canvas.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:OpenGLMapCanvas
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.map_canvas }" />
```

## Top Command Toolbar (top_toolbar)
* Element type: `toolbar`
* Complexity: High
* Domain logic: autosteer, section-control
* Suggested contract: core.top_toolbar.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:TopCommandToolbar
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.top_toolbar }" />
```

## File Menu (file_menu)
* Element type: `menu`
* Complexity: Low
* Domain logic: core
* Suggested contract: core.file_menu.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:FileMenu
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.file_menu }" />
```

## Tools Menu (tools_menu)
* Element type: `menu`
* Complexity: High
* Domain logic: autosteer, diagnostics
* Suggested contract: core.tools_menu.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:ToolsMenu
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.tools_menu }" />
```

## Field Menu (field_menu)
* Element type: `menu`
* Complexity: Medium
* Domain logic: mapping
* Suggested contract: core.field_menu.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:FieldMenu
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.field_menu }" />
```

## Job Manager Dialog (job_manager_dialog)
* Element type: `dialog`
* Complexity: Medium
* Domain logic: mapping, data-management
* Suggested contract: core.job_manager_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:JobManagerDialog
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.job_manager_dialog }" />
```

## Create Field Dialog (field_directory_dialog)
* Element type: `dialog`
* Complexity: Medium
* Domain logic: mapping
* Suggested contract: core.field_directory_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:CreateFieldDialog
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.field_directory_dialog }" />
```

## Open Existing Field Dialog (field_existing_dialog)
* Element type: `dialog`
* Complexity: Medium
* Domain logic: mapping
* Suggested contract: core.field_existing_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:OpenExistingFieldDialog
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.field_existing_dialog }" />
```

## Field Map Preview (field_map_dialog)
* Element type: `dialog`
* Complexity: Medium
* Domain logic: mapping
* Suggested contract: core.field_map_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:FieldMapPreview
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.field_map_dialog }" />
```

## Save Field Confirmation (field_save_dialog)
* Element type: `dialog`
* Complexity: Medium
* Domain logic: data-management
* Suggested contract: core.field_save_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:SaveFieldConfirmation
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.field_save_dialog }" />
```

## Boundary Tool (boundary_tool_dialog)
* Element type: `dialog`
* Complexity: Medium
* Domain logic: mapping
* Suggested contract: core.boundary_tool_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:BoundaryTool
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.boundary_tool_dialog }" />
```

## Flag Manager (flag_manager_dialog)
* Element type: `dialog`
* Complexity: Medium
* Domain logic: mapping
* Suggested contract: core.flag_manager_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:FlagManager
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.flag_manager_dialog }" />
```

## Section Control Widget (section_control_widget)
* Element type: `widget`
* Complexity: High
* Domain logic: section-control
* Suggested contract: section_control.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:SectionControlWidget
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.section_control_widget }" />
```

## Section Configuration (section_config_dialog)
* Element type: `dialog`
* Complexity: High
* Domain logic: section-control
* Suggested contract: section_control.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:SectionConfiguration
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.section_config_dialog }" />
```

## Steer Settings (steer_settings_dialog)
* Element type: `dialog`
* Complexity: High
* Domain logic: autosteer
* Suggested contract: autosteer.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:SteerSettings
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.steer_settings_dialog }" />
```

## Steer Wizard (steer_wizard_dialog)
* Element type: `dialog`
* Complexity: High
* Domain logic: autosteer
* Suggested contract: autosteer.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:SteerWizard
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.steer_wizard_dialog }" />
```

## Steer Performance Chart (steer_chart_dialog)
* Element type: `dialog`
* Complexity: High
* Domain logic: autosteer, diagnostics
* Suggested contract: autosteer.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:SteerPerformanceChart
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.steer_chart_dialog }" />
```

## Display Color Settings (color_settings_dialog)
* Element type: `dialog`
* Complexity: Low
* Domain logic: visual
* Suggested contract: core.color_settings_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:DisplayColorSettings
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.color_settings_dialog }" />
```

## Hotkey Manager (hotkey_dialog)
* Element type: `dialog`
* Complexity: Low
* Domain logic: core
* Suggested contract: core.hotkey_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:HotkeyManager
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.hotkey_dialog }" />
```

## GPS Data Monitor (gps_data_dialog)
* Element type: `dialog`
* Complexity: Medium
* Domain logic: telemetry
* Suggested contract: core.gps_data_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:GPSDataMonitor
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.gps_data_dialog }" />
```

## UDP Status (udp_status_dialog)
* Element type: `dialog`
* Complexity: Medium
* Domain logic: telemetry
* Suggested contract: core.udp_status_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:UDPStatus
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.udp_status_dialog }" />
```

## Event Viewer (event_viewer_dialog)
* Element type: `dialog`
* Complexity: Medium
* Domain logic: diagnostics
* Suggested contract: core.event_viewer_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:EventViewer
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.event_viewer_dialog }" />
```

## Help Dialog (help_dialog)
* Element type: `dialog`
* Complexity: Low
* Domain logic: support
* Suggested contract: core.help_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:HelpDialog
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.help_dialog }" />
```

## About Dialog (about_dialog)
* Element type: `dialog`
* Complexity: Low
* Domain logic: support
* Suggested contract: core.about_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:AboutDialog
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.about_dialog }" />
```

## Webcam Viewer (webcam_dialog)
* Element type: `dialog`
* Complexity: Medium
* Domain logic: monitoring
* Suggested contract: video_monitor.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:WebcamViewer
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.webcam_dialog }" />
```

## Shift Position (shift_position_dialog)
* Element type: `dialog`
* Complexity: Medium
* Domain logic: mapping
* Suggested contract: core.shift_position_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:ShiftPosition
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.shift_position_dialog }" />
```

## AgIO Loop Monitor (ag_io_loop_dialog)
* Element type: `dialog`
* Complexity: High
* Domain logic: hardware
* Suggested contract: core.ag_io_loop_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:AgIOLoopMonitor
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.ag_io_loop_dialog }" />
```

## AgIO Serial Configuration (ag_io_serial_dialog)
* Element type: `dialog`
* Complexity: High
* Domain logic: hardware
* Suggested contract: core.ag_io_serial_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:AgIOSerialConfiguration
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.ag_io_serial_dialog }" />
```

## AgIO Advanced Settings (ag_io_advanced_settings_dialog)
* Element type: `dialog`
* Complexity: High
* Domain logic: hardware
* Suggested contract: core.ag_io_advanced_settings_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:AgIOAdvancedSettings
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.ag_io_advanced_settings_dialog }" />
```

## ModSim Main Window (simulator_main_dialog)
* Element type: `dialog`
* Complexity: High
* Domain logic: simulation
* Suggested contract: core.simulator_main_dialog.v1

### Props / Bindings
- Bind to core view-model services exposed via plugin contracts.
- Provide ICommand hooks for primary and secondary actions listed in inventory.

### Example Usage
```xaml
<nexus:ModSimMainWindow
    PrimaryCommand="{Binding Commands.Primary}"
    SecondaryCommands="{Binding Commands.Secondary}"
    DataContext="{Binding Ui.simulator_main_dialog }" />
```
