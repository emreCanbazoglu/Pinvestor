There is some useful editor stuff in the Properties/ folder.
(You can see them in action in Asset Variants Examples/Scriptable Objects/Foldered Example SO)

Aside from those, you probably want to delete the rest of the stuff in this folder.

Now for an explanation on what the things in the Properties/ folder do:
	
    - Bool 4: Like a Vector4 but with bools. It also comes with Bool3
	
    - AssertRange Drawer/Attribute: Similar to the [Range(min, max)] attribute, but doesn't draw the slider. It's just to ensure the int/float stays within a certain range. The use case for that opposed to RangeAttribute, is that in certain cases the range of acceptable values is large and chosen rather arbitrarily. If Range is used with a very large range, precision is all but lost, and the space taken up by the slider is a waste.
    
    - ButtonBool Drawer/Attribute: It's not exactly good practice to use this for code that will exist in a build (for editor-only code it doesn't really matter). But basically it's just a bool with a button instead of a toggle, which should be used like this:
        public class Script : MonoBehaviour/ScriptableObject
        {
            [ButtonBool]
            public bool doThing;
            public void DoThing() { }
            private void OnValidate()
            {
                if (doThing)
                {
                    doThing = false;
                    DoThing();
                }
            }
        }
    Unfortunately it leaves behind an unnecessary bool field even in a build, and because it even is a field means that Asset Variants will draw an override button for it.
    
    - FolderedEditor: it is used to group up fields into foldouts. It's mostly useful when you have different categories, and you want to create derived classes whose fields can get grouped up into the same categories, whereas normally fields are ordered by sequence, with derived fields at the bottom. You use it like so:
        #if UNITY_EDITOR
        using UnityEditor;

        [CustomEditor(typeof(FolderedExample))]
        public class FolderedExampleEditor : FolderedEditor<FolderedExample> { }
        #endif

        and the foldout folders are created with these:
            [BeginFolder("Folder Name", 10)]
        (where the 10 is the spacing between the new folder and the previous field/folder)
            [EndFolder]
        and
            [NewFolder("Folder Name", 10)]
        which combines the functionality of the previous 2; it ends the previous folder and begins a new one.
        
        (Reusing the same folder name will group them up into the same folder.)
        
    - InlinePropertyDrawer: This basically makes it so a class/struct will be drawn as if it's a part of the containing class; it will draw without the foldout, optionally without an indent, and optionally without drawing the labels of the fields. It needs to be inherited from like so:
        #if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ExampleClass))]
        public class ExampleClassDrawer : InlinePropertyDrawer
        {
            protected override bool DrawLabels => true;

            protected override bool Indent => false;
        }
        #endif
        
    - NegativeColor Drawer/Attribute: This is used to allow setting negative values to a color field. A color doesnt have to be minimum 0 anymore.
    
    - ReadOnlyField Drawer/Attribute: Locks the field with GUI.enabled=false, making it readonly.
    
    - SuggestionRange Drawer/Attribute: You give it 2 ranges, min/max and absoluteMin/absoluteMax, min/max are used for the slider/s, and absoluteMin/absoluteMax are used to clamp the value/s. That's to give the functionality to a slider that blender has, where the slider is sometimes just a suggestion, and you can type a number in the text field beside it to give a number outside of the slider's range.
    It works with: int/float/Vector2/Vector2Int/Vector3/Vector3Int/Vector4 (no Vector4Int exists).
    For the vectors, the sliders are ordered vertically.
    
    - MinMax Drawer/Attribute I found somewhere, it's used with a Vector2 field, and it gives a 2 ended slider, where you can specify the min and the max of a range using the x and the y of the Vector2.
    