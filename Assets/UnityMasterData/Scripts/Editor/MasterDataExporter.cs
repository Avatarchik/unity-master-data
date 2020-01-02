// Copyright 2019 Shintaro Tanikawa
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityMasterData.Editor.Interfaces;
using UnityMasterData.Interfaces;
using UnityMasterData.Libs;

namespace UnityMasterData.Editor {

    /// <summary>
    /// The tool to export data assets as a ScriptableObject in your project.
    /// Before export, you need data class scripts that generated by MasterDataClassGenerator.
    /// The destination of data assets is the specified path that DAO scripts have.
    /// </summary>
    public static class MasterDataExporter {

        /// <summary>
        /// Export master data assets in your project with using all of classes of IMasterDataExporter that searched by Reflection.
        /// </summary>
        public static void Export () {
            foreach (var type in Assembly.Load ("Assembly-CSharp-Editor").GetTypes ().Distinct ()) {
                if (!type.IsInterface && type.GetInterfaces ().Contains (typeof (IMasterDataExporter))) {
                    (Activator.CreateInstance (type) as IMasterDataExporter)?.Export ();
                }
            }
        }

        /// <summary>
        /// Export master data assets as a ScriptableObject.
        /// 
        /// *** Don't invoke manually, since this is invoked just by generated scripts. ***
        /// </summary>
        /// <param name="sourcePath">The path of a specified excel data file</param>
        /// <param name="destPath">The path of the destination to export</param>
        /// <param name="baseName">The name of a specified excel file without a extension.</param>
        /// <param name="name">The name of a specified excel sheet</param>
        /// <typeparam name="T">MasterDataTransferObject type</typeparam>
        /// <typeparam name="E">IValueObject type</typeparam>
        /// <typeparam name="K">Key type</typeparam>
        /// <returns></returns>
        public static void Export<T, E, K> (string sourcePath, string destPath, string baseName, string name) where T : MasterDataTransferObject<E, K> where E : IValueObject<K>, new () {
            Excel excel;
            if (!Excel.TryRead (sourcePath, out excel)) {
                return;
            }

            Debug.Log ("EXPORT: " + baseName + "." + name);
            var path = DataAssetPath (destPath, baseName, name);
            var asset = AssetDatabase.LoadAssetAtPath<T> (path);
            if (asset == null) {
                asset = ScriptableObject.CreateInstance<T> ();
                Directory.CreateDirectory (Path.GetDirectoryName (path));
                AssetDatabase.CreateAsset (asset, path);
            } else {
                asset.list.Clear ();
            }
            var sheet = excel.GetSheet (name);
            var nameCells = sheet.GetRowCells ((int) MasterDataClassGenerator.RowSettings.KeyName);
            for (int r = (int) MasterDataClassGenerator.RowSettings.Type + 1; r < sheet.GetColumnCells ((int) MasterDataClassGenerator.ColumnSettings.Key).Length; r++) {
                asset.list.Add (CreateElement<E, K> (nameCells, sheet.GetRowCells (r)));
            }
            EditorUtility.SetDirty (asset);
        }

        private static E CreateElement<E, K> (Excel.Cell[] nameCells, Excel.Cell[] valueCells) where E : IValueObject<K>, new () {
            var e = new E ();
            foreach (var field in typeof (E).GetFields (BindingFlags.Instance | BindingFlags.Public)) {
                string value = null;
                var index = Array.FindIndex (nameCells, o => o.value == field.Name);
                if (index >= 0) {
                    value = valueCells[index].value;
                }
                var type = field.FieldType;
                if (string.IsNullOrEmpty (value) && type != typeof (string)) {
                    Debug.LogWarning ("Skipped to set blank value. " + field.Name);
                    continue;
                }
                if (type.IsEnum) {
                    type = Enum.GetUnderlyingType (type);
                }
                object v;
                if (TryParseType (type, value, out v)) {
                    field.SetValue (e, v);
                } else {
                    Debug.LogWarning ("Attempted to load with an invalid type or an invalid value. " + type.Name + " : " + field.Name);
                }
            }
            return e;
        }

        private static bool TryParseType (Type type, string value, out object result) {
            bool ret = false;
            if (type == typeof (int)) {
                int buf;
                ret = int.TryParse (value, out buf);
                result = buf;
            } else if (type == typeof (uint)) {
                uint buf;
                ret = uint.TryParse (value, out buf);
                result = buf;
            } else if (type == typeof (byte)) {
                byte buf;
                ret = byte.TryParse (value, out buf);
                result = buf;
            } else if (type == typeof (char)) {
                char buf;
                ret = char.TryParse (value, out buf);
                result = buf;
            } else if (type == typeof (long)) {
                long buf;
                ret = long.TryParse (value, out buf);
                result = buf;
            } else if (type == typeof (ulong)) {
                ulong buf;
                ret = ulong.TryParse (value, out buf);
                result = buf;
            } else if (type == typeof (short)) {
                short buf;
                ret = short.TryParse (value, out buf);
                result = buf;
            } else if (type == typeof (ushort)) {
                ushort buf;
                ret = ushort.TryParse (value, out buf);
                result = buf;
            } else if (type == typeof (float)) {
                float buf;
                ret = float.TryParse (value, out buf);
                result = buf;
            } else if (type == typeof (double)) {
                double buf;
                ret = double.TryParse (value, out buf);
                result = buf;
            } else if (type == typeof (bool)) {
                bool buf;
                ret = bool.TryParse (value, out buf);
                result = buf;
            } else if (type == typeof (string)) {
                ret = true;
                result = value;
            } else {
                Debug.LogError (type.FullName + " is unsupported to parse.");
                result = null;
            }
            return ret;
        }

        private static string DataAssetPath (string destPath, string baseName, string name) {
            return Path.Combine (destPath, string.Format ("{0}/{1}.asset", baseName, name));
        }
    }
}