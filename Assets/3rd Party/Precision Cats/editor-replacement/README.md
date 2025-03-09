# Editor Replacement
 
Editor Replacement allows a third party to customize the editors that are registered with [CustomEditor()].


It works by using reflection to modify Unity's dictionaries of inspectedType as key and List<MonoEditorType> as value.

An EditorTreeDef is a node tree. The root node decides the outermost editor that Unity will create instead of the original editor. Typically a leaf node (no children nodes) would have null `replacer`, and if a node has null `replacer` then it needs to be a leaf node.

An EditorTreeNode with null `replacer` will create the original Editor, the Editor that Unity originally intended to create for targetType (which is not always inspectedType, because an EditorReplacer can change the targetType down the line).

An EditorReplacer instance gets one editor created for itself. If that Editor might end up with children editors in the treeDef (the EditorTreeNode which contains the EditorReplacer has children nodes), then GetEditorType() must return an IReplacementEditor, or preferably WrapperEditor.

Something WrapperEditor does is it calls `ERManager.CreateTree(this);` at the top of OnEnable(), and if the Editor calling that method is the root of the treeDef, it will cause the creation of every other editor in the treeDef (excluding the root node's, which Unity just created).
WrapperEditor will also call ERManager.DestroyTree(this) in OnDisable().

EditorTreeDefs are created after script compilation, and will be deep-copied and post-processed a final time before use by WrapperEditor's call to ERManager.CreateTree().

By default every non-abstract non-generic class that inherits from EditorReplacer will have an instance created for it, which will be put into a list, and sorted ascendingly by EditorPrefs.GetInt(replacer.EditorPrefsPriorityKey, 0). That means that a replacer with a higher priority will be called later, and have the chance to modify and overwrite any of the work in the EditorTreeDef done by previous EditorReplacers, if it decides to do so.

The EditorTreeDef specifies a combination of conditions that can be used to determine the course of replacement: `inspectedType,renderPipelineType,multiEdit,forChildClasses`.
For every inspectedType, it will accumulate every render pipeline type that is relevant, by going through all the original entries in List<MonoEditorType>, and through EditorReplacer.GetRenderPipelineTypes().
Then it will call EditorReplacer.Replace() for every combination of renderPipelineType, multiEdit, and forChildClasses. That means if there was no original Editor with [CustomEditorForRenderPipelineAttribute()], it will make these calls:
renderPipelineType,multiEdit,forChildClasses:
null,false,false
null,false,true
null,true,false
null,true,true

Typically, if it decides to modify the treeDef, a replacer will insert (a deep copy of) itself into the treeDef, usually with EditorTreeDef.WrapRoot()/Wrap()/WrapDefaults(). But it can do a lot more than just that.

After every replacer type has decided to modify or not modify the treeDef, PostProcess() will be called for every replacer in the treeDef.

The targets are not known yet, this has so far been creating a preset of sorts.

When CreateTree() gets called, the targets CAN be known. The treeDef is then deep-copied.
GetTargets() gets called for every node's replacer. It returns a readonly array of target UnityEngine.Objects, which get stored in EditorTreeNode.Targets.
It's very likely that you won't be overriding GetTargets() (and GetExpectedTargetType()). If you don't it will simply inherit the targets from its parent node (or inspectedTargets in the root node's case).
But if you want to change the targets of the resulting Editor, you are able to do so. This functionality is used in BasicSubAssetsEditorReplacers.cs/SubAssetsEditorReplacers.cs, which draws dropdown editors for its sub-assets.

OnAssignedTargets() gets called, and the replacer returns one of these:
`public enum ChildAction { Keep, RemoveReparent, RemoveWhole }`
Essentially the replacer decides if it's happy with the Targets the node received, usually by checking the type, and if not it can self destruct.

Then FinalPostProcess() gets called. This can do a final modification of the treeDef.

Then the root node's replacer gets a call to ProcessTree() (named ProcessEditorTree() for WrapperEditor) then a call to OnCreatedEditor() (named OnCreatedEditorTree() for WrapperEditor). The root editor was already created by Unity, but better late than never.

Then it goes down into the treeDef, creating the editors.
Before any Editor is created, if the node has a replacer, ShouldSkip(Editor parentEditor) gets called.
If a node has a replacer, it will use replacer.GetEditorType(), otherwise it will find the original editor type. It then calls Editor.CreateEditor() with that editor type and the node's Targets.
After every Editor is created, if the node has a replacer, the replacer will get a call to OnCreatedEditor().

Every IReplacementEditor gets ProcessTree() then OnCreatedTree() called, then every replacer in the treeDef gets OnCreatedTree() called.


I suggest you take a look at "EditorReplacer Examples".

This package depends on my other package SystemPrefs. That system controls the priority, and thereby sequence, of EditorReplacers.

This depends on my other package common.

Extra caution should be taken to ensure that EditorReplacer.DeepCopyTo() is properly implemented, if the EditorReplacer class has instance fields.

Let me know if there are things I should change or functionality I should add.
