using UnityEditor;
using UnityEngine;
using AttributeSystem.Authoring;

namespace Pinvestor.GameConfigSystem.Editor
{
    internal static class CompanyConfigDomainEditor
    {
        public static void Draw(SerializedObject serializedObject)
        {
            SerializedProperty companiesProp = serializedObject.FindProperty("_companies");
            if (companiesProp == null)
            {
                EditorGUILayout.HelpBox("Companies property not found.", MessageType.Warning);
                return;
            }

            DrawToolbar(companiesProp);
            EditorGUILayout.Space(4);
            DrawSpreadsheet(companiesProp);
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Raw Company Data", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(companiesProp, includeChildren: true);
        }

        private static void DrawToolbar(SerializedProperty companiesProp)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Company Row"))
            {
                companiesProp.InsertArrayElementAtIndex(companiesProp.arraySize);
                SerializedProperty row = companiesProp.GetArrayElementAtIndex(companiesProp.arraySize - 1);
                row.FindPropertyRelative("companyId").stringValue = string.Empty;
                row.FindPropertyRelative("attributes").arraySize = 0;
                row.FindPropertyRelative("values").arraySize = 0;
            }

            if (GUILayout.Button("Sort By CompanyId"))
            {
                SortByCompanyId(companiesProp);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSpreadsheet(SerializedProperty companiesProp)
        {
            AttributeColumnInfo[] attributeColumns = CollectAttributeColumns(companiesProp);

            EditorGUILayout.LabelField("Company Attributes Grid", EditorStyles.boldLabel);
            DrawHeader(attributeColumns);

            for (int i = 0; i < companiesProp.arraySize; i++)
            {
                SerializedProperty companyProp = companiesProp.GetArrayElementAtIndex(i);
                DrawRow(companiesProp, i, companyProp, attributeColumns);
            }
        }

        private static void DrawHeader(AttributeColumnInfo[] attributeColumns)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("CompanyId", EditorStyles.miniBoldLabel, GUILayout.Width(180));
            for (int i = 0; i < attributeColumns.Length; i++)
            {
                GUIContent header = new GUIContent(attributeColumns[i].Label, attributeColumns[i].Key);
                GUILayout.Label(header, EditorStyles.miniBoldLabel, GUILayout.Width(90));
            }
            GUILayout.Label("Actions", EditorStyles.miniBoldLabel, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawRow(
            SerializedProperty companiesProp,
            int rowIndex,
            SerializedProperty companyProp,
            AttributeColumnInfo[] attributeColumns)
        {
            SerializedProperty companyIdProp = companyProp.FindPropertyRelative("companyId");
            SerializedProperty attrsProp = companyProp.FindPropertyRelative("attributes");

            EditorGUILayout.BeginHorizontal();
            if (companyIdProp != null)
            {
                companyIdProp.stringValue = EditorGUILayout.TextField(
                    companyIdProp.stringValue,
                    GUILayout.Width(180));
            }

            for (int i = 0; i < attributeColumns.Length; i++)
            {
                DrawAttributeCell(attrsProp, attributeColumns[i].Key);
            }

            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                companiesProp.DeleteArrayElementAtIndex(rowIndex);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                return;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawAttributeCell(
            SerializedProperty attrsProp,
            string attributeKey)
        {
            if (attrsProp == null)
            {
                EditorGUILayout.FloatField(0f, GUILayout.Width(90));
                return;
            }

            int attrIndex = FindNamedEntryIndex(attrsProp, attributeKey);
            float currentValue = 0f;
            if (attrIndex >= 0)
            {
                SerializedProperty valueProp = attrsProp.GetArrayElementAtIndex(attrIndex).FindPropertyRelative("value");
                if (valueProp != null)
                {
                    currentValue = valueProp.floatValue;
                }
            }

            float newValue = EditorGUILayout.FloatField(currentValue, GUILayout.Width(90));
            if (!Mathf.Approximately(newValue, currentValue))
            {
                EnsureNamedEntry(attrsProp, attributeKey, out SerializedProperty valueProp);
                if (valueProp != null)
                {
                    valueProp.floatValue = newValue;
                }
            }
        }

        private static AttributeColumnInfo[] CollectAttributeColumns(SerializedProperty companiesProp)
        {
            var keys = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < companiesProp.arraySize; i++)
            {
                SerializedProperty row = companiesProp.GetArrayElementAtIndex(i);
                SerializedProperty attrs = row.FindPropertyRelative("attributes");
                if (attrs == null)
                {
                    continue;
                }

                for (int a = 0; a < attrs.arraySize; a++)
                {
                    SerializedProperty attr = attrs.GetArrayElementAtIndex(a);
                    string key = attr.FindPropertyRelative("key").stringValue;
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        keys.Add(key);
                    }
                }
            }

            string[] sortedKeys = new string[keys.Count];
            keys.CopyTo(sortedKeys);
            System.Array.Sort(sortedKeys, System.StringComparer.Ordinal);

            var results = new AttributeColumnInfo[sortedKeys.Length];
            var usedLabels = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
            for (int i = 0; i < sortedKeys.Length; i++)
            {
                string key = sortedKeys[i];
                string baseLabel = ResolveAttributeDisplayName(key);
                if (string.IsNullOrWhiteSpace(baseLabel))
                {
                    baseLabel = key;
                }

                string finalLabel = baseLabel;
                if (usedLabels.TryGetValue(baseLabel, out int count))
                {
                    count++;
                    usedLabels[baseLabel] = count;
                    finalLabel = $"{baseLabel} ({count})";
                }
                else
                {
                    usedLabels[baseLabel] = 1;
                }

                results[i] = new AttributeColumnInfo
                {
                    Key = key,
                    Label = finalLabel
                };
            }

            return results;
        }

        private static string ResolveAttributeDisplayName(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return key;
            }

            string[] guids = AssetDatabase.FindAssets("t:AttributeScriptableObject");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AttributeScriptableObject attribute = AssetDatabase.LoadAssetAtPath<AttributeScriptableObject>(path);
                if (attribute == null)
                {
                    continue;
                }

                if (attribute.UniqueId != key)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(attribute.Name))
                {
                    if (attribute.name == "Attribute.MaxHP")
                    {
                        return "MaxHP";
                    }

                    if (attribute.name == "Attribute.HP")
                    {
                        return "HP";
                    }

                    return attribute.Name;
                }

