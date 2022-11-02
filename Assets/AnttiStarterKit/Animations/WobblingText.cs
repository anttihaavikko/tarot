using System;
using AnttiStarterKit.Extensions;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AnttiStarterKit.Animations
{
    public class WobblingText : MonoBehaviour
    {
        [SerializeField] private float amount = 0.01f;
        [SerializeField] private float speed = 2f;
        
        private float baseOffset;
        private TMP_Text textField;

        public void Clone(WobblingText wobble)
        {
            amount = wobble.amount;
            speed = wobble.speed;
        }

        private void Awake()
        {
            baseOffset = Random.value * 0.2f;
            textField = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            textField.ForceMeshUpdate();
        
            var mesh = textField.mesh;
            var verts = mesh.vertices;

            for (var i = 0; i < textField.text.Length; i++)
            {
                OffsetCharacter(i, textField.textInfo.characterInfo[i], ref verts);
            }

            mesh.vertices = verts;

            if (textField.canvasRenderer)
            {
                textField.canvasRenderer.SetMesh(mesh);
            }
        }
        
        private void OffsetCharacter(int index, TMP_CharacterInfo info, ref Vector3[] verts)
        {
            var offset = Vector3.zero.WhereY(Mathf.Sin(Time.time * speed + index * 0.5f + baseOffset) * amount);
            verts[info.vertexIndex] += offset;
            verts[info.vertexIndex + 1] += offset;
            verts[info.vertexIndex + 2] += offset;
            verts[info.vertexIndex + 3] += offset;
        }
    }
}