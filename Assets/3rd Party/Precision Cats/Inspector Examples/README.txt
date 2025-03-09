Color Wraps/ is mostly for giving color to an editor, but it's also helpful in giving an outer border to each unique editor, to distinguish between them. It's nice for Basic SubAssets/, as there can then be multiple leaf nodes in the Editor Replacement's EditorTreeDef. There are some settings for customizing the appearance of these rectangles.


Basic SubAssets/ is used for giving dropdown editors for a main asset's subassets. This is nice because it's easier to multi select the main asset, and then modifying a common child can be possible (if the child is editable).
The way it works is it will not hide/skip any child (unless maxSubAssetsToDisplay is exceeded), instead it will choose a child of the first target and find any same-typed child in the other targets, and if it doesn't exist in some of the other targets, it will still draw it. Then to discern what assets the targets of the sub-asset editor are sub-assets to, there is an additional dropdown for the targets.
Because of this, the order of the sub-assets shown can be unreliable, as it has to take any set of sub-assets and combine them into a single sequence with the sub-assets of multiple other assets.


Tooltip Icon/ is for showing a little (!) icon when a field has a tooltip.


Testing/Destructive/ is just for demonstration purposes for how Editor Replacement works. It completely clears the EditorTreeDef and adds a plain old Editor to the root. Depending on the priority of the replacers involved, it can destroy everything in the EditorTreeDef, or just the original editor.

Testing/Empty/ is for testing that the behaviour of the inspector remains the same whether a WrapperEditor is used or not.

Testing/DefaultUIToolkitEditorWhileWeWaitForTheRealThing.cs is for testing the parity of Asset Variants when UI Toolkit is used instead of IMGUI.
