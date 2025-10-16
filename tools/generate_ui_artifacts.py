#!/usr/bin/env python3
"""Generate UI modernization artifacts for NX-343."""

from __future__ import annotations

import csv
import json
import os
from datetime import datetime
from textwrap import dedent

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ARTIFACT_DIR = os.path.join(REPO_ROOT, "artifacts")
SCREENSHOT_DIR = os.path.join(ARTIFACT_DIR, "ui-screenshots")

os.makedirs(ARTIFACT_DIR, exist_ok=True)
os.makedirs(SCREENSHOT_DIR, exist_ok=True)

source_repos = [
    "Legacy SourceCode -V6",
    "Legacy SourceCode -Dev",
    "Legacy SourceCode -AgValoniaGPS",
]

items = [
    {
        "id": "app_shell",
        "name": "Main Field View",
        "element_type": "panel",
        "description": "Primary FormGPS shell hosting the OpenGL canvas, menu strip, toolbars, and status widgets.",
        "primary_action": "Operate guidance session, monitor map layers, and control automation.",
        "secondary_actions": [
            "Open guidance dialogs (AB lines, headland, flags)",
            "Toggle autosteer and section control states",
            "Access settings, help, and profile management"
        ],
        "menu_entries": [
            {"hierarchy": ["File"], "text": "File"},
            {"hierarchy": ["Tools"], "text": "Tools"},
            {"hierarchy": ["Field"], "text": "Field"},
            {"hierarchy": ["Help"], "text": "Help"}
        ],
        "toolbar_buttons": [
            "AutoSteer Toggle",
            "YouTurn",
            "Section Master",
            "Manual Section Overrides",
            "Guidance Snap"
        ],
        "settings_pages": ["config_host_dialog"],
        "dialogs": [
            "job_manager_dialog",
            "field_directory_dialog",
            "steer_wizard_dialog",
            "boundary_tool_dialog",
            "event_viewer_dialog"
        ],
        "domain_logic": ["mapping", "section-control", "autosteer"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/FormGPS.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/FormGPS.cs"},
            {"repo": "Legacy SourceCode -AgValoniaGPS", "path": "Views/MainWindow.axaml"}
        ],
        "dependencies": ["OpenTK", "ApplicationCore", "System.Windows.Forms"],
        "likely_plugin_owner": "core.shell",
        "screenshot": "ui-screenshots/app-shell.png",
        "screenshot_description": "Map canvas with menus, toolbar buttons, and status strip."
    },
    {
        "id": "map_canvas",
        "name": "OpenGL Map Canvas",
        "element_type": "panel",
        "description": "OpenGL surface rendering boundaries, coverage, guidance lines, and machine models.",
        "primary_action": "Render geospatial layers and accept pan/zoom interactions.",
        "secondary_actions": [
            "Display section states and AB guidance",
            "Support flag context menu"
        ],
        "menu_entries": [
            {"hierarchy": ["Field", "Boundary"], "text": "Boundary"},
            {"hierarchy": ["Field", "Headland"], "text": "Headland"}
        ],
        "toolbar_buttons": ["Zoom In", "Zoom Out", "Center Vehicle"],
        "settings_pages": ["config_data_page", "config_vehicle_page"],
        "dialogs": ["boundary_tool_dialog", "flag_manager_dialog"],
        "domain_logic": ["mapping"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/OpenGL.Designer.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/OpenGL.Designer.cs"},
            {"repo": "Legacy SourceCode -AgValoniaGPS", "path": "Views/MainWindow.axaml"}
        ],
        "dependencies": ["OpenTK", "WorldGrid"],
        "likely_plugin_owner": "core.map",
        "screenshot": "ui-screenshots/map-canvas.png",
        "screenshot_description": "OpenGL surface with vehicle glyph, boundaries, and coverage polygons."
    },
    {
        "id": "top_toolbar",
        "name": "Top Command Toolbar",
        "element_type": "toolbar",
        "description": "Toolbar containing automation toggles, AB controls, and section master buttons.",
        "primary_action": "Provide single-click access to automation states and frequently used commands.",
        "secondary_actions": [
            "Launch steering configuration dialogs",
            "Toggle manual section overrides",
            "Arm YouTurn guidance"
        ],
        "menu_entries": [
            {"hierarchy": ["Tools", "Wizards"], "text": "Steer Wizard"}
        ],
        "toolbar_buttons": [
            "btnAutoSteer",
            "btnAutoSteerConfig",
            "btnAutoYouTurn",
            "btnSectionMasterAuto",
            "btnAutoTrack"
        ],
        "settings_pages": ["steer_settings_dialog", "section_config_dialog"],
        "dialogs": ["steer_wizard_dialog", "section_config_dialog"],
        "domain_logic": ["autosteer", "section-control"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/FormGPS.Designer.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/FormGPS.Designer.cs"},
            {"repo": "Legacy SourceCode -AgValoniaGPS", "path": "Views/MainWindow.axaml"}
        ],
        "dependencies": ["System.Windows.Forms", "HotkeyService"],
        "likely_plugin_owner": "core.shell",
        "screenshot": "ui-screenshots/top-toolbar.png",
        "screenshot_description": "Command buttons with glyphs for autosteer, sections, and AB guidance."
    },
    {
        "id": "file_menu",
        "name": "File Menu",
        "element_type": "menu",
        "description": "Right-aligned menu with profile management, simulator toggle, reset, and About.",
        "primary_action": "Manage profiles and global session state.",
        "secondary_actions": ["Toggle simulator", "Reset configuration", "Open About"],
        "menu_entries": [
            {"hierarchy": ["File", "Profile"], "text": "Profile"},
            {"hierarchy": ["File", "Simulator On"], "text": "Simulator On"},
            {"hierarchy": ["File", "About"], "text": "About"}
        ],
        "toolbar_buttons": [],
        "settings_pages": ["config_host_dialog"],
        "dialogs": ["about_dialog", "simulator_main_dialog"],
        "domain_logic": ["core"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/FormGPS.Designer.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/FormGPS.Designer.cs"}
        ],
        "dependencies": ["System.Windows.Forms"],
        "likely_plugin_owner": "core.shell",
        "screenshot": "ui-screenshots/file-menu.png",
        "screenshot_description": "Dropdown with profile, simulator, reset, and about commands."
    },
    {
        "id": "tools_menu",
        "name": "Tools Menu",
        "element_type": "menu",
        "description": "Dropdown housing wizards, charts, event viewer, guidelines, and webcam tools.",
        "primary_action": "Provide access to diagnostics and setup utilities.",
        "secondary_actions": ["Launch steer wizard", "Open steering charts", "Access log viewer"],
        "menu_entries": [
            {"hierarchy": ["Tools", "Wizards"], "text": "Steer Wizard"},
            {"hierarchy": ["Tools", "Charts"], "text": "Steer Chart"},
            {"hierarchy": ["Tools", "Log Viewer"], "text": "Log Viewer"}
        ],
        "toolbar_buttons": [],
        "settings_pages": ["steer_settings_dialog"],
        "dialogs": ["steer_wizard_dialog", "event_viewer_dialog", "webcam_dialog"],
        "domain_logic": ["autosteer", "diagnostics"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/FormGPS.Designer.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/FormGPS.Designer.cs"}
        ],
        "dependencies": ["System.Windows.Forms"],
        "likely_plugin_owner": "core.shell",
        "screenshot": "ui-screenshots/tools-menu.png",
        "screenshot_description": "Tools dropdown with wizard and chart entries."
    },
    {
        "id": "field_menu",
        "name": "Field Menu",
        "element_type": "menu",
        "description": "Field menu grouping boundary, headland, tramline, and applied data operations.",
        "primary_action": "Manage field geometry assets for the current job.",
        "secondary_actions": ["Open boundary editor", "Launch headland builder", "Clear applied coverage"],
        "menu_entries": [
            {"hierarchy": ["Field", "Boundary"], "text": "Boundary"},
            {"hierarchy": ["Field", "Headland"], "text": "Headland"},
            {"hierarchy": ["Field", "Delete Applied"], "text": "Delete Applied"}
        ],
        "toolbar_buttons": [],
        "settings_pages": [],
        "dialogs": ["boundary_tool_dialog"],
        "domain_logic": ["mapping"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/FormGPS.Designer.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/FormGPS.Designer.cs"}
        ],
        "dependencies": ["System.Windows.Forms"],
        "likely_plugin_owner": "core.map",
        "screenshot": "ui-screenshots/field-menu.png",
        "screenshot_description": "Menu section with boundary and headland icons."
    },
    {
        "id": "job_manager_dialog",
        "name": "Job Manager Dialog",
        "element_type": "dialog",
        "description": "Workflow for starting, resuming, or closing field jobs with metadata entry.",
        "primary_action": "Create or resume field jobs with correct directories and file templates.",
        "secondary_actions": ["Import ISOXML fields", "Open job history"],
        "menu_entries": [
            {"hierarchy": ["File", "Profile"], "text": "New Job"}
        ],
        "toolbar_buttons": ["Job Start"],
        "settings_pages": [],
        "dialogs": ["field_directory_dialog", "field_existing_dialog", "field_save_dialog"],
        "domain_logic": ["mapping", "data-management"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Field/FormJob.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Field/FormJob.cs"}
        ],
        "dependencies": ["System.IO", "RegistrySettings"],
        "likely_plugin_owner": "core.jobs",
        "screenshot": "ui-screenshots/job-manager.png",
        "screenshot_description": "Dialog with job list, field metadata, and start/close buttons."
    },
    {
        "id": "field_directory_dialog",
        "name": "Create Field Dialog",
        "element_type": "dialog",
        "description": "Form for creating new field directories with sanitized names and timestamp helpers.",
        "primary_action": "Capture new field name and initialize directory structure.",
        "secondary_actions": ["Append date/time", "Cancel without creating"],
        "menu_entries": [
            {"hierarchy": ["File", "Profile"], "text": "Create Field"}
        ],
        "toolbar_buttons": [],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["mapping"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Field/FormFieldDir.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Field/FormFieldDir.cs"}
        ],
        "dependencies": ["RegistrySettings", "FormGPS"],
        "likely_plugin_owner": "core.jobs",
        "screenshot": "ui-screenshots/create-field.png",
        "screenshot_description": "Input form with field name textbox and timestamp helpers."
    },
    {
        "id": "field_existing_dialog",
        "name": "Open Existing Field Dialog",
        "element_type": "dialog",
        "description": "Dialog listing existing fields with previews for quick job selection.",
        "primary_action": "Allow operator to select and open existing field directories.",
        "secondary_actions": ["Delete field", "Preview boundary"],
        "menu_entries": [
            {"hierarchy": ["File", "Profile"], "text": "Load Field"}
        ],
        "toolbar_buttons": [],
        "settings_pages": [],
        "dialogs": ["field_map_dialog"],
        "domain_logic": ["mapping"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Field/FormFieldExisting.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Field/FormFieldExisting.cs"}
        ],
        "dependencies": ["System.IO", "FieldPreview"],
        "likely_plugin_owner": "core.jobs",
        "screenshot": "ui-screenshots/open-field.png",
        "screenshot_description": "Grid of field directories with preview pane."
    },
    {
        "id": "field_map_dialog",
        "name": "Field Map Preview",
        "element_type": "dialog",
        "description": "Map viewer showing selected field boundary, coverage, and machine preview before loading.",
        "primary_action": "Confirm selected field geometry prior to activation.",
        "secondary_actions": ["Adjust zoom", "Inspect coverage layers"],
        "menu_entries": [
            {"hierarchy": ["Field", "Boundary"], "text": "Preview Boundary"}
        ],
        "toolbar_buttons": ["Zoom", "Pan"],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["mapping"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Field/FormMap.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Field/FormMap.cs"}
        ],
        "dependencies": ["OpenTK"],
        "likely_plugin_owner": "core.map",
        "screenshot": "ui-screenshots/field-map.png",
        "screenshot_description": "Preview map with boundary outline and coverage shading."
    },
    {
        "id": "field_save_dialog",
        "name": "Save Field Confirmation",
        "element_type": "dialog",
        "description": "Confirmation dialog prompting the operator to save or discard changes when exiting a field.",
        "primary_action": "Prevent accidental data loss when closing field sessions.",
        "secondary_actions": ["Discard changes", "Cancel"],
        "menu_entries": [
            {"hierarchy": ["File", "Profile"], "text": "Close Field"}
        ],
        "toolbar_buttons": [],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["data-management"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Field/FormSaveOrNot.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Field/FormSaveOrNot.cs"}
        ],
        "dependencies": ["FileManagement"],
        "likely_plugin_owner": "core.jobs",
        "screenshot": "ui-screenshots/save-field.png",
        "screenshot_description": "Yes/No dialog with save summary text."
    },
    {
        "id": "boundary_tool_dialog",
        "name": "Boundary Tool",
        "element_type": "dialog",
        "description": "Editor for drawing, editing, and validating field boundaries and exclusion zones.",
        "primary_action": "Let operators define and edit field boundaries using recorded data.",
        "secondary_actions": ["Merge segments", "Simplify geometry", "Export shapefile"],
        "menu_entries": [
            {"hierarchy": ["Field", "Boundary"], "text": "Boundary Tool"}
        ],
        "toolbar_buttons": ["Snap to AB", "Boundary Undo"],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["mapping"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Field/FormBndTool.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Field/FormBndTool.cs"}
        ],
        "dependencies": ["BoundaryBuilder", "WorldGrid"],
        "likely_plugin_owner": "core.map",
        "screenshot": "ui-screenshots/boundary-tool.png",
        "screenshot_description": "Boundary editing window with control buttons and map preview."
    },
    {
        "id": "flag_manager_dialog",
        "name": "Flag Manager",
        "element_type": "dialog",
        "description": "List and manage map flags with color-coded quick actions.",
        "primary_action": "Create, edit, and delete field flags with context metadata.",
        "secondary_actions": ["Filter by category", "Jump map to flag"],
        "menu_entries": [
            {"hierarchy": ["Tools", "Flags"], "text": "Flag Manager"}
        ],
        "toolbar_buttons": ["Add Flag"],
        "settings_pages": [],
        "dialogs": ["flag_entry_dialog"],
        "domain_logic": ["mapping"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Field/FormFlags.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Field/FormFlags.cs"}
        ],
        "dependencies": ["FlagService"],
        "likely_plugin_owner": "core.map",
        "screenshot": "ui-screenshots/flag-manager.png",
        "screenshot_description": "Grid of flags with color chips and quick action buttons."
    },
    {
        "id": "section_control_widget",
        "name": "Section Control Widget",
        "element_type": "widget",
        "description": "Toolbar widget showing per-section status with manual override toggles.",
        "primary_action": "Allow operator to monitor and override section application states.",
        "secondary_actions": ["Toggle master", "Run test cycle"],
        "menu_entries": [
            {"hierarchy": ["Tools", "Section Control"], "text": "Section Control"}
        ],
        "toolbar_buttons": ["Section Master", "Auto/Manual"],
        "settings_pages": ["section_config_dialog"],
        "dialogs": ["section_config_dialog"],
        "domain_logic": ["section-control"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Sections.Designer.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Sections.Designer.cs"}
        ],
        "dependencies": ["SectionControlService"],
        "likely_plugin_owner": "plugin.section-control",
        "screenshot": "ui-screenshots/section-widget.png",
        "screenshot_description": "Row of section indicators with auto/manual toggles."
    },
    {
        "id": "section_config_dialog",
        "name": "Section Configuration",
        "element_type": "dialog",
        "description": "Configure number of sections, widths, delays, and controller ports.",
        "primary_action": "Persist section geometry and IO mapping for implement control.",
        "secondary_actions": ["Calibrate delays", "Assign controller outputs"],
        "menu_entries": [
            {"hierarchy": ["Settings", "Sections"], "text": "Section Settings"}
        ],
        "toolbar_buttons": [],
        "settings_pages": ["config_tool_page"],
        "dialogs": [],
        "domain_logic": ["section-control"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Settings/FormButtonsRightPanel.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Settings/FormButtonsRightPanel.cs"},
            {"repo": "Legacy SourceCode -AgValoniaGPS", "path": "Views/UserControls/VehicleImplementConfigUserControl.axaml"}
        ],
        "dependencies": ["ImplementModel", "SectionControlService"],
        "likely_plugin_owner": "plugin.section-control",
        "screenshot": "ui-screenshots/section-config.png",
        "screenshot_description": "Grid inputs for section widths with preview diagram."
    },
    {
        "id": "steer_settings_dialog",
        "name": "Steer Settings",
        "element_type": "dialog",
        "description": "Detailed configuration for autosteer controller gains, sensors, and hardware IO.",
        "primary_action": "Tune autosteer controller parameters for the active vehicle.",
        "secondary_actions": ["Test motor outputs", "Review steer charts"],
        "menu_entries": [
            {"hierarchy": ["Settings", "Autosteer"], "text": "Autosteer Settings"}
        ],
        "toolbar_buttons": ["Open Steer Settings"],
        "settings_pages": ["config_vehicle_page"],
        "dialogs": ["steer_chart_dialog"],
        "domain_logic": ["autosteer"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Settings/FormSteer.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Settings/FormSteer.cs"},
            {"repo": "Legacy SourceCode -AgValoniaGPS", "path": "Views/UserControls/VehicleImplementConfigUserControl.axaml"}
        ],
        "dependencies": ["SteerModule", "VehicleModel"],
        "likely_plugin_owner": "plugin.autosteer",
        "screenshot": "ui-screenshots/steer-settings.png",
        "screenshot_description": "Tabbed dialog with PID inputs and hardware mappings."
    },
    {
        "id": "steer_wizard_dialog",
        "name": "Steer Wizard",
        "element_type": "dialog",
        "description": "Multi-step wizard guiding operators through initial autosteer tuning.",
        "primary_action": "Provide step-by-step configuration with live validation.",
        "secondary_actions": ["Run wheel angle tests", "Set default tuning"],
        "menu_entries": [
            {"hierarchy": ["Tools", "Wizards"], "text": "Steer Wizard"}
        ],
        "toolbar_buttons": ["Steer Wizard"],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["autosteer"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Settings/FormSteerWiz.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Settings/FormSteerWiz.cs"}
        ],
        "dependencies": ["SteerModule", "WizardFramework"],
        "likely_plugin_owner": "plugin.autosteer",
        "screenshot": "ui-screenshots/steer-wizard.png",
        "screenshot_description": "Wizard pages with navigation buttons and calibration prompts."
    },
    {
        "id": "steer_chart_dialog",
        "name": "Steer Performance Chart",
        "element_type": "dialog",
        "description": "Charting dialog plotting autosteer error, PWM, and response for diagnostics.",
        "primary_action": "Visualize steering performance metrics over time.",
        "secondary_actions": ["Export CSV", "Adjust sample rate"],
        "menu_entries": [
            {"hierarchy": ["Tools", "Charts"], "text": "Steer Chart"}
        ],
        "toolbar_buttons": [],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["autosteer", "diagnostics"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Settings/FormGraphSteer.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Settings/FormGraphSteer.cs"}
        ],
        "dependencies": ["System.Windows.Forms.DataVisualization"],
        "likely_plugin_owner": "plugin.autosteer",
        "screenshot": "ui-screenshots/steer-chart.png",
        "screenshot_description": "Line graph showing steer error and output traces."
    },
    {
        "id": "color_settings_dialog",
        "name": "Display Color Settings",
        "element_type": "dialog",
        "description": "Dialog for customizing UI theme colors including map surfaces and status indicators.",
        "primary_action": "Persist user-chosen colors for key UI elements.",
        "secondary_actions": ["Restore defaults", "Preview live"],
        "menu_entries": [
            {"hierarchy": ["Settings", "Display"], "text": "Colors"}
        ],
        "toolbar_buttons": [],
        "settings_pages": ["config_data_page"],
        "dialogs": [],
        "domain_logic": ["visual"],
        "visual_only": True,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Settings/FormColor.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Settings/FormColor.cs"},
            {"repo": "Legacy SourceCode -AgValoniaGPS", "path": "Views/App.axaml"}
        ],
        "dependencies": ["ThemeManager"],
        "likely_plugin_owner": "core.shell",
        "screenshot": "ui-screenshots/color-settings.png",
        "screenshot_description": "Color picker grid with preview swatches."
    },
    {
        "id": "hotkey_dialog",
        "name": "Hotkey Manager",
        "element_type": "dialog",
        "description": "Dialog listing keyboard shortcuts with ability to rebind commands.",
        "primary_action": "Let operator customize global hotkeys.",
        "secondary_actions": ["Restore defaults", "Export mapping"],
        "menu_entries": [
            {"hierarchy": ["Settings", "Input"], "text": "Hotkeys"}
        ],
        "toolbar_buttons": [],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["core"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Form_Keys.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Form_Keys.cs"}
        ],
        "dependencies": ["HotkeyService"],
        "likely_plugin_owner": "core.shell",
        "screenshot": "ui-screenshots/hotkeys.png",
        "screenshot_description": "Key binding table with edit buttons."
    },
    {
        "id": "gps_data_dialog",
        "name": "GPS Data Monitor",
        "element_type": "dialog",
        "description": "Displays live NMEA sentences, fix quality, and satellite metrics.",
        "primary_action": "Diagnose GPS feed and confirm fix quality.",
        "secondary_actions": ["Pause stream", "Copy data"],
        "menu_entries": [
            {"hierarchy": ["Tools", "Diagnostics"], "text": "GPS Data"}
        ],
        "toolbar_buttons": ["Show GPS Data"],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["telemetry"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/FormGPSData.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/FormGPSData.cs"}
        ],
        "dependencies": ["GPSProvider"],
        "likely_plugin_owner": "core.telemetry",
        "screenshot": "ui-screenshots/gps-data.png",
        "screenshot_description": "Window with scrolling NMEA text and status lights."
    },
    {
        "id": "udp_status_dialog",
        "name": "UDP Status",
        "element_type": "dialog",
        "description": "Communication console for UDP telemetry, showing packet counters and connection status.",
        "primary_action": "Verify UDP networking state between modules.",
        "secondary_actions": ["Restart sockets", "Send test packet"],
        "menu_entries": [
            {"hierarchy": ["Tools", "Diagnostics"], "text": "UDP Console"}
        ],
        "toolbar_buttons": [],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["telemetry"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/UDPComm.Designer.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/UDPComm.Designer.cs"}
        ],
        "dependencies": ["UdpComm"],
        "likely_plugin_owner": "core.telemetry",
        "screenshot": "ui-screenshots/udp-status.png",
        "screenshot_description": "Console showing UDP packets in/out and connection toggles."
    },
    {
        "id": "event_viewer_dialog",
        "name": "Event Viewer",
        "element_type": "dialog",
        "description": "Log viewer displaying system events, errors, and warnings with filtering.",
        "primary_action": "Review diagnostic logs within the application.",
        "secondary_actions": ["Filter by severity", "Export logs"],
        "menu_entries": [
            {"hierarchy": ["Tools", "Log Viewer"], "text": "Log Viewer"}
        ],
        "toolbar_buttons": ["Open Log Viewer"],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["diagnostics"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/FormEventViewer.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/FormEventViewer.cs"}
        ],
        "dependencies": ["AgLibrary.Logging"],
        "likely_plugin_owner": "core.telemetry",
        "screenshot": "ui-screenshots/event-viewer.png",
        "screenshot_description": "Log grid with severity icons and filter controls."
    },
    {
        "id": "help_dialog",
        "name": "Help Dialog",
        "element_type": "dialog",
        "description": "Static help content summarizing hotkeys, usage, and support channels.",
        "primary_action": "Provide quick reference without leaving the app.",
        "secondary_actions": ["Open external documentation"],
        "menu_entries": [
            {"hierarchy": ["Help", "Help"], "text": "Help"}
        ],
        "toolbar_buttons": [],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["support"],
        "visual_only": True,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Form_Help.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Form_Help.cs"}
        ],
        "dependencies": ["Localization"],
        "likely_plugin_owner": "core.docs",
        "screenshot": "ui-screenshots/help-dialog.png",
        "screenshot_description": "Markdown-like text view with instructions."
    },
    {
        "id": "about_dialog",
        "name": "About Dialog",
        "element_type": "dialog",
        "description": "Displays version, contributors, and license information.",
        "primary_action": "Provide attribution and build metadata.",
        "secondary_actions": ["Copy version", "Open website"],
        "menu_entries": [
            {"hierarchy": ["Help", "About"], "text": "About"}
        ],
        "toolbar_buttons": [],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["support"],
        "visual_only": True,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/Form_About.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/Form_About.cs"}
        ],
        "dependencies": ["AssemblyInfo"],
        "likely_plugin_owner": "core.docs",
        "screenshot": "ui-screenshots/about-dialog.png",
        "screenshot_description": "Dialog with logo, version string, and license text."
    },
    {
        "id": "webcam_dialog",
        "name": "Webcam Viewer",
        "element_type": "dialog",
        "description": "Displays live webcam feed for implement or field monitoring.",
        "primary_action": "Show live video and snapshot controls.",
        "secondary_actions": ["Capture image", "Select camera"],
        "menu_entries": [
            {"hierarchy": ["Tools", "Webcam"], "text": "Webcam"}
        ],
        "toolbar_buttons": ["Open Webcam"],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["monitoring"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/FormWebCam.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/FormWebCam.cs"}
        ],
        "dependencies": ["AForge.Video"],
        "likely_plugin_owner": "plugin.video-monitor",
        "screenshot": "ui-screenshots/webcam.png",
        "screenshot_description": "Video preview window with capture and device selection controls."
    },
    {
        "id": "shift_position_dialog",
        "name": "Shift Position",
        "element_type": "dialog",
        "description": "Allows manual shifting of vehicle position for calibration and troubleshooting.",
        "primary_action": "Offset vehicle pose in the map to align with reality.",
        "secondary_actions": ["Nudge north/east", "Reset offset"],
        "menu_entries": [
            {"hierarchy": ["Tools", "Offset"], "text": "Shift Position"}
        ],
        "toolbar_buttons": ["Offset Fix"],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["mapping"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/GPS/Forms/FormShiftPos.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/GPS/Forms/FormShiftPos.cs"}
        ],
        "dependencies": ["GeoTransforms"],
        "likely_plugin_owner": "core.map",
        "screenshot": "ui-screenshots/shift-position.png",
        "screenshot_description": "Dialog with numeric offset inputs and apply/reset buttons."
    },
    {
        "id": "ag_io_loop_dialog",
        "name": "AgIO Loop Monitor",
        "element_type": "dialog",
        "description": "Main AgIO monitoring loop showing board state, NMEA throughput, and connection health.",
        "primary_action": "Monitor hardware IO loop running outside the main UI.",
        "secondary_actions": ["Restart loop", "Open advanced settings"],
        "menu_entries": [
            {"hierarchy": ["AgIO", "Loop"], "text": "AgIO Loop"}
        ],
        "toolbar_buttons": [],
        "settings_pages": [],
        "dialogs": ["ag_io_advanced_settings_dialog"],
        "domain_logic": ["hardware"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/AgIO/Source/Forms/FormLoop.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/AgIO/Source/Forms/FormLoop.cs"}
        ],
        "dependencies": ["AgIO"],
        "likely_plugin_owner": "core.agio",
        "screenshot": "ui-screenshots/agio-loop.png",
        "screenshot_description": "Status window with port indicators and throughput metrics."
    },
    {
        "id": "ag_io_serial_dialog",
        "name": "AgIO Serial Configuration",
        "element_type": "dialog",
        "description": "Serial port configuration for AgIO modules including port selection and baud rate.",
        "primary_action": "Assign serial ports and connection parameters for hardware modules.",
        "secondary_actions": ["Test connection", "Auto-detect"],
        "menu_entries": [
            {"hierarchy": ["AgIO", "Serial"], "text": "Serial Configuration"}
        ],
        "toolbar_buttons": [],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["hardware"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/AgIO/Source/Forms/SerialComm.Designer.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/AgIO/Source/Forms/SerialComm.Designer.cs"}
        ],
        "dependencies": ["System.IO.Ports"],
        "likely_plugin_owner": "core.agio",
        "screenshot": "ui-screenshots/agio-serial.png",
        "screenshot_description": "Serial port list with baud rate dropdown and connect button."
    },
    {
        "id": "ag_io_advanced_settings_dialog",
        "name": "AgIO Advanced Settings",
        "element_type": "dialog",
        "description": "Advanced configuration for AgIO modules covering buffer sizes, debug logging, and fail-safe behavior.",
        "primary_action": "Tune AgIO performance and diagnostics options.",
        "secondary_actions": ["Enable verbose logging", "Reset defaults"],
        "menu_entries": [
            {"hierarchy": ["AgIO", "Advanced"], "text": "Advanced Settings"}
        ],
        "toolbar_buttons": [],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["hardware"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/AgIO/Source/Forms/FormAdvancedSettings.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/AgIO/Source/Forms/FormAdvancedSettings.cs"}
        ],
        "dependencies": ["AgIO"],
        "likely_plugin_owner": "core.agio",
        "screenshot": "ui-screenshots/agio-advanced.png",
        "screenshot_description": "Tab strip with advanced toggles and log level selectors."
    },
    {
        "id": "simulator_main_dialog",
        "name": "ModSim Main Window",
        "element_type": "dialog",
        "description": "Simulation host showing vehicle telemetry, map preview, and control toggles for ModSim module.",
        "primary_action": "Run headless simulations for hardware testing.",
        "secondary_actions": ["Start/stop simulation", "Adjust noise profiles"],
        "menu_entries": [
            {"hierarchy": ["File", "Simulator On"], "text": "Simulator"}
        ],
        "toolbar_buttons": ["Run Sim", "Stop Sim"],
        "settings_pages": [],
        "dialogs": [],
        "domain_logic": ["simulation"],
        "visual_only": False,
        "source_files": [
            {"repo": "Legacy SourceCode -V6", "path": "SourceCode/ModSim/Source/Forms/FormSim.cs"},
            {"repo": "Legacy SourceCode -Dev", "path": "SourceCode/ModSim/Source/Forms/FormSim.cs"}
        ],
        "dependencies": ["SimulationEngine"],
        "likely_plugin_owner": "core.simulation",
        "screenshot": "ui-screenshots/modsim-main.png",
        "screenshot_description": "Simulator dashboard with telemetry readouts and start/stop buttons."
    }
]
inventory = {
    "metadata": {
        "generated_at": datetime.utcnow().isoformat() + "Z",
        "task_id": "NX-343",
        "source_repos": source_repos,
        "item_count": len(items)
    },
    "items": items
}

