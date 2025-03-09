using UnityEngine;

//// Because Unity's internal integer implicit "enums" only work with IMGUI, and with UI Toolkit it causes an error:
///*
//System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation. ---> System.ArgumentException: Type provided must be an Enum.
//Parameter name: enumType
//at System.RuntimeType.GetEnumUnderlyingType () [0x00012] in <6073cf49ed704e958b8a66d540dea948>:0 
//at System.Enum.GetUnderlyingType (System.Type enumType) [0x00014] in <6073cf49ed704e958b8a66d540dea948>:0 
//at UnityEngine.EnumDataUtility.GetCachedEnumData (System.Type enumType, System.Boolean excludeObsolete, System.Func`2[T,TResult] nicifyName) [0x00058] in <40205bb2cb25478a9cb0f5e54cf11441>:0 
//at UnityEditor.EnumDataUtility.GetCachedEnumData (System.Type enumType, System.Boolean excludeObsolete) [0x00001] in <a017e354f3154926a5617fbac3a64fcc>:0 
//at UnityEditor.UIElements.PropertyField.CreateOrUpdateFieldFromProperty (UnityEditor.SerializedProperty property, System.Object originalField) [0x004aa] in <5cf6bb2c14f1476f9cd6b153e70ca8b2>:0 
//at (wrapper managed-to-native) System.Reflection.RuntimeMethodInfo.InternalInvoke(System.Reflection.RuntimeMethodInfo,object,object[],System.Exception&)
//at System.Reflection.RuntimeMethodInfo.Invoke (System.Object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, System.Object[] parameters, System.Globalization.CultureInfo culture) [0x0006a] in <6073cf49ed704e958b8a66d540dea948>:0 
//...
//*/
//public override bool OnlyIMGUI => true;

[CreateAssetMenu(menuName = "UnrelatedTestSO")]
public class UnrelatedTestSO : ScriptableObject
{
#if UNITY_EDITOR && EDITOR_REPLACEMENT && ASSET_VARIANTS
    [UnityEditor.InitializeOnLoadMethod]
    private static void Init()
    {
        AssetVariants.AVHeader.OnCreated += (header) =>
        {
            if (header.targets[0] is UnrelatedTestSO)
            {
                header.allowParentAssignment.Add((Object newParent) =>
                {
                    if (newParent == null)
                        return true;
                    var type = newParent.GetType();
                    if (typeof(UnrelatedTestSO).IsAssignableFrom(type))
                        return true;
                    if (typeof(Texture).IsAssignableFrom(type))
                        return true;
                    return false;
                });

                header.parentFilterType = typeof(Object);
            }
        };
    }
#endif

    [Header("float (doesn't get copied because the propertyType doesn't match)")]
    public float m_Width;
    [Header("long")]
    public long m_Height;

    public string childOnlyField = "Some Text";

    public TextureSettings m_TextureSettings;

    [System.Serializable]
    public class TextureSettings
    {
        [Tooltip("Huh, I guess I found out how Unity's custom enums work.")]
        public int m_Aniso, m_WrapU, m_WrapW;
    }
}