                if (attribute.name == "Attribute.MaxHP")
                {
                    return "MaxHP";
                }

                if (attribute.name == "Attribute.HP")
                {
                    return "HP";
                }

                return attribute.name;
            }

            return key;
        }

        private static int FindNamedEntryIndex(SerializedProperty entriesProp, string key)
        {
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                SerializedProperty item = entriesProp.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("key").stringValue == key)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void EnsureNamedEntry(
            SerializedProperty entriesProp,
            string key,
            out SerializedProperty valueProp)
        {
            valueProp = null;

            int index = FindNamedEntryIndex(entriesProp, key);
            if (index < 0)
            {
                index = entriesProp.arraySize;
                entriesProp.InsertArrayElementAtIndex(index);
                SerializedProperty item = entriesProp.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("key").stringValue = key;
                item.FindPropertyRelative("value").floatValue = 0f;
            }

            valueProp = entriesProp.GetArrayElementAtIndex(index).FindPropertyRelative("value");
        }

        private static void SortByCompanyId(SerializedProperty companiesProp)
        {
            var rows = new System.Collections.Generic.List<RowSnapshot>();
            for (int i = 0; i < companiesProp.arraySize; i++)
            {
                SerializedProperty row = companiesProp.GetArrayElementAtIndex(i);
                rows.Add(RowSnapshot.From(row));
            }

            rows.Sort((a, b) => string.Compare(a.CompanyId, b.CompanyId, System.StringComparison.Ordinal));
            companiesProp.arraySize = rows.Count;

            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].ApplyTo(companiesProp.GetArrayElementAtIndex(i));
            }
        }

        private sealed class RowSnapshot
        {
            public string CompanyId;
            public System.Collections.Generic.List<NamedValueSnapshot> Attributes;
            public System.Collections.Generic.List<NamedValueSnapshot> Values;

            public static RowSnapshot From(SerializedProperty row)
            {
                return new RowSnapshot
                {
                    CompanyId = row.FindPropertyRelative("companyId").stringValue,
                    Attributes = ReadNamedValues(row.FindPropertyRelative("attributes")),
                    Values = ReadNamedValues(row.FindPropertyRelative("values"))
                };
            }

            public void ApplyTo(SerializedProperty row)
            {
                row.FindPropertyRelative("companyId").stringValue = CompanyId ?? string.Empty;
                WriteNamedValues(row.FindPropertyRelative("attributes"), Attributes);
                WriteNamedValues(row.FindPropertyRelative("values"), Values);
            }

            private static System.Collections.Generic.List<NamedValueSnapshot> ReadNamedValues(SerializedProperty prop)
            {
                var list = new System.Collections.Generic.List<NamedValueSnapshot>();
                if (prop == null)
                {
                    return list;
                }

                for (int i = 0; i < prop.arraySize; i++)
                {
                    SerializedProperty item = prop.GetArrayElementAtIndex(i);
                    list.Add(new NamedValueSnapshot
                    {
                        Key = item.FindPropertyRelative("key").stringValue,
                        Value = item.FindPropertyRelative("value").floatValue
                    });
                }

                return list;
            }

            private static void WriteNamedValues(
                SerializedProperty prop,
                System.Collections.Generic.List<NamedValueSnapshot> values)
            {
                if (prop == null)
                {
                    return;
                }

                prop.arraySize = values != null ? values.Count : 0;
                if (values == null)
                {
                    return;
                }

                for (int i = 0; i < values.Count; i++)
                {
                    SerializedProperty item = prop.GetArrayElementAtIndex(i);
                    item.FindPropertyRelative("key").stringValue = values[i].Key ?? string.Empty;
                    item.FindPropertyRelative("value").floatValue = values[i].Value;
                }
            }
        }

        private sealed class NamedValueSnapshot
        {
            public string Key;
            public float Value;
        }

        private sealed class AttributeColumnInfo
        {
            public string Key;
            public string Label;
        }
    }
}