with open(os.path.join(ARTIFACT_DIR, "ui-inventory.json"), "w", encoding="utf-8") as fp:
    json.dump(inventory, fp, indent=2, ensure_ascii=False)
    fp.write("\n")

csv_path = os.path.join(ARTIFACT_DIR, "ui-inventory.csv")
with open(csv_path, "w", encoding="utf-8", newline="") as fp:
    writer = csv.writer(fp)
    writer.writerow(["id", "screen", "element_type", "primary_action", "secondary_actions", "dependencies", "likely_plugin_owner"])
    for item in items:
        writer.writerow([
            item["id"],
            item["name"],
            item["element_type"],
            item["primary_action"],
            "; ".join(item["secondary_actions"]),
            "; ".join(item["dependencies"]),
            item["likely_plugin_owner"]
        ])

readme_lines = [
    "# UI Screenshot Descriptions",
    "",
    "Environment captured textual descriptions because automation cannot capture WinForms surfaces inside the container.",
    "",
]
for item in items:
    readme_lines.append(f"## {item['name']} ({item['id']})")
    readme_lines.append(f"*Placeholder:* {item['screenshot_description']}")
    readme_lines.append("")

with open(os.path.join(SCREENSHOT_DIR, "README.md"), "w", encoding="utf-8") as fp:
    fp.write("\n".join(readme_lines).strip() + "\n")

