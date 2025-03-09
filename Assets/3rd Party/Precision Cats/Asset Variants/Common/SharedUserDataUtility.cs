/*
MIT License

Copyright (c) 2022 Steffen Vetne

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Object = UnityEngine.Object;

public static class SharedUserDataAssetsMenu
{
    public delegate void OnPasteHandler(Object asset, AssetImporter importer);
    public static event OnPasteHandler OnPaste;

    [MenuItem("Assets/Copy Importer UserData", false, 2000)]
    private static void CopyUserData(MenuCommand command)
    {
        var importer = GetImporter(Selection.activeObject);
        EditorGUIUtility.systemCopyBuffer = importer.userData;
    }
    [MenuItem("Assets/Copy Importer UserData", true)]
    private static bool ValidateCopyUserData()
    {
        return GetImporter(Selection.activeObject) != null;
    }

    [MenuItem("Assets/Paste Importer UserData", false, 2001)]
    private static void PasteUserData()
    {
        var selection = Selection.objects;
        for (int i = 0; i < selection.Length; i++)
        {
            Object target = selection[i];

            var importer = GetImporter(target);
            if (importer == null)
                continue;

            string clipboard = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(clipboard))
                clipboard = null;

            Undo.RecordObject(importer, "Paste UserData");
            importer.userData = clipboard;
            EditorUtility.SetDirty(importer); //? //importer.SaveAndReimport();

            if (OnPaste != null)
                OnPaste(target, importer);
        }
    }
    [MenuItem("Assets/Paste Importer UserData", true)]
    private static bool ValidatePasteUserData()
    {
        return GetImporter(Selection.activeObject) != null;

        //var selection = Selection.objects;
        //for (int i = 0; i < selection.Length; i++)
        //{
        //    if (GetImporter(selection[i]) != null)
        //        return true;
        //}
        //return false;
    }

    private static AssetImporter GetImporter(Object obj)
    {
        if (obj == null)
            return null;
        var path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path))
            return null;
        return AssetImporter.GetAtPath(path);
    }
}

public static class SharedUserDataUtility
{
    /// <summary>
    /// The key string used to store old userData that has not yet determined its owner.
    /// Ownership is only found using ValidateOwnership(true).
    /// If GetUserData() is never called from the original single userData owner (the script that originally saved a vanilla userData),
    /// then that userData will stick around with UNKNOWN_KEY as its key. It's also possible that another script will claim ownership of it by calling ValidateOwnership(true).
    /// An UNKNOWN_KEY userData Pair can be removed with ClearUNKNOWN() or by calling SetUserData() with clearUNKNOWN passed as true.
    /// </summary>
    public const string UNKNOWN_KEY = "UNKNOWN";


    /// <summary>
    /// Success = true if you claim ownership of the userData that you received.
    /// This needs to be called after you receive both it and the userData string from GetUserData().
    /// when you've determined that the userData matches what you expect.
    /// This is so that the previously vanilla userData can be added to a SharedUserData
    /// with the key you provided. If for example JsonUtility.FromJson<YourUserDataType>
    /// doesn't work on the string you receive, then the userData is not owned by the key you provided,
    /// and you should call validateOwnership(false)
    /// </summary>
    public delegate void ValidateOwnership(bool success);

    /// <summary>
    /// The replacement for "value = AssetImporter.userData;"
    /// </summary>
    /// <param name="key">The unique key you want to pair with your userData</param>
    /// <param name="onValidateOwnership">See ValidateOwnership for usage</param>
    /// <returns></returns>
    public static string GetUserData(this AssetImporter importer, string key, out ValidateOwnership onValidateOwnership)
    {
        string userData = importer.userData;
        if (string.IsNullOrEmpty(userData))
        {
            onValidateOwnership = null;
            return userData;
        }

        try
        {
            var shared = SharedUserData.FromJSON(userData);

            bool unknown; int _;
            var pair = shared.FindPair(key, out unknown, out _);
            if (unknown)
            {
                onValidateOwnership = (bool success) =>
                {
                    if (pair != null && success)
                    {
                        pair.key = key;
                        Undo.RecordObject(importer, "Automatic updating of Importer userData"); // TODO2: should this exist?
                        importer.userData = JsonUtility.ToJson(shared);

                        Debug.Log("UNKNOWN key Pair's userData:\n" + pair.userData + "\nWas validated successfully using validateOwnership(true).\nKey changed to: \"" + key + "\"");
                    }
                };
            }
            else
                onValidateOwnership = null;
            return pair != null ? pair.userData : null;
        }
        catch (Exception e)
        {
            onValidateOwnership = (bool success) =>
            {
                var newShared = new SharedUserData();
                newShared.typeIsSharedUserData = true;
                var newPair = new SharedUserData.Pair()
                {
                    key = success ? key : UNKNOWN_KEY,
                    userData = userData
                };
                newShared.pairs.Add(newPair);
                Undo.RecordObject(importer, "Automatic updating of Importer userData"); // TODO2: should this exist?
                importer.userData = JsonUtility.ToJson(newShared);

                Debug.Log(importer.assetPath + "'s AssetImporter's old UserData:\n" +
                    userData + "\nwas not yet a SharedUserData. Converted to SharedUserData from GetUserData() called with key \"" +
                    key + "\". The Pair was created with the key \"" + newPair.key + "\"" +
                    "\n\nThe exception from SharedUserData.FromJSON(userData):\n" + e);
            };
            return userData;
        }
    }

    private static void UndoAndSet(AssetImporter importer, string undoName, string userData)
    {
        if (undoName != null)
            Undo.RecordObject(importer, undoName);
        importer.userData = userData;
    }

    /// <summary>
    /// The replacement for "AssetImporter.userData = value;"
    /// </summary>
    /// <param name="key">The unique key you want to pair with your userData</param>
    /// <param name="clearUNKNOWN">See UNKNOWN_KEY. Pass as true if you want to get rid of unclaimed userData</param>
    /// <returns>If modified the importer's userData</returns>
    public static bool SetUserData(this AssetImporter importer, string key, string newUserData, string undoName = null, bool clearUNKNOWN = false)
    {
        string oldUserData = importer.userData;

        try
        {
            var shared = SharedUserData.FromJSON(oldUserData);

            bool dirty = false;

            if (clearUNKNOWN)
            {
                for (int i = 0; i < shared.pairs.Count; i++)
                {
                    if (shared.pairs[i].key == UNKNOWN_KEY)
                    {
                        Debug.Log("Removed SharedUserData Pair with UNKNOWN key and userData: " + shared.pairs[i].userData);
                        shared.pairs.RemoveAt(i);
                        i--;
                        dirty = true;
                    }
                }
            }

            bool empty = string.IsNullOrEmpty(newUserData);

            bool unknown; int pairID;
            var pair = shared.FindPair(key, out unknown, out pairID);
            if (pair == null)
            {
                if (!empty)
                {
                    var newPair = new SharedUserData.Pair()
                    {
                        key = key,
                        userData = newUserData
                    };
                    shared.pairs.Add(newPair);
                    dirty = true;
                }
            }
            else
            {
                if (empty)
                {
                    shared.pairs.RemoveAt(pairID);
                    dirty = true;
                }
                else if (pair.userData != newUserData)
                {
                    pair.userData = newUserData;
                    dirty = true;
                }
            }

            if (dirty)
            {
                if (shared.pairs.Count == 0)
                    UndoAndSet(importer, undoName, null);
                else
                    UndoAndSet(importer, undoName, JsonUtility.ToJson(shared));

                return true;
            }
        }
        catch (Exception e)
        {
            var newShared = new SharedUserData();
            newShared.typeIsSharedUserData = true;

            bool newExists = !string.IsNullOrEmpty(newUserData);
            if (newExists)
            {
                var newPair = new SharedUserData.Pair()
                {
                    key = key,
                    userData = newUserData
                };
                newShared.pairs.Add(newPair);
            }

            bool oldExisted = !string.IsNullOrEmpty(oldUserData);

            if (!clearUNKNOWN && oldExisted)
            {
                var unknownPair = new SharedUserData.Pair()
                {
                    key = UNKNOWN_KEY,
                    userData = oldUserData
                };
                newShared.pairs.Add(unknownPair);

                Debug.Log("Saved both an UNKNOWN key Pair of the old userData and a \"" + key + "\" key Pair of the new userData.\n" +
                    "If the old userData:\n" + oldUserData + "\nis the previous value of the new userData:\n" + newUserData +
                    "\nThen it's likely that the UNKNOWN data will simply stick around permanently without an owner, until clearUNKNOWN of SetUserData() is passed as true.\n" +
                    "An UNKNOWN key Pair is meant to find itself a known key with a call to GetUserData()'s validateOwnership(true). " +
                    "If you never use GetUserData() to read the previous userData for your key, before writing new userData for your key, it cannot find what key the old userData belongs to.");
            }

            if (newShared.pairs.Count == 0)
                UndoAndSet(importer, undoName, null);
            else
                UndoAndSet(importer, undoName, JsonUtility.ToJson(newShared));

            if (oldExisted)
            {
                Debug.Log(importer.assetPath + "'s AssetImporter's old UserData:\n" +
                    oldUserData + "\nwas not yet a SharedUserData. Converted to SharedUserData from SetUserData() called with key \"" + key + "\"" +
                    "\n\nThe exception from SharedUserData.FromJSON(oldUserData):\n" + e);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// See UNKNOWN_KEY
    /// </summary>
    public static void ClearUNKNOWN(this AssetImporter importer)
    {
        string userData = importer.userData;

        try
        {
            var shared = SharedUserData.FromJSON(userData);

            bool dirty = false;
            for (int i = 0; i < shared.pairs.Count; i++)
            {
                if (shared.pairs[i].key == UNKNOWN_KEY)
                {
                    Debug.Log("Removed SharedUserData Pair with UNKNOWN key and userData: " + shared.pairs[i].userData);
                    shared.pairs.RemoveAt(i);
                    i--;
                    dirty = true;
                }
            }

            if (dirty)
            {
                Undo.RecordObject(importer, "Cleared UNKNOWN key Pairs for userData of AssetImporter: " + importer.name);
                importer.userData = JsonUtility.ToJson(shared);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("ClearUNKNOWN() could not load a SharedUserData with this userData string:\n" + userData +
                "\n\nThe exception from SharedUserData.FromJSON(userData):\n" + e);
        }
    }


    [Serializable]
    private class SharedUserData
    {
        public static SharedUserData FromJSON(string json)
        {
            var shared = JsonUtility.FromJson<SharedUserData>(json);
            if (!shared.typeIsSharedUserData)
                throw new Exception("JSON is not SharedUserData");
            return shared;
        }

        public bool typeIsSharedUserData;

        public List<Pair> pairs = new List<Pair>();

        public Pair FindPair(string key, out bool unknown, out int id)
        {
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].key == key)
                {
                    unknown = false;
                    id = i;
                    return pairs[i];
                }
            }
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].key == UNKNOWN_KEY)
                {
                    unknown = true;
                    id = i;
                    return pairs[i];
                }
            }
            unknown = false;
            id = -1;
            return null;
        }

        [Serializable]
        public class Pair
        {
            public string key;
            public string userData;
        }
    }
}

#endif
