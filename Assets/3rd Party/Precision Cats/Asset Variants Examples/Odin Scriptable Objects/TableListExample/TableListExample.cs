#if ODIN_INSPECTOR
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Asset Variants Odin Examples/TableListExample")]
public class TableListExample : ScriptableObject
{
    [TableList(ShowIndexLabels = true)]
    public List<SomeCustomClass> TableListWithIndexLabels = new List<SomeCustomClass>()
    {
        new SomeCustomClass(),
        new SomeCustomClass(),
    };

    [TableList(DrawScrollView = true, MaxScrollViewHeight = 200, MinScrollViewHeight = 100)]
    public List<SomeCustomClass> MinMaxScrollViewTable = new List<SomeCustomClass>()
    {
        new SomeCustomClass(),
        new SomeCustomClass(),
    };

    [TableList(AlwaysExpanded = true, DrawScrollView = false)]
    public List<SomeCustomClass> AlwaysExpandedTable = new List<SomeCustomClass>()
    {
        new SomeCustomClass(),
        new SomeCustomClass(),
    };

    [TableList(ShowPaging = true)]
    public List<SomeCustomClass> TableWithPaging = new List<SomeCustomClass>()
    {
        new SomeCustomClass(),
        new SomeCustomClass(),
    };

    [Serializable]
    public class SomeCustomClass
    {
        [TableColumnWidth(57, Resizable = false)]
        [PreviewField(Alignment = ObjectFieldAlignment.Center)]
        public Texture Icon;

        [TextArea]
        public string Description;

        [VerticalGroup("Combined Column"), LabelWidth(22)]
        public string A, B, C;

        [TableColumnWidth(60)]
        [Button, VerticalGroup("Actions")]
        public void Test1() { }

        [TableColumnWidth(60)]
        [Button, VerticalGroup("Actions")]
        public void Test2() { }


        // (This is of course not compatible. It randomizes the contents every time you select the asset.)

        //[OnInspectorInit]
        //private void CreateData()
        //{
        //    Description = ExampleHelper.GetString();
        //    Icon = ExampleHelper.GetTexture();
        //}
    }
}
#endif
