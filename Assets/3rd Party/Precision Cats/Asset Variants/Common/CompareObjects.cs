using System.Collections;
using System;
//using System.Reflection;
//using System.Runtime.Serialization.Formatters.Binary;

internal static class ObjectComparer
{
    public static bool AreEqual(object a, object b)
    {
        //https://gist.github.com/danielkillyevo/5443439

        //Compare the references
        if (ReferenceEquals(a, null) ^ ReferenceEquals(b, null))
            return false;
        if (ReferenceEquals(a, b))
            return true;

        var aType = a.GetType();

        if (aType.IsPrimitive) //aType.IsValueType (IsValueType isn't good enough if there are reference fields in a struct)
            return a.Equals(b);

        //Compare the types
        if (aType != b.GetType())
            return false;

        if (aType == typeof(string))
            return (string)a == (string)b;

        if (typeof(UnityEngine.Object).IsAssignableFrom(aType))
            return ReferenceEquals(a, b);

        if (a is Array)
        {
            var arrayA = ((Array)a);
            var arrayB = ((Array)b);
            if (arrayA.Rank != arrayB.Rank)
                return false;
            for (int i = 0; i < arrayA.Rank; i++)
                if (arrayA.GetLength(i) != arrayB.GetLength(i))
                    return false;
        }

        if (a is IEnumerable)
        {
            var e1 = ((IEnumerable)a).GetEnumerator();
            var e2 = ((IEnumerable)b).GetEnumerator();
            try
            {
                while (true)
                {
                    bool move1 = e1.MoveNext();
                    if (move1 != e2.MoveNext())
                        return false;
                    if (!move1)
                        break;

                    var item1 = e1.Current;
                    var item2 = e2.Current;
                    if (!AreEqual(item1, item2))
                        return false;
                }
            }
            finally
            {
                if (e1 is IDisposable)
                    ((IDisposable)e1).Dispose();
                if (e2 is IDisposable)
                    ((IDisposable)e2).Dispose();
            }

            // (What this misses is if you inherit from a collection and give it additional fields. But that is rather unlikely).
            return true;
        }

        /*
        //Get all field infos of b
        var fields = b.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        //Compare the field values of the a and b object
        foreach (var field in fields)
        {
            var aValue = field.GetValue(a);
            var bValue = field.GetValue(b);
            if (bValue == null && aValue == null)
                continue;

            if (bValue == null ^ aValue == null)
                return false;

            //Comparison if the field is a generic (IList type)
            if ((aValue is IList && field.FieldType.IsGenericType) ||
                (bValue is IList && field.FieldType.IsGenericType))
            {
                //here we work with dynamics because don't need to care about the generic type
                dynamic cur = aValue;
                dynamic oth = bValue;
                if (cur != null && cur.Count > 0)
                {
                    var result = false;
                    foreach (object cVal in cur)
                    {
                        foreach (object oVal in oth)
                        {
                            //Recursively call the AreEqual method
                            var areEqual = AreEqual(cVal, oVal);
                            if (!areEqual) continue;

                            result = true;
                            break;
                        }

                    }
                    if (result == false)
                        return false;
                }
            }
            else
            {
                //Comparison for fields of a non collection type
                var curType = aValue.GetType();

                //Comparison for primitive types
                if (curType.IsValueType || aValue is string)
                {
                    var areEquals = aValue.Equals(bValue);
                    if (areEquals == false)
                        return false;      //This is the out point for this methods
                }
                else
                {
                    //values are complex/classes
                    //that's why we have to recursively call the AreEqual methods
                    var areEqual = AreEqual(aValue, bValue);
                    if (areEqual == false)
                        return false;
                }
            }
        }
        */

        // TODO2: is this the best idea?
        string jsonA = UnityEngine.JsonUtility.ToJson(a);
        if (jsonA == "{}") // Just for safety
            return false;
        string jsonB = UnityEngine.JsonUtility.ToJson(b);
        //UnityEngine.Debug.Log("JSONs: " + jsonA + " " + jsonB + " " + a);
        if (jsonA != jsonB)
            return false;

        return true;
    }
}
