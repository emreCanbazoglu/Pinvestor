using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Asset Variants Examples/ManagedReferencesSO")]
public class ManagedReferencesSO : ScriptableObject
{
    [TextArea(15, 100)]
    public string info;

#if UNITY_2019_3_OR_NEWER
    [Header("[SerializeReference]")]
    [SerializeReference]
    public Class1 shared1;
    [SerializeReference]
    public Class1 shared2;

    public ManagedReferencesSO()
    {
        shared1 = shared2 = new Class1();

        a.b = b;
        b.a = a;

        srA.b = srB;
        srB.a = srA;
    }

    [Serializable]
    public class Class1
    {
        public string text;
        public float number;
        public int count;
        public Rect rect;
    }

    [Header("A references B and B references A")]
    [SerializeReference]
    public A srA = new A();
    [SerializeReference]
    public B srB = new B();

    [Space(5)]

    public A a = new A();
    public B b = new B();

    [Serializable]
    public class A
    {
        [SerializeReference]
        public B b;

        public float variable;
    }

    [Serializable]
    public class B
    {
        [SerializeReference]
        public A a;
    }
#endif
}
