using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AnttiStarterKit.Managers;
using AnttiStarterKit.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(SoundComposition))]
    public class SoundCompositionEditor : UnityEditor.Editor
    {
        private Vector2 _listScroll, _playerScroll;
        private Texture2D _black;
        private string _filter = "";

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            var pix = new Color[width * height];
 
            for(var i = 0; i < pix.Length; i++)
                pix[i] = col;
 
            var result = new Texture2D(width, height);
            
            result.SetPixels(pix);
            result.Apply();
 
            return result;
        }

        private void OnEnable()
        {
            var amount = 0.15f;
            _black = MakeTex(600, 1, new Color(amount, amount, amount));
            // _object = (SoundComposition)target;

            var rows = serializedObject.FindProperty("rows");

            // if (serializedObject.FindProperty("rows") == null)
            // {
            //     serializedObject.FindProperty("rows").arra
            //     _object.rows = new List<SoundCompositionRow>();
            //     return;
            // }

            // _sounds = _object.rows.Select(r => r.clip).ToList();
            // _soundVolumes = _object.rows.Select(r => r.volume).ToList();
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            // DrawDefaultInspector();
            
            serializedObject.Update();
            var rows =  serializedObject.FindProperty("rows");

            EditorGUILayout.BeginVertical(new GUIStyle
            {
                fixedHeight = 350
            });

            DrawFilter();

            _listScroll = GUILayout.BeginScrollView(_listScroll);
            EditorGUILayout.Space();
            
            var eventStyle = new GUIStyle
            {
                normal = new GUIStyleState
                {
                    background = _black
                }
            };
            
            var clips = GetAtPath<AudioClip>("Sounds");
            var row = 0;
            foreach (var clip in clips)
            {
                if (_filter.Length > 0 && !clip.name.Contains(_filter)) continue;
                
                EditorGUILayout.BeginHorizontal(row % 2 == 0 ? eventStyle : new GUIStyle());
                EditorGUILayout.LabelField(clip.name);
                
                if (GUILayout.Button("►", GUILayout.MaxWidth(20.0f)))
                {
                    if (AudioManager.Instance)
                    {
                        AudioManager.Instance.PlayEffectAt(clip, Vector3.zero, 1f);
                    }
                }

                if (GUILayout.Button("Add", GUILayout.MaxWidth(70.0f)))
                {
                    var index = serializedObject.FindProperty("rows").arraySize;
                    serializedObject.FindProperty("rows").InsertArrayElementAtIndex(index);
                    serializedObject.FindProperty("rows").GetArrayElementAtIndex(index).FindPropertyRelative("clip").objectReferenceInstanceIDValue = clip.GetInstanceID();
                    serializedObject.FindProperty("rows").GetArrayElementAtIndex(index).FindPropertyRelative("volume").floatValue = 1f;
                    serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.EndHorizontal();

                row++;
            }
            
            EditorGUILayout.Space();
            GUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
            
            DrawPlayer();
        }

        private void DrawFilter()
        {
            EditorGUILayout.BeginHorizontal();

            _filter = EditorGUILayout.TextField(_filter);
            
            if (GUILayout.Button("Clear", GUILayout.MaxWidth(120.0f)))
            {
                _filter = "";
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPlayer()
        {
            EditorGUILayout.BeginVertical(new GUIStyle
            {
                padding = new RectOffset(0, 0, 20, 0)
            });

            for (var i = 0; i < serializedObject.FindProperty("rows").arraySize; i++)
            {
                var row = serializedObject.FindProperty("rows").GetArrayElementAtIndex(i);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("►", GUILayout.MaxWidth(20.0f)))
                {
                    if (AudioManager.Instance)
                    {
                        AudioManager.Instance.PlayEffectAt((AudioClip)row.FindPropertyRelative("clip").objectReferenceValue, Vector3.zero, row.FindPropertyRelative("volume").floatValue);
                    }
                }
                GUILayout.Label(row.FindPropertyRelative("clip").objectReferenceValue.name, EditorStyles.boldLabel, GUILayout.Width(80.0f));
                row.FindPropertyRelative("volume").floatValue = EditorGUILayout.Slider("", row.FindPropertyRelative("volume").floatValue, 0f, 5f);
                if (GUILayout.Button("X", GUILayout.MaxWidth(20.0f)))
                {
                    serializedObject.FindProperty("rows").DeleteArrayElementAtIndex(i);
                }
                GUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("► Play"))
            {
                ((SoundComposition)serializedObject.targetObject).Play();
            }

            EditorGUILayout.EndVertical();
            // GUILayout.EndScrollView();
            
            serializedObject.ApplyModifiedProperties();
        }

        public static T[] GetAtPath<T> (string path) {
       
            var al = new ArrayList();
            var fileEntries = Directory.GetFiles(Application.dataPath+"/"+path);
            foreach(var fileName in fileEntries)
            {
                var index = fileName.LastIndexOf("/", StringComparison.Ordinal);
                var localPath = "Assets/" + path;
           
                if (index > 0)
                    localPath += fileName.Substring(index);
               
                // var t = Resources.LoadAssetAtPath(localPath, typeof(T));
                var t = AssetDatabase.LoadAssetAtPath(localPath, typeof(T));

                if(t != null)
                    al.Add(t);
            }
            var result = new T[al.Count];
            for(var i=0;i<al.Count;i++)
                result[i] = (T)al[i];
           
            return result;
        }
    }
}