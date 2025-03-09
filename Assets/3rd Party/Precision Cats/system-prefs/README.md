# System Prefs

System Prefs is a means to fix compatibility issues stemming from arbitrary hardcoded priority values, chosen individually by multiple developers, which could not normally have reached balance.

The way it works is that instead of providing e.g. a double or integer as the priority, you provide a key string for `EditorProjectPrefs.GetInt(systemKey + ".Priority")/SetInt()` and `EditorProjectPrefs.GetBool(systemKey + ".Active")/SetBool()`. (Or rather SystemPrefsUtility which concats for you).
Then alongside your script, you create an SystemPrefsState asset, and give it the key, active bool, and your arbitrary guess for a reasonable priority value.
You could also instead just use Window/System Prefs to set the values without the use of an asset, but that would likely require a new Tab that understands what the systemKeys you are using are.

Keys should never be shared (among different systems), because then the user loses a lot of freedom for fixing compatibility problems.
For example say A=10,B=15,C=20 and D=50,E=15,F=60; if B and E share the same priority key, then the user can't reasonably make E have a priority between D and F at the same time as B's priority is between A and C.

A SystemPrefsState can be disabled, and this works by an EditorProjectPrefs bool that involves the asset's guid and localId. When it's disabled, it doesn't update EditorProjectPrefs with its systemActive field and systemPriority field anymore.

A SystemPrefsState asset is created at the bottom of the Assets/Create menu: System Prefs/State.
The SystemPrefsGroup asset which can be created at Assets/Create/System Prefs/Group is helpful for managing the states of multiple SystemPrefsState assets.
SystemPrefsGroup is designed to be mostly functional even if the asset file itself is not editable (`HideFlags.NotEditable`).

EditorProjectPrefs is a wrap of either PlayerPrefs, or EditorPrefs with a project unique prefix: "PlayerSettings.companyName + "." + PlayerSettings.productName + "/"". Which is used depends on if EPP_USES_EDITOR_PREFS is defined. The purpose of this is just so that the user can isolate editor-only prefs from PlayerPrefs if they choose to do so, by putting EPP_USES_EDITOR_PREFS in the scripting define symbols.
