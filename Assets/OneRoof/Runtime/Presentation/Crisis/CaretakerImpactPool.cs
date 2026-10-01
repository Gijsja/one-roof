using UnityEngine;

namespace OneRoof.Presentation.Crisis
{
    /// <summary>Bounded, reusable radial impact marks; no combat state or per-effect update loops.</summary>
    internal sealed class CaretakerImpactPool
    {
        private readonly SpriteRenderer[] _marks = new SpriteRenderer[8];
        private readonly float[] _age = new float[8];
        private readonly Texture2D _texture;
        private readonly Sprite _sprite;
        private int _next;
        public CaretakerImpactPool(Transform parent, Material material)
        {
            _texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                name = "Caretaker_ImpactStar",
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[32 * 32];
            for (var y = 0; y < 32; y++)
                for (var x = 0; x < 32; x++)
                {
                    var d = new Vector2(x - 15.5f, y - 15.5f) / 16;
                    var angle = Mathf.Atan2(d.y, d.x);
                    var edge = .25f + .65f * Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 4)), 12);
                    pixels[y * 32 + x] = new Color(1, 1, 1, Mathf.Clamp01((edge - d.magnitude) * 14));
                }

            _texture.SetPixels(pixels);
            _texture.Apply(false, true);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 32);
            for (var i = 0; i < _marks.Length; i++)
            {
                var go = new GameObject("Impact " + i);
                go.transform.SetParent(parent, false);
                var mark = go.AddComponent<SpriteRenderer>();
                mark.sprite = _sprite;
                mark.sharedMaterial = material;
                mark.sortingOrder = 900;
                mark.enabled = false;
                _marks[i] = mark;
                _age[i] = 1;
            }
        }

        public void Emit(Vector3 position, bool caretakerHit)
        {
            var i = _next++ % _marks.Length;
            _marks[i].transform.position = position;
            _marks[i].transform.rotation = Quaternion.Euler(0, 0, i * 23);
            _marks[i].color = caretakerHit ? new Color(1, .27f, .2f) : new Color(1, .84f, .35f);
            _marks[i].enabled = true;
            _age[i] = 0;
        }

        public void Tick(float delta)
        {
            for (var i = 0; i < _marks.Length; i++)
            {
                if (!_marks[i].enabled)
                    continue;
                _age[i] += delta;
                var t = _age[i] / .22f;
                if (t >= 1)
                {
                    _marks[i].enabled = false;
                    continue;
                }

                _marks[i].transform.localScale = Vector3.one * Mathf.Lerp(.28f, .72f, t);
                var color = _marks[i].color;
                color.a = 1 - t;
                _marks[i].color = color;
            }
        }

        public void Dispose()
        {
            Object.Destroy(_sprite);
            Object.Destroy(_texture);
        }
    }
}
