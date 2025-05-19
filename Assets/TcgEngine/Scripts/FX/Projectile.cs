using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using TcgEngine.UI;

namespace TcgEngine.FX
{

    public class Projectile : MonoBehaviour
    {
        public float speed = 10f;
        public float duration = 4f;
        public GameObject explode_fx;
        public AudioClip explode_audio;

        [HideInInspector]

        private Transform source;
        private Transform target;
        private Vector3 source_offset;
        private Vector3 target_offset;
        private float timer = 0f;


        void Update()
        {
            timer += Time.deltaTime;

            if (source == null || target == null)
            {
                Destroy(gameObject);
                return;
            }

            if (timer > duration)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 spos = transform.position;
            Vector3 tpos = target.position + target_offset;
            Vector3 dir = (tpos - spos);
            transform.position += dir.normalized * Mathf.Min(dir.magnitude, 1f) * speed * Time.deltaTime;
            transform.rotation = GetFXRotation(dir.normalized);

            if (dir.magnitude < 0.2f)
            {
                FXTool.DoFX(explode_fx, target.position);
                AudioTool.Get().PlaySFX("fx", explode_audio);
                Destroy(gameObject);
            }
        }

        public void SetSource(Transform source)
        {
            this.source = source;
            transform.position = source.position;
        }

        public void SetSource(Transform source, Vector3 offset)
        {
            this.source = source;
            source_offset = offset;
            transform.position = source.position + source_offset;
        }

        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        public void SetTarget(Transform target, Vector3 offset)
        {
            this.target = target;
            target_offset = offset;
        }

        private static Quaternion GetFXRotation(Vector3 dir)
        {
            GameBoard board = GameBoard.Get();
            Vector3 facing = board != null ? board.transform.forward : Vector3.forward;
            return Quaternion.LookRotation(facing, dir);
        }
    }
}