inject_overrides = {
    "app_shell": "shell.main",
    "map_canvas": "shell.map",
    "top_toolbar": "toolbar.top",
    "file_menu": "menu.file",
    "tools_menu": "menu.tools",
    "field_menu": "menu.field",
    "job_manager_dialog": "dialog.jobs",
    "field_directory_dialog": "dialog.jobs",
    "field_existing_dialog": "dialog.jobs",
    "field_map_dialog": "dialog.jobs",
    "boundary_tool_dialog": "dialog.boundary",
    "flag_manager_dialog": "dialog.flags",
    "section_control_widget": "toolbar.section",
    "section_config_dialog": "settings.plugins",
    "steer_settings_dialog": "settings.plugins",
    "steer_wizard_dialog": "wizard.autosteer",
    "steer_chart_dialog": "diagnostics.autosteer",
    "color_settings_dialog": "settings.display",
    "hotkey_dialog": "settings.input",
    "gps_data_dialog": "diagnostics.telemetry",
    "udp_status_dialog": "diagnostics.telemetry",
    "event_viewer_dialog": "diagnostics.logs",
    "help_dialog": "help.modal",
    "about_dialog": "help.modal",
    "webcam_dialog": "plugin.video",
    "shift_position_dialog": "tools.offset",
    "ag_io_loop_dialog": "plugin.agio",
    "ag_io_serial_dialog": "plugin.agio",
    "ag_io_advanced_settings_dialog": "plugin.agio",
    "simulator_main_dialog": "simulation.shell"
}

mapping_entries = []
for item in items:
    owner = item["likely_plugin_owner"]
    if owner.startswith("plugin."):
        slug = owner.split(".", 1)[1]
        host = f"plugin:{slug}"
        contract = f"{slug.replace('-', '_')}.v1"
    else:
        host = "core"
        contract = f"core.{item['id'].replace('-', '_')}.v1"
    inject_point = inject_overrides.get(item["id"], {
        "menu": "menu.unassigned",
        "toolbar": "toolbar.main",
        "widget": "toolbar.widgets",
        "panel": "shell.panels",
        "dialog": "dialog.general"
    }[item["element_type"]])
    mapping_entries.append({
        "id": item["id"],
        "host": host,
        "inject_point": inject_point,
        "contract": contract
    })

mapping_yaml_lines = ["screens:"]
for entry in mapping_entries:
    mapping_yaml_lines.append(f"  - id: {entry['id']}")
    mapping_yaml_lines.append(f"    host: {entry['host']}")
    mapping_yaml_lines.append(f"    inject_point: {entry['inject_point']}")
    mapping_yaml_lines.append(f"    contract: {entry['contract']}")

with open(os.path.join(ARTIFACT_DIR, "ui-to-plugin.yaml"), "w", encoding="utf-8") as fp:
    fp.write("\n".join(mapping_yaml_lines) + "\n")

complexity_map = {
    "autosteer": "High",
    "section-control": "High",
    "simulation": "High",
    "hardware": "High",
    "mapping": "Medium",
    "telemetry": "Medium",
    "diagnostics": "Medium",
    "visual": "Low",
    "core": "Low",
    "support": "Low"
}

def infer_complexity(item: dict) -> str:
    levels = [complexity_map.get(domain, "Medium") for domain in item["domain_logic"]]
    if not levels:
        return "Low"
    if "High" in levels:
        return "High"
    if "Medium" in levels:
        return "Medium"
    return "Low"

spec_lines = ["# @nexus/ui-core Component Specification", "", "Generated from `artifacts/ui-inventory.json`. Each component references inventory ids and suggested complexity.", ""]
for item in items:
    complexity = infer_complexity(item)
    spec_lines.append(f"## {item['name']} ({item['id']})")
    spec_lines.append(f"* Element type: `{item['element_type']}`")
    spec_lines.append(f"* Complexity: {complexity}")
    spec_lines.append(f"* Domain logic: {', '.join(item['domain_logic']) or 'none'}")
    spec_lines.append(f"* Suggested contract: {mapping_entries[[m['id'] for m in mapping_entries].index(item['id'])]['contract']}")
    spec_lines.append("")
    spec_lines.append("### Props / Bindings")
    spec_lines.append("- Bind to core view-model services exposed via plugin contracts.")
    spec_lines.append("- Provide ICommand hooks for primary and secondary actions listed in inventory.")
    spec_lines.append("")
    spec_lines.append("### Example Usage")
    sample = dedent(f"""
    ```xaml
    <nexus:{item['name'].replace(' ', '')}
        PrimaryCommand="{{Binding Commands.Primary}}"
        SecondaryCommands="{{Binding Commands.Secondary}}"
        DataContext="{{Binding Ui.{item['id']} }}" />
    ```
    """)
    spec_lines.append(sample.strip())
    spec_lines.append("")

with open(os.path.join(ARTIFACT_DIR, "ui-core-spec.md"), "w", encoding="utf-8") as fp:
    fp.write("\n".join(spec_lines).rstrip() + "\n")

theme_tokens = {
    "metadata": {"version": 1, "generated_at": inventory["metadata"]["generated_at"]},
    "font": {
        "family": "Segoe UI",
        "sizes": {
            "xs": 10,
            "sm": 12,
            "md": 14,
            "lg": 18,
            "xl": 24
        }
    },
    "spacing": {
        "xs": 4,
        "sm": 8,
        "md": 12,
        "lg": 16,
        "xl": 24
    },
    "radius": {
        "sm": 4,
        "md": 8,
        "lg": 12
    },
    "color": {
        "light": {
            "surface": "#FFFFFF",
            "surface-alt": "#F3F6FB",
            "text": "#1F2933",
            "accent": "#1D9BF0",
            "accent-muted": "#7CC4FF",
            "success": "#2DCB74",
            "warning": "#F7B529",
            "danger": "#E5484D"
        },
        "dark": {
            "surface": "#131722",
            "surface-alt": "#1F2534",
            "text": "#F8FAFC",
            "accent": "#5AB9FF",
            "accent-muted": "#94D2FF",
            "success": "#4ADE80",
            "warning": "#FACC15",
            "danger": "#F87171"
        }
    },
    "stroke": {
        "thin": 1,
        "medium": 2,
        "thick": 3
    }
}

with open(os.path.join(ARTIFACT_DIR, "ui-theme-tokens.json"), "w", encoding="utf-8") as fp:
    json.dump(theme_tokens, fp, indent=2)
    fp.write("\n")

# Backlog generation
EPICS = {
    "shell-and-navigation": ["app_shell", "map_canvas", "top_toolbar", "file_menu", "tools_menu", "field_menu"],
    "settings-and-plugins": ["section_config_dialog", "steer_settings_dialog", "color_settings_dialog", "hotkey_dialog"],
    "mapping-and-canvas": ["boundary_tool_dialog", "flag_manager_dialog", "field_map_dialog", "shift_position_dialog"],
    "section-control": ["section_control_widget", "section_config_dialog"],
    "autosteer": ["steer_settings_dialog", "steer_wizard_dialog", "steer_chart_dialog"],
    "dialogs-and-wizards": ["job_manager_dialog", "field_directory_dialog", "field_existing_dialog", "gps_data_dialog", "event_viewer_dialog", "webcam_dialog", "help_dialog", "about_dialog"],
    "hardware-and-sim": ["ag_io_loop_dialog", "ag_io_serial_dialog", "ag_io_advanced_settings_dialog", "simulator_main_dialog", "udp_status_dialog"]
}

backlog = []
issue_id = 1
for epic, related_ids in EPICS.items():
    for item_id in related_ids:
        item = next(i for i in items if i["id"] == item_id)
        complexity = infer_complexity(item)
        for suffix in ["UI", "ViewModel", "Tests"]:
            title = f"[{epic}] Implement {item['name']} {suffix}"
            body = dedent(f"""
            ## Summary
            - Build the {suffix.lower()} for `{item['id']}` derived from ui-inventory.json.

            ## Context
            - Inventory reference: `{item['id']}`
            - Contract: `{mapping_entries[[m['id'] for m in mapping_entries].index(item['id'])]['contract']}`

            ## Acceptance Criteria
            - [ ] Matches legacy behavior documented in ui-inventory.json.
            - [ ] Hooks into plugin host via `{mapping_entries[[m['id'] for m in mapping_entries].index(item['id'])]['inject_point']}`.
            - [ ] Includes unit tests for primary interactions.
            - [ ] Provides Storybook-style example in @nexus/ui-core samples.

            ## Suggested Files
            - src/Aog.UI.Avalonia/{item['id'].replace('-', '_').title().replace('_', '')}{suffix}.cs
            - src/Aog.UI.Avalonia/Views/{item['id'].replace('-', '_').title().replace('_', '')}{suffix}.axaml
            """)
            labels = ["ui", "avalonia", f"epic:{epic}"]
            owner = item["likely_plugin_owner"]
            if owner.startswith("plugin."):
                labels.append(f"plugin:{owner.split('.', 1)[1]}")
            backlog.append({
                "title": title,
                "body": body.strip(),
                "labels": labels,
                "assignees": [],
                "milestone": None,
                "complexity": complexity,
                "inventory_id": item_id
            })
            issue_id += 1

# add PR template issues per epic
for epic in EPICS:
    template_body = dedent(f"""
    ## Summary
    - Describe changes for `{epic}` epic.

    ## Testing
    - [ ] `dotnet build`
    - [ ] `dotnet test`
    - [ ] `nexus sim smoke`
    """)
    backlog.append({
        "title": f"PR Template: {epic}",
        "body": template_body.strip(),
        "labels": ["meta", f"epic:{epic}"],
        "assignees": [],
        "milestone": None,
        "complexity": "Low",
        "inventory_id": None
    })

backlog_path = os.path.join(ARTIFACT_DIR, "ui-backlog.json")
with open(backlog_path, "w", encoding="utf-8") as fp:
    json.dump(backlog, fp, indent=2, ensure_ascii=False)
    fp.write("\n")

# License checklist
license_paths = {
    "Legacy SourceCode -V6": os.path.join(REPO_ROOT, "Legacy SourceCode -V6", "LICENSE"),
    "Legacy SourceCode -Dev": os.path.join(REPO_ROOT, "Legacy SourceCode -Dev", "LICENSE"),
    "Legacy SourceCode -AgValoniaGPS": os.path.join(REPO_ROOT, "Legacy SourceCode -AgValoniaGPS", "LICENSE"),
}

checklist_lines = ["# NX-343 License Compliance Checklist", ""]
for name, path in license_paths.items():
    if os.path.exists(path):
        with open(path, "r", encoding="utf-8", errors="ignore") as fp:
            head = fp.read(2048)
        if "GNU GENERAL PUBLIC LICENSE" in head:
            license_type = "GPL-3.0"
            obligations = [
                "Preserve GPL-3.0 license text with redistributed binaries.",
                "Provide source for any distributed binaries derived from this code.",
                "Do not copy GPL code into permissively licensed modules without relicensing."
            ]
        elif "MIT License" in head:
            license_type = "MIT"
            obligations = [
                "Retain copyright notice and MIT license text.",
                "Document modifications in THIRD_PARTY_NOTICES." ]
        else:
            license_type = "Unknown"
            obligations = ["Review repository manually; license not detected automatically."]
    else:
        license_type = "Missing"
        obligations = ["Contact repository owner to confirm license before reuse."]
    checklist_lines.append(f"## {name}")
    checklist_lines.append(f"- Detected license: {license_type}")
    for obligation in obligations:
        checklist_lines.append(f"- [ ] {obligation}")
    checklist_lines.append("- [ ] Record attribution in THIRD_PARTY_NOTICES.md")
    checklist_lines.append("")

checklist_lines.extend([
    "## Global Diff Checklist",
    "- [ ] Verify no GPL-licensed source files were copied verbatim into Nexus code.",
    "- [ ] Document regenerated assets (icons, bitmaps) instead of copying binary resources.",
    "- [ ] Confirm new UI components reference plugin contracts instead of legacy singletons.",
])

with open(os.path.join(ARTIFACT_DIR, "ui-license-checklist.md"), "w", encoding="utf-8") as fp:
    fp.write("\n".join(checklist_lines).rstrip() + "\n")

print("Generated UI modernization artifacts:")
for relative in [
    "ui-inventory.json",
    "ui-inventory.csv",
    "ui-screenshots/README.md",
    "ui-to-plugin.yaml",
    "ui-core-spec.md",
    "ui-theme-tokens.json",
    "ui-backlog.json",
    "ui-license-checklist.md"
]:
    print(f" - artifacts/{relative}")
