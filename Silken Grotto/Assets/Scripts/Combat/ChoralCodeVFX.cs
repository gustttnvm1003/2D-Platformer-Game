using System.Collections;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// 100% code-driven 2D vector VFX renderer (zero external image files required!):
    /// Draws crisp crescent slash arcs, piercing lance thrusts, telegraph danger glints,
    /// metallic parry/block spark bursts, crossed guard stance indicators, and charge/skid ground dust
    /// directly via Unity LineRenderer ribbons.
    /// </summary>
    public static class ChoralCodeVFX
    {
        private static Material _sharedLineMaterial;

        public static Material GetLineMaterial()
        {
            if (_sharedLineMaterial == null)
            {
                Shader s = Shader.Find("Sprites/Default");
                if (s != null)
                {
                    _sharedLineMaterial = new Material(s);
                }
            }
            return _sharedLineMaterial;
        }

        private static LineRenderer CreateLine(Transform parent, string name, int sortingOrder, bool worldSpace)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = worldSpace;
            lr.sortingLayerName = GameConstants.SortVFX;
            lr.sortingOrder = sortingOrder;
            Material mat = GetLineMaterial();
            if (mat != null) lr.sharedMaterial = mat;
            return lr;
        }

        /// <summary>
        /// Draws a crisp curved crescent slash arc purely from code (WideHeavySlash, QuickSlash, HeavySwing, StepStrike).
        /// </summary>
        public static void SpawnSlashArc(Vector3 center, int facingDir, float radiusX, float radiusY, float arcDeg, float startWidth, Color color, float duration = 0.14f)
        {
            GameObject root = new GameObject("CodeVFX_SlashArc");
            root.transform.position = center;
            LineRenderer lr = root.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.sortingLayerName = GameConstants.SortVFX;
            lr.sortingOrder = 16;
            Material mat = GetLineMaterial();
            if (mat != null) lr.sharedMaterial = mat;

            int pts = 14;
            lr.positionCount = pts;
            lr.startWidth = startWidth * 0.25f;
            lr.endWidth = startWidth;
            lr.startColor = new Color(color.r, color.g, color.b, color.a * 0.35f);
            lr.endColor = color;

            float dir = facingDir >= 0 ? 1f : -1f;
            float halfRad = arcDeg * 0.5f * Mathf.Deg2Rad;
            for (int i = 0; i < pts; i++)
            {
                float u = (float)i / (pts - 1);
                float ang = Mathf.Lerp(halfRad, -halfRad, u);
                float px = Mathf.Cos(ang) * radiusX * dir;
                float py = Mathf.Sin(ang) * radiusY;
                lr.SetPosition(i, new Vector3(px, py, 0f));
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { lr }, color, startWidth, duration, new Vector3(dir * 1.4f, 0f, 0f), 1.12f);
        }

        /// <summary>
        /// Draws a razor-sharp piercing lance thrust streak (DoubleThrustCombo, ForwardLunge).
        /// </summary>
        public static void SpawnThrustStreak(Vector3 origin, int facingDir, float length, float width, Color color, float duration = 0.13f)
        {
            GameObject root = new GameObject("CodeVFX_ThrustStreak");
            root.transform.position = origin;
            float dir = facingDir >= 0 ? 1f : -1f;

            LineRenderer core = CreateLine(root.transform, "Core", 17, false);
            core.positionCount = 2;
            core.startWidth = width * 0.22f;
            core.endWidth = width;
            core.startColor = new Color(color.r, color.g, color.b, 0.25f);
            core.endColor = Color.white;
            core.SetPosition(0, Vector3.zero);
            core.SetPosition(1, new Vector3(dir * length, 0f, 0f));

            LineRenderer outer = CreateLine(root.transform, "Outer", 16, false);
            outer.positionCount = 2;
            outer.startWidth = width * 0.5f;
            outer.endWidth = width * 1.85f;
            outer.startColor = new Color(color.r, color.g, color.b, 0.15f);
            outer.endColor = new Color(color.r, color.g, color.b, 0.88f);
            outer.SetPosition(0, new Vector3(-dir * 0.25f, 0f, 0f));
            outer.SetPosition(1, new Vector3(dir * (length * 1.06f), 0f, 0f));

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { core, outer }, color, width, duration, new Vector3(dir * 2.8f, 0f, 0f), 1.06f);
        }

        /// <summary>
        /// Draws an unmistakable Silksong-style golden-white 4-ray danger telegraph glint + subtle halo ring.
        /// </summary>
        public static void SpawnTelegraphGlint(Vector3 position, Color color, float radius = 0.65f, float duration = 0.28f)
        {
            GameObject root = new GameObject("CodeVFX_TelegraphGlint");
            root.transform.position = position;

            LineRenderer horiz = CreateLine(root.transform, "RayH", 18, false);
            horiz.positionCount = 3;
            horiz.startWidth = 0.04f;
            horiz.endWidth = 0.04f;
            horiz.startColor = color;
            horiz.endColor = color;
            horiz.SetPosition(0, new Vector3(-radius * 1.35f, 0f, 0f));
            horiz.SetPosition(1, Vector3.zero);
            horiz.SetPosition(2, new Vector3(radius * 1.35f, 0f, 0f));

            LineRenderer vert = CreateLine(root.transform, "RayV", 18, false);
            vert.positionCount = 3;
            vert.startWidth = 0.04f;
            vert.endWidth = 0.04f;
            vert.startColor = Color.white;
            vert.endColor = Color.white;
            vert.SetPosition(0, new Vector3(0f, -radius * 0.95f, 0f));
            vert.SetPosition(1, Vector3.zero);
            vert.SetPosition(2, new Vector3(0f, radius * 0.95f, 0f));

            LineRenderer ring = CreateLine(root.transform, "Ring", 17, false);
            int segs = 17;
            ring.positionCount = segs;
            ring.startWidth = 0.045f;
            ring.endWidth = 0.045f;
            ring.startColor = new Color(color.r, color.g, color.b, 0.85f);
            ring.endColor = new Color(color.r, color.g, color.b, 0.85f);
            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius * 0.52f, Mathf.Sin(a) * radius * 0.52f, 0f));
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { horiz, vert, ring }, color, 0.055f, duration, Vector3.zero, 1.45f);
        }

        /// <summary>
        /// Draws sharp radiating silver-gold metallic clang sparks when Reed / Grand Reed blocks a frontal strike.
        /// </summary>
        public static void SpawnBlockSparks(Vector3 position, int normalDirX)
        {
            GameObject root = new GameObject("CodeVFX_BlockSparks");
            root.transform.position = position;
            int rayCount = 7;
            LineRenderer[] lines = new LineRenderer[rayCount + 1];
            float baseAngle = normalDirX >= 0 ? 0f : 180f;

            for (int i = 0; i < rayCount; i++)
            {
                LineRenderer lr = CreateLine(root.transform, "Spark_" + i, 19, false);
                lr.positionCount = 2;
                float angDeg = baseAngle + Random.Range(-78f, 78f);
                float rad = angDeg * Mathf.Deg2Rad;
                float len = Random.Range(0.45f, 1.05f);
                lr.startWidth = 0.065f;
                lr.endWidth = 0.015f;
                Color c = (i % 2 == 0) ? new Color(1.0f, 0.95f, 0.72f, 1f) : Color.white;
                lr.startColor = c;
                lr.endColor = new Color(c.r, c.g, c.b, 0.25f);
                lr.SetPosition(0, new Vector3(Mathf.Cos(rad) * 0.08f, Mathf.Sin(rad) * 0.08f, 0f));
                lr.SetPosition(1, new Vector3(Mathf.Cos(rad) * len, Mathf.Sin(rad) * len, 0f));
                lines[i] = lr;
            }

            // Shield impact ring
            LineRenderer ring = CreateLine(root.transform, "ParryRing", 18, false);
            int segs = 15;
            ring.positionCount = segs;
            ring.startWidth = 0.07f;
            ring.endWidth = 0.07f;
            Color gold = new Color(1.0f, 0.90f, 0.48f, 0.95f);
            ring.startColor = gold;
            ring.endColor = gold;
            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.34f, Mathf.Sin(a) * 0.48f, 0f));
            }
            lines[rayCount] = ring;

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(lines, gold, 0.065f, 0.18f, new Vector3(normalDirX * 1.1f, 0.25f, 0f), 1.38f);
        }

        /// <summary>
        /// Draws code-driven ground dust kick-up arcs for Hornhead ChargeWindup, ChargeDash, and SkidStop.
        /// </summary>
        public static void SpawnChargeDust(Vector3 feetPosition, int facingDir, bool isSkid)
        {
            GameObject root = new GameObject("CodeVFX_ChargeDust");
            root.transform.position = feetPosition + new Vector3(0f, 0.14f, 0f);
            float backDir = isSkid ? (facingDir >= 0 ? 1f : -1f) : (facingDir >= 0 ? -1f : 1f);

            int puffs = 4;
            LineRenderer[] lines = new LineRenderer[puffs];
            Color dustCol = new Color(0.92f, 0.90f, 0.84f, 0.85f);
            for (int i = 0; i < puffs; i++)
            {
                LineRenderer lr = CreateLine(root.transform, "Dust_" + i, 13, false);
                int segs = 7;
                lr.positionCount = segs;
                lr.startWidth = 0.11f;
                lr.endWidth = 0.03f;
                lr.startColor = dustCol;
                lr.endColor = new Color(dustCol.r, dustCol.g, dustCol.b, 0.12f);

                float ox = backDir * (0.12f + i * 0.22f);
                float oy = i * 0.08f;
                float r = 0.16f + i * 0.06f;
                for (int s = 0; s < segs; s++)
                {
                    float u = (float)s / (segs - 1);
                    float ang = Mathf.Lerp(0.15f, Mathf.PI * 1.15f, u);
                    lr.SetPosition(s, new Vector3(ox + Mathf.Cos(ang) * r * backDir, oy + Mathf.Sin(ang) * r, 0f));
                }
                lines[i] = lr;
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(lines, dustCol, 0.10f, isSkid ? 0.28f : 0.22f, new Vector3(backDir * (isSkid ? 2.4f : 3.2f), 0.75f, 0f), 1.35f);
        }

        /// <summary>
        /// Builds a crisp crossed golden-silver guard stance indicator attached directly to FrontBlockBox.
        /// </summary>
        public static GameObject CreateGuardStanceVisual(Transform parent, Vector2 size, Color tint)
        {
            GameObject go = new GameObject("CodeVFX_GuardStance");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            float hw = Mathf.Max(0.22f, size.x * 0.42f);
            float hh = Mathf.Max(0.45f, size.y * 0.46f);

            // Primary angled parry blade gleam
            LineRenderer bladeA = CreateLine(go.transform, "ParryBladeA", 15, false);
            bladeA.positionCount = 2;
            bladeA.startWidth = 0.085f;
            bladeA.endWidth = 0.03f;
            bladeA.startColor = Color.white;
            bladeA.endColor = tint;
            bladeA.SetPosition(0, new Vector3(-hw * 0.55f, -hh * 0.72f, 0f));
            bladeA.SetPosition(1, new Vector3(hw * 0.72f, hh * 0.78f, 0f));

            // Curved protective frontal arc
            LineRenderer shieldArc = CreateLine(go.transform, "ShieldArc", 14, false);
            int segs = 9;
            shieldArc.positionCount = segs;
            shieldArc.startWidth = 0.04f;
            shieldArc.endWidth = 0.04f;
            Color soft = new Color(tint.r, tint.g, tint.b, 0.72f);
            shieldArc.startColor = soft;
            shieldArc.endColor = soft;
            for (int i = 0; i < segs; i++)
            {
                float u = (float)i / (segs - 1);
                float a = Mathf.Lerp(-1.05f, 1.05f, u);
                shieldArc.SetPosition(i, new Vector3(Mathf.Cos(a) * hw * 0.85f, Mathf.Sin(a) * hh * 0.88f, 0f));
            }

            go.SetActive(false);
            return go;
        }

        /// <summary>
        /// Draws code-driven mossy dust & leaf-spark burst on downward pogo bounces (Mossmir / Aknid / Mossgrub).
        /// </summary>
        public static void SpawnMossPogoBurst(Vector3 position, Color mossTint)
        {
            GameObject root = new GameObject("CodeVFX_MossPogoBurst");
            root.transform.position = position;
            int count = 6;
            LineRenderer[] lines = new LineRenderer[count + 1];

            for (int i = 0; i < count; i++)
            {
                LineRenderer lr = CreateLine(root.transform, "MossLeaf_" + i, 17, false);
                lr.positionCount = 2;
                float angDeg = 200f + (i / (float)(count - 1)) * 140f + Random.Range(-10f, 10f);
                float rad = angDeg * Mathf.Deg2Rad;
                float len = Random.Range(0.32f, 0.72f);
                lr.startWidth = 0.085f;
                lr.endWidth = 0.02f;
                Color c = (i % 2 == 0) ? mossTint : new Color(0.92f, 0.99f, 0.84f, 0.96f);
                lr.startColor = c;
                lr.endColor = new Color(c.r, c.g, c.b, 0.15f);
                lr.SetPosition(0, new Vector3(Mathf.Cos(rad) * 0.12f, Mathf.Sin(rad) * 0.12f, 0f));
                lr.SetPosition(1, new Vector3(Mathf.Cos(rad) * len, Mathf.Sin(rad) * len, 0f));
                lines[i] = lr;
            }

            // Crisp elastic bounce ring
            LineRenderer ring = CreateLine(root.transform, "PogoRing", 16, false);
            int segs = 15;
            ring.positionCount = segs;
            ring.startWidth = 0.06f;
            ring.endWidth = 0.06f;
            ring.startColor = Color.white;
            ring.endColor = Color.white;
            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.42f, Mathf.Sin(a) * 0.18f, 0f));
            }
            lines[count] = ring;

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(lines, mossTint, 0.075f, 0.22f, new Vector3(0f, -0.8f, 0f), 1.42f);
        }

        /// <summary>
        /// Short crisp surprised/provoked exclamation pop + dust puff for Mossgrub ProvokedAlert (0.25s).
        /// </summary>
        public static void SpawnProvokedAlertPop(Vector3 position)
        {
            GameObject root = new GameObject("CodeVFX_ProvokedAlert");
            root.transform.position = position;
            Color col = new Color(1.0f, 0.94f, 0.56f, 0.98f);

            LineRenderer stem = CreateLine(root.transform, "ExclaimStem", 18, false);
            stem.positionCount = 2;
            stem.startWidth = 0.085f;
            stem.endWidth = 0.05f;
            stem.startColor = col;
            stem.endColor = Color.white;
            stem.SetPosition(0, new Vector3(0f, 0.18f, 0f));
            stem.SetPosition(1, new Vector3(0f, 0.58f, 0f));

            LineRenderer dot = CreateLine(root.transform, "ExclaimDot", 18, false);
            dot.positionCount = 2;
            dot.startWidth = 0.085f;
            dot.endWidth = 0.085f;
            dot.startColor = Color.white;
            dot.endColor = col;
            dot.SetPosition(0, new Vector3(0f, 0.02f, 0f));
            dot.SetPosition(1, new Vector3(0f, 0.10f, 0f));

            LineRenderer sparkL = CreateLine(root.transform, "SparkL", 17, false);
            sparkL.positionCount = 2;
            sparkL.startWidth = 0.045f;
            sparkL.endWidth = 0.015f;
            sparkL.startColor = col;
            sparkL.endColor = col;
            sparkL.SetPosition(0, new Vector3(-0.14f, 0.34f, 0f));
            sparkL.SetPosition(1, new Vector3(-0.40f, 0.52f, 0f));

            LineRenderer sparkR = CreateLine(root.transform, "SparkR", 17, false);
            sparkR.positionCount = 2;
            sparkR.startWidth = 0.045f;
            sparkR.endWidth = 0.015f;
            sparkR.startColor = col;
            sparkR.endColor = col;
            sparkR.SetPosition(0, new Vector3(0.14f, 0.34f, 0f));
            sparkR.SetPosition(1, new Vector3(0.40f, 0.52f, 0f));

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { stem, dot, sparkL, sparkR }, col, 0.07f, 0.25f, new Vector3(0f, 0.9f, 0f), 1.25f);
        }

        /// <summary>
        /// Joyful sparkling burst when a carried Flea is rescued from Aknid!
        /// </summary>
        public static void SpawnJoyfulFleaSparkles(Vector3 position)
        {
            GameObject root = new GameObject("CodeVFX_FleaSparkles");
            root.transform.position = position;
            int rays = 8;
            LineRenderer[] lines = new LineRenderer[rays];
            Color gold = new Color(1.0f, 0.95f, 0.62f, 0.98f);
            Color mint = new Color(0.76f, 1.0f, 0.88f, 0.95f);

            for (int i = 0; i < rays; i++)
            {
                LineRenderer lr = CreateLine(root.transform, "Sparkle_" + i, 18, false);
                lr.positionCount = 2;
                float a = (i / (float)rays) * Mathf.PI * 2f + Random.Range(-0.18f, 0.18f);
                float r0 = 0.12f;
                float r1 = Random.Range(0.42f, 0.88f);
                Color c = (i % 2 == 0) ? gold : mint;
                lr.startWidth = 0.06f;
                lr.endWidth = 0.015f;
                lr.startColor = c;
                lr.endColor = new Color(c.r, c.g, c.b, 0.18f);
                lr.SetPosition(0, new Vector3(Mathf.Cos(a) * r0, Mathf.Sin(a) * r0, 0f));
                lr.SetPosition(1, new Vector3(Mathf.Cos(a) * r1, Mathf.Sin(a) * r1, 0f));
                lines[i] = lr;
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(lines, gold, 0.06f, 0.36f, new Vector3(0f, 1.4f, 0f), 1.55f);
        }

        /// <summary>
        /// Builds a pure code-drawn suspended Mossberry or Captive Flea visual hanging beneath Aknid (zero image files!).
        /// </summary>
        public static GameObject CreateAknidCarryVisual(Transform parent, AknidCarryType carryType)
        {
            if (carryType == AknidCarryType.None || parent == null) return null;

            GameObject go = new GameObject("AknidCarryVisual_" + carryType);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, -0.58f, 0f);

            // Delicate silk tether connecting Aknid's legs to the payload
            LineRenderer tether = CreateLine(go.transform, "SilkTether", 14, false);
            tether.positionCount = 2;
            tether.startWidth = 0.025f;
            tether.endWidth = 0.025f;
            Color silk = new Color(0.96f, 0.98f, 0.92f, 0.80f);
            tether.startColor = silk;
            tether.endColor = silk;
            tether.SetPosition(0, new Vector3(0f, 0.34f, 0f));
            tether.SetPosition(1, new Vector3(0f, 0.08f, 0f));

            if (carryType == AknidCarryType.Berry)
            {
                // Glowing emerald-lime Mossberry orb + tiny leaf crown
                LineRenderer berry = CreateLine(go.transform, "MossBerryBody", 15, false);
                int segs = 13;
                berry.positionCount = segs;
                berry.startWidth = 0.14f;
                berry.endWidth = 0.14f;
                Color berryCol = new Color(0.60f, 0.95f, 0.34f, 0.98f);
                berry.startColor = berryCol;
                berry.endColor = berryCol;
                for (int i = 0; i < segs; i++)
                {
                    float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                    berry.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.13f, Mathf.Sin(a) * 0.14f, 0f));
                }

                LineRenderer leaf = CreateLine(go.transform, "MossBerryLeaf", 16, false);
                leaf.positionCount = 3;
                leaf.startWidth = 0.05f;
                leaf.endWidth = 0.05f;
                Color leafCol = new Color(0.24f, 0.62f, 0.22f, 0.98f);
                leaf.startColor = leafCol;
                leaf.endColor = leafCol;
                leaf.SetPosition(0, new Vector3(-0.13f, 0.16f, 0f));
                leaf.SetPosition(1, new Vector3(0f, 0.11f, 0f));
                leaf.SetPosition(2, new Vector3(0.13f, 0.17f, 0f));
            }
            else if (carryType == AknidCarryType.Flea)
            {
                // Cute wriggling ivory-gold captive Flea silhouette + two antennae
                LineRenderer body = CreateLine(go.transform, "FleaBody", 15, false);
                int segs = 13;
                body.positionCount = segs;
                body.startWidth = 0.14f;
                body.endWidth = 0.14f;
                Color cream = new Color(0.98f, 0.95f, 0.84f, 0.98f);
                body.startColor = cream;
                body.endColor = cream;
                for (int i = 0; i < segs; i++)
                {
                    float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                    body.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.15f, Mathf.Sin(a) * 0.12f, 0f));
                }

                LineRenderer ears = CreateLine(go.transform, "FleaAntennae", 16, false);
                ears.positionCount = 3;
                ears.startWidth = 0.045f;
                ears.endWidth = 0.045f;
                Color warm = new Color(0.95f, 0.78f, 0.42f, 0.98f);
                ears.startColor = warm;
                ears.endColor = warm;
                ears.SetPosition(0, new Vector3(-0.14f, 0.20f, 0f));
                ears.SetPosition(1, new Vector3(0f, 0.08f, 0f));
                ears.SetPosition(2, new Vector3(0.14f, 0.20f, 0f));
            }

            return go;
        }

        /// <summary>
        /// Draws crisp coral-amber spark rays radiating off surface normal when Driznit's BouncingProjectile rebounds.
        /// </summary>
        public static void SpawnBounceSparks(Vector3 position, Vector2 normal)
        {
            GameObject root = new GameObject("CodeVFX_BounceSparks");
            root.transform.position = position;
            int rays = 5;
            LineRenderer[] lines = new LineRenderer[rays];
            Vector2 n = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector2.up;
            float baseDeg = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg;
            Color sparkCol = new Color(1.0f, 0.78f, 0.44f, 0.98f);

            for (int i = 0; i < rays; i++)
            {
                LineRenderer lr = CreateLine(root.transform, "BounceSpark_" + i, 18, false);
                lr.positionCount = 2;
                float ang = (baseDeg + Random.Range(-65f, 65f)) * Mathf.Deg2Rad;
                float len = Random.Range(0.28f, 0.64f);
                lr.startWidth = 0.055f;
                lr.endWidth = 0.015f;
                Color c = (i % 2 == 0) ? Color.white : sparkCol;
                lr.startColor = c;
                lr.endColor = new Color(c.r, c.g, c.b, 0.15f);
                lr.SetPosition(0, new Vector3(Mathf.Cos(ang) * 0.05f, Mathf.Sin(ang) * 0.05f, 0f));
                lr.SetPosition(1, new Vector3(Mathf.Cos(ang) * len, Mathf.Sin(ang) * len, 0f));
                lines[i] = lr;
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(lines, sparkCol, 0.05f, 0.16f, (Vector3)(n * 0.9f), 1.32f);
        }

        /// <summary>
        /// Draws a thin, fair telegraph direction flash line during Conchfly's AimTelegraph (0.35s)
        /// so the player can read the locked diagonal dart path and dodge or jump cleanly.
        /// </summary>
        public static void SpawnConchflyAimLine(Vector3 origin, Vector2 lockedDirection, float length, float duration = 0.33f)
        {
            GameObject root = new GameObject("CodeVFX_ConchflyAimLine");
            root.transform.position = origin;
            Vector2 dir = lockedDirection.sqrMagnitude > 0.001f ? lockedDirection.normalized : Vector2.right;
            Color aimCol = new Color(1.0f, 0.62f, 0.46f, 0.88f);

            // Thin directional laser telegraph line
            LineRenderer beam = CreateLine(root.transform, "AimBeam", 16, false);
            beam.positionCount = 2;
            beam.startWidth = 0.06f;
            beam.endWidth = 0.018f;
            beam.startColor = Color.white;
            beam.endColor = new Color(aimCol.r, aimCol.g, aimCol.b, 0.18f);
            beam.SetPosition(0, (Vector3)(dir * 0.35f));
            beam.SetPosition(1, (Vector3)(dir * Mathf.Max(2.5f, length)));

            // Crisp star-glint at launch nose
            LineRenderer glint = CreateLine(root.transform, "AimGlint", 18, false);
            glint.positionCount = 3;
            glint.startWidth = 0.05f;
            glint.endWidth = 0.05f;
            glint.startColor = Color.white;
            glint.endColor = aimCol;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            glint.SetPosition(0, (Vector3)(dir * 0.42f - perp * 0.34f));
            glint.SetPosition(1, (Vector3)(dir * 0.65f));
            glint.SetPosition(2, (Vector3)(dir * 0.42f + perp * 0.34f));

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { beam, glint }, aimCol, 0.055f, duration, Vector3.zero, 1.04f);
        }

        /// <summary>
        /// Spawns downward-falling grit & dust streaks during Pharlid's 0.18s overhead DropWarning.
        /// </summary>
        public static void SpawnCeilingDropDust(Vector3 ceilingPos, float width = 0.9f)
        {
            GameObject root = new GameObject("CodeVFX_CeilingDropDust");
            CoralShardBurstRunner runner = root.AddComponent<CoralShardBurstRunner>();
            Color[] dustPalette = new Color[]
            {
                new Color(0.92f, 0.86f, 0.76f, 0.88f),
                new Color(0.80f, 0.72f, 0.62f, 0.82f),
                new Color(0.98f, 0.95f, 0.88f, 0.85f)
            };
            runner.InitializeShards(
                ceilingPos + new Vector3(0f, -0.22f, 0f),
                6,
                0.8f,
                2.2f,
                -1.8f,
                14.0f,
                0.36f,
                false,
                dustPalette,
                0.08f,
                0.15f);
        }

        /// <summary>
        /// Spawns Coral Furm's signature crunchy coral shard burst (red-orange & chalky reef-white chunks under gravity)
        /// plus a crisp radial crack ring on hit or death — 100% code-drawn, zero image files.
        /// </summary>
        public static void SpawnCoralShardBurst(Vector3 center, int shardCount, bool isDeath)
        {
            GameObject shardsRoot = new GameObject("CodeVFX_CoralShards");
            CoralShardBurstRunner runner = shardsRoot.AddComponent<CoralShardBurstRunner>();
            Color[] coralPalette = new Color[]
            {
                new Color(0.96f, 0.38f, 0.28f, 1f), // vibrant coral red
                new Color(0.98f, 0.56f, 0.30f, 1f), // reef orange
                new Color(0.98f, 0.94f, 0.88f, 1f), // chalky reef white
                new Color(0.86f, 0.28f, 0.26f, 1f)  // deep crimson shell
            };
            runner.InitializeShards(
                center,
                Mathf.Max(4, shardCount),
                isDeath ? 4.2f : 3.0f,
                isDeath ? 9.4f : 6.8f,
                isDeath ? 2.6f : 1.8f,
                20.5f,
                isDeath ? 0.72f : 0.48f,
                true,
                coralPalette,
                isDeath ? 0.11f : 0.08f,
                isDeath ? 0.20f : 0.15f);

            // Crisp crunchy impact ring
            GameObject ringRoot = new GameObject("CodeVFX_CoralCrunchRing");
            ringRoot.transform.position = center;
            LineRenderer ring = CreateLine(ringRoot.transform, "CrunchRing", 17, false);
            int segs = 13;
            ring.positionCount = segs;
            ring.startWidth = isDeath ? 0.09f : 0.065f;
            ring.endWidth = isDeath ? 0.09f : 0.065f;
            Color ringCol = new Color(1.0f, 0.64f, 0.42f, 0.95f);
            ring.startColor = ringCol;
            ring.endColor = ringCol;
            float r = isDeath ? 0.46f : 0.30f;
            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                float jag = (i % 2 == 0) ? 1.12f : 0.88f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * r * jag, Mathf.Sin(a) * r * jag, 0f));
            }
            CodeVfxFadeRunner fade = ringRoot.AddComponent<CodeVfxFadeRunner>();
            fade.Initialize(new LineRenderer[] { ring }, ringCol, ring.startWidth, isDeath ? 0.24f : 0.16f, Vector3.zero, isDeath ? 1.85f : 1.48f);
        }

        /// <summary>
        /// Draws intense luminous ice-gold impact sparks where Servitor Boran's 2-damage laser strikes terrain or player.
        /// </summary>
        public static void SpawnLaserImpactSparks(Vector3 position, Vector2 normal)
        {
            GameObject root = new GameObject("CodeVFX_LaserImpact");
            root.transform.position = position;
            int rays = 6;
            LineRenderer[] lines = new LineRenderer[rays + 1];
            Vector2 n = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector2.up;
            float baseDeg = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg;
            Color beamTint = new Color(0.82f, 0.96f, 1.0f, 0.98f);

            for (int i = 0; i < rays; i++)
            {
                LineRenderer lr = CreateLine(root.transform, "LaserSpark_" + i, 19, false);
                lr.positionCount = 2;
                float ang = (baseDeg + Random.Range(-72f, 72f)) * Mathf.Deg2Rad;
                float len = Random.Range(0.36f, 0.88f);
                lr.startWidth = 0.065f;
                lr.endWidth = 0.015f;
                Color c = (i % 2 == 0) ? Color.white : beamTint;
                lr.startColor = c;
                lr.endColor = new Color(c.r, c.g, c.b, 0.14f);
                lr.SetPosition(0, new Vector3(Mathf.Cos(ang) * 0.06f, Mathf.Sin(ang) * 0.06f, 0f));
                lr.SetPosition(1, new Vector3(Mathf.Cos(ang) * len, Mathf.Sin(ang) * len, 0f));
                lines[i] = lr;
            }

            LineRenderer flare = CreateLine(root.transform, "LaserFlareRing", 18, false);
            int segs = 13;
            flare.positionCount = segs;
            flare.startWidth = 0.075f;
            flare.endWidth = 0.075f;
            flare.startColor = Color.white;
            flare.endColor = beamTint;
            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                flare.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.28f, Mathf.Sin(a) * 0.28f, 0f));
            }
            lines[rays] = flare;

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(lines, beamTint, 0.065f, 0.18f, (Vector3)(n * 0.85f), 1.42f);
        }

        /// <summary>
        /// Builds a 360-degree code-drawn retaliation crystal-metallic spike ring around Mnemonid / Mnemonord
        /// during SpikeActive (zero external image files required!).
        /// </summary>
        public static GameObject CreateRetaliationSpikeRing(
            Transform parent,
            int spikeCount,
            float innerRadius,
            float outerRadius,
            Color baseColor,
            Color tipColor)
        {
            GameObject go = new GameObject("CodeVFX_RetaliationSpikes");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            int count = Mathf.Clamp(spikeCount, 6, 20);
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                float perpX = -sin;
                float perpY = cos;
                float halfBase = (outerRadius - innerRadius) * 0.26f;

                LineRenderer spike = CreateLine(go.transform, "Spike_" + i, 17, false);
                spike.positionCount = 3;
                spike.startWidth = 0.055f;
                spike.endWidth = 0.055f;
                spike.startColor = baseColor;
                spike.endColor = tipColor;
                spike.SetPosition(0, new Vector3(cos * innerRadius - perpX * halfBase, sin * innerRadius - perpY * halfBase, 0f));
                spike.SetPosition(1, new Vector3(cos * outerRadius, sin * outerRadius, 0f));
                spike.SetPosition(2, new Vector3(cos * innerRadius + perpX * halfBase, sin * innerRadius + perpY * halfBase, 0f));
            }

            // Inner glowing defensive crystal ring
            LineRenderer halo = CreateLine(go.transform, "SpikeHalo", 16, false);
            int segs = 21;
            halo.positionCount = segs;
            halo.startWidth = 0.065f;
            halo.endWidth = 0.065f;
            Color ringCol = new Color(baseColor.r, baseColor.g, baseColor.b, 0.85f);
            halo.startColor = ringCol;
            halo.endColor = ringCol;
            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                halo.SetPosition(i, new Vector3(Mathf.Cos(a) * innerRadius, Mathf.Sin(a) * innerRadius, 0f));
            }

            go.SetActive(false);
            return go;
        }

        /// <summary>
        /// Draws a crisp directional spear thrust streak at any angle (horizontal OR diagonal downward)
        /// for Pondcatcher's HuntIdle pond-poke and SpearThrust attacks.
        /// </summary>
        public static void SpawnSpearThrustLine(Vector3 origin, Vector2 direction, float length, Color color, float duration = 0.16f)
        {
            GameObject root = new GameObject("CodeVFX_SpearThrust");
            root.transform.position = origin;
            Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;

            LineRenderer core = CreateLine(root.transform, "SpearCore", 18, false);
            core.positionCount = 2;
            core.startWidth = 0.03f;
            core.endWidth = 0.12f;
            core.startColor = new Color(color.r, color.g, color.b, 0.25f);
            core.endColor = Color.white;
            core.SetPosition(0, Vector3.zero);
            core.SetPosition(1, (Vector3)(dir * length));

            LineRenderer edge = CreateLine(root.transform, "SpearEdge", 17, false);
            edge.positionCount = 2;
            edge.startWidth = 0.06f;
            edge.endWidth = 0.22f;
            edge.startColor = new Color(color.r, color.g, color.b, 0.12f);
            edge.endColor = new Color(color.r, color.g, color.b, 0.82f);
            edge.SetPosition(0, (Vector3)(-dir * 0.22f));
            edge.SetPosition(1, (Vector3)(dir * (length * 1.06f)));

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { core, edge }, color, 0.12f, duration, (Vector3)(dir * 2.2f), 1.05f);
        }

        /// <summary>
        /// Builds a subtle code-drawn quivering soil mound & crack lines where Furm hides underground.
        /// </summary>
        public static GameObject CreateBurrowMoundVisual(Transform parent)
        {
            GameObject go = new GameObject("CodeVFX_BurrowMound");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            LineRenderer mound = CreateLine(go.transform, "SoilArc", 14, false);
            int segs = 11;
            mound.positionCount = segs;
            mound.startWidth = 0.10f;
            mound.endWidth = 0.10f;
            Color earthCol = new Color(0.56f, 0.48f, 0.40f, 0.90f);
            mound.startColor = earthCol;
            mound.endColor = earthCol;
            for (int i = 0; i < segs; i++)
            {
                float u = (float)i / (segs - 1);
                float a = Mathf.Lerp(Mathf.PI, 0f, u);
                mound.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.62f, Mathf.Sin(a) * 0.24f, 0f));
            }

            // Two crisp soil pebble lines
            LineRenderer crack = CreateLine(go.transform, "SoilRim", 15, false);
            crack.positionCount = 4;
            crack.startWidth = 0.05f;
            crack.endWidth = 0.05f;
            Color highlight = new Color(0.78f, 0.72f, 0.62f, 0.85f);
            crack.startColor = highlight;
            crack.endColor = highlight;
            crack.SetPosition(0, new Vector3(-0.46f, 0.06f, 0f));
            crack.SetPosition(1, new Vector3(-0.14f, 0.22f, 0f));
            crack.SetPosition(2, new Vector3(0.18f, 0.19f, 0f));
            crack.SetPosition(3, new Vector3(0.48f, 0.05f, 0f));

            return go;
        }

        /// <summary>
        /// Spawns gravity-driven dirt clods & dust bursting from the floor for Furm EruptWarning / FastPopJump / Burrow.
        /// </summary>
        public static void SpawnBurrowDirtBurst(Vector3 feetPos, bool isErupt)
        {
            GameObject root = new GameObject("CodeVFX_BurrowDirt");
            CoralShardBurstRunner runner = root.AddComponent<CoralShardBurstRunner>();
            Color[] dirtPalette = new Color[]
            {
                new Color(0.58f, 0.48f, 0.38f, 0.96f),
                new Color(0.76f, 0.68f, 0.56f, 0.92f),
                new Color(0.42f, 0.36f, 0.32f, 0.96f)
            };
            runner.InitializeShards(
                feetPos + new Vector3(0f, 0.14f, 0f),
                isErupt ? 10 : 5,
                isErupt ? 3.2f : 1.6f,
                isErupt ? 7.6f : 3.8f,
                isErupt ? 3.4f : 1.4f,
                19.5f,
                isErupt ? 0.55f : 0.32f,
                true,
                dirtPalette,
                0.07f,
                isErupt ? 0.15f : 0.11f);
        }

        /// <summary>
        /// Builds code-drawn orbiting golden dizzy stars + faint confusion ring hovering above Winged Furm's helmet.
        /// </summary>
        public static GameObject CreateDizzyStarsVisual(Transform parent, Vector3 localOffset)
        {
            GameObject go = new GameObject("CodeVFX_DizzyStars");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localOffset;

            // Elliptical dizzy orbit ring
            LineRenderer orbit = CreateLine(go.transform, "DizzyOrbit", 17, false);
            int segs = 19;
            orbit.positionCount = segs;
            orbit.startWidth = 0.032f;
            orbit.endWidth = 0.032f;
            Color haloCol = new Color(1.0f, 0.94f, 0.56f, 0.65f);
            orbit.startColor = haloCol;
            orbit.endColor = haloCol;
            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                orbit.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.58f, Mathf.Sin(a) * 0.20f, 0f));
            }

            // 3 orbiting 4-ray stars around the ellipse
            for (int s = 0; s < 3; s++)
            {
                float a = (s / 3f) * Mathf.PI * 2f;
                Vector3 center = new Vector3(Mathf.Cos(a) * 0.58f, Mathf.Sin(a) * 0.20f, 0f);
                GameObject starGo = new GameObject("DizzyStar_" + s);
                starGo.transform.SetParent(go.transform, false);
                starGo.transform.localPosition = center;

                LineRenderer h = CreateLine(starGo.transform, "H", 18, false);
                h.positionCount = 2;
                h.startWidth = 0.045f;
                h.endWidth = 0.045f;
                h.startColor = Color.white;
                h.endColor = new Color(1.0f, 0.88f, 0.36f, 1f);
                h.SetPosition(0, new Vector3(-0.14f, 0f, 0f));
                h.SetPosition(1, new Vector3(0.14f, 0f, 0f));

                LineRenderer v = CreateLine(starGo.transform, "V", 18, false);
                v.positionCount = 2;
                v.startWidth = 0.045f;
                v.endWidth = 0.045f;
                v.startColor = Color.white;
                v.endColor = new Color(1.0f, 0.88f, 0.36f, 1f);
                v.SetPosition(0, new Vector3(0f, -0.14f, 0f));
                v.SetPosition(1, new Vector3(0f, 0.14f, 0f));
            }

            go.SetActive(false);
            return go;
        }

        /// <summary>
        /// Draws a gleaming golden rosary chain sweep arc with luminous bead flashes for Covetous Pilgrim's ChainSwing.
        /// </summary>
        public static void SpawnGoldenChainArc(Vector3 center, int facingDir, float radiusX, float radiusY, float duration = 0.18f)
        {
            GameObject root = new GameObject("CodeVFX_GoldenChainArc");
            root.transform.position = center;
            float dir = facingDir >= 0 ? 1f : -1f;
            Color gold = new Color(1.0f, 0.86f, 0.34f, 0.98f);
            Color crimson = new Color(0.98f, 0.36f, 0.34f, 0.92f);

            LineRenderer mainArc = CreateLine(root.transform, "ChainRibbon", 17, false);
            int pts = 16;
            mainArc.positionCount = pts;
            mainArc.startWidth = 0.05f;
            mainArc.endWidth = 0.15f;
            mainArc.startColor = new Color(gold.r, gold.g, gold.b, 0.30f);
            mainArc.endColor = gold;

            LineRenderer innerArc = CreateLine(root.transform, "BeadGlints", 18, false);
            innerArc.positionCount = pts;
            innerArc.startWidth = 0.03f;
            innerArc.endWidth = 0.09f;
            innerArc.startColor = crimson;
            innerArc.endColor = Color.white;

            float halfRad = 72f * Mathf.Deg2Rad;
            for (int i = 0; i < pts; i++)
            {
                float u = (float)i / (pts - 1);
                float a = Mathf.Lerp(halfRad, -halfRad, u);
                float jitter = (i % 2 == 0) ? 1.04f : 0.96f;
                Vector3 p = new Vector3(Mathf.Cos(a) * radiusX * jitter * dir, Mathf.Sin(a) * radiusY * jitter, 0f);
                mainArc.SetPosition(i, p);
                innerArc.SetPosition(i, p * 0.94f);
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { mainArc, innerArc }, gold, 0.14f, duration, new Vector3(dir * 1.6f, 0f, 0f), 1.12f);
        }

        /// <summary>
        /// Spawns gleaming gravity-driven golden-crimson rosary beads / coins bouncing on the ground
        /// for Covetous Pilgrim HeavyStepSlam, hit-drop feedback, and defeat jackpot.
        /// </summary>
        public static void SpawnRosaryCoinShower(Vector3 origin, int count, bool isHeavy)
        {
            GameObject root = new GameObject("CodeVFX_RosaryCoins");
            CoralShardBurstRunner runner = root.AddComponent<CoralShardBurstRunner>();
            Color[] rosaryPalette = new Color[]
            {
                new Color(1.0f, 0.88f, 0.36f, 1f), // bright gold coin
                new Color(0.98f, 0.32f, 0.36f, 1f), // crimson rosary bead
                new Color(1.0f, 0.98f, 0.82f, 1f)   // gleaming specular glint
            };
            runner.InitializeShards(
                origin,
                Mathf.Clamp(count, 3, 20),
                isHeavy ? 3.4f : 2.2f,
                isHeavy ? 8.2f : 5.4f,
                isHeavy ? 3.2f : 2.2f,
                19.0f,
                isHeavy ? 0.68f : 0.46f,
                true,
                rosaryPalette,
                0.065f,
                isHeavy ? 0.13f : 0.10f);
        }

        /// <summary>
        /// Draws swirling floor dust kicked backward during Pilgrim Hulk's RollingCharge & WallBonk.
        /// </summary>
        public static void SpawnRollingDustTrail(Vector3 feetPos, int rollDir)
        {
            SpawnChargeDust(feetPos, rollDir, false);
        }

        /// <summary>
        /// Builds a reusable pulsing golden-white silk needle charge aura attached to the player during Charge Attack buildup.
        /// </summary>
        public static GameObject CreateNeedleChargeAura(Transform parent)
        {
            GameObject go = new GameObject("CodeVFX_NeedleChargeAura");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.96f, 0f);

            LineRenderer ring = CreateLine(go.transform, "ChargeRing", 17, false);
            int segs = 19;
            ring.positionCount = segs;
            ring.startWidth = 0.048f;
            ring.endWidth = 0.048f;
            Color goldWhite = new Color(1.0f, 0.97f, 0.78f, 0.92f);
            ring.startColor = goldWhite;
            ring.endColor = goldWhite;
            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.62f, Mathf.Sin(a) * 0.72f, 0f));
            }

            LineRenderer rayH = CreateLine(go.transform, "GlintH", 18, false);
            rayH.positionCount = 2;
            rayH.startWidth = 0.055f;
            rayH.endWidth = 0.055f;
            rayH.startColor = Color.white;
            rayH.endColor = goldWhite;
            rayH.SetPosition(0, new Vector3(-0.78f, 0f, 0f));
            rayH.SetPosition(1, new Vector3(0.78f, 0f, 0f));

            LineRenderer rayV = CreateLine(go.transform, "GlintV", 18, false);
            rayV.positionCount = 2;
            rayV.startWidth = 0.055f;
            rayV.endWidth = 0.055f;
            rayV.startColor = Color.white;
            rayV.endColor = goldWhite;
            rayV.SetPosition(0, new Vector3(0f, -0.58f, 0f));
            rayV.SetPosition(1, new Vector3(0f, 0.58f, 0f));

            go.SetActive(false);
            return go;
        }

        /// <summary>
        /// Draws a glowing X-shaped Cross Stitch silk parry weave right in front of the player (00:28:46).
        /// </summary>
        public static void SpawnCrossStitchStance(Transform parent, int facingDir, float duration = 0.52f)
        {
            GameObject root = new GameObject("CodeVFX_CrossStitchStance");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
                root.transform.localPosition = new Vector3(facingDir * 0.32f, 0.98f, 0f);
            }
            Color silkWhite = new Color(0.95f, 0.99f, 1.0f, 0.96f);
            Color paleCyan = new Color(0.72f, 0.96f, 1.0f, 0.88f);

            LineRenderer slashA = CreateLine(root.transform, "StitchA", 19, false);
            slashA.positionCount = 2;
            slashA.startWidth = 0.085f;
            slashA.endWidth = 0.035f;
            slashA.startColor = Color.white;
            slashA.endColor = paleCyan;
            slashA.SetPosition(0, new Vector3(-0.72f, -0.78f, 0f));
            slashA.SetPosition(1, new Vector3(0.78f, 0.82f, 0f));

            LineRenderer slashB = CreateLine(root.transform, "StitchB", 19, false);
            slashB.positionCount = 2;
            slashB.startWidth = 0.085f;
            slashB.endWidth = 0.035f;
            slashB.startColor = Color.white;
            slashB.endColor = paleCyan;
            slashB.SetPosition(0, new Vector3(-0.72f, 0.82f, 0f));
            slashB.SetPosition(1, new Vector3(0.78f, -0.78f, 0f));

            LineRenderer ring = CreateLine(root.transform, "StitchRing", 18, false);
            int segs = 17;
            ring.positionCount = segs;
            ring.startWidth = 0.042f;
            ring.endWidth = 0.042f;
            ring.startColor = silkWhite;
            ring.endColor = paleCyan;
            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.62f, Mathf.Sin(a) * 0.68f, 0f));
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { slashA, slashB, ring }, silkWhite, 0.08f, duration, Vector3.zero, 1.18f);
        }

        /// <summary>
        /// Explosive crossed pure-white counter-slash burst triggered when Cross Stitch successfully parries a hit!
        /// </summary>
        public static void SpawnCrossStitchCounterBurst(Vector3 center, int facingDir)
        {
            GameObject root = new GameObject("CodeVFX_CrossStitchCounter");
            root.transform.position = center;
            Color cyan = new Color(0.78f, 0.98f, 1.0f, 0.98f);
            float dir = facingDir >= 0 ? 1f : -1f;

            LineRenderer[] lines = new LineRenderer[4];
            float[] angles = new float[] { 34f, -34f, 8f, -14f };
            for (int i = 0; i < 4; i++)
            {
                LineRenderer lr = CreateLine(root.transform, "CounterBlade_" + i, 20, false);
                lr.positionCount = 2;
                lr.startWidth = i < 2 ? 0.14f : 0.09f;
                lr.endWidth = 0.025f;
                lr.startColor = Color.white;
                lr.endColor = cyan;
                float rad = angles[i] * Mathf.Deg2Rad;
                float len = i < 2 ? 1.85f : 2.25f;
                Vector3 v = new Vector3(Mathf.Cos(rad) * len * dir, Mathf.Sin(rad) * len, 0f);
                lr.SetPosition(0, v * -0.45f);
                lr.SetPosition(1, v * 0.95f);
                lines[i] = lr;
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(lines, Color.white, 0.13f, 0.22f, new Vector3(dir * 2.4f, 0f, 0f), 1.32f);
        }

        /// <summary>
        /// Swirling 360-degree pure-white & pale-cyan Thread Storm silk cyclone (00:28:29) attached around the player!
        /// </summary>
        public static void SpawnThreadStormVortex(Transform parent, float radius, float duration)
        {
            GameObject root = new GameObject("CodeVFX_ThreadStormVortex");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
                root.transform.localPosition = new Vector3(0f, 0.98f, 0f);
            }
            Color silkWhite = new Color(0.98f, 0.99f, 1.0f, 0.96f);
            Color stormCyan = new Color(0.76f, 0.96f, 1.0f, 0.88f);

            int ringCount = 5;
            int slashCount = 6;
            LineRenderer[] allLines = new LineRenderer[ringCount + slashCount];

            // 5 tilted elliptical swirling silk thread rings
            for (int r = 0; r < ringCount; r++)
            {
                LineRenderer lr = CreateLine(root.transform, "StormRing_" + r, 17 + (r % 3), false);
                int segs = 21;
                lr.positionCount = segs;
                lr.startWidth = 0.085f - r * 0.01f;
                lr.endWidth = 0.035f;
                lr.startColor = (r % 2 == 0) ? Color.white : stormCyan;
                lr.endColor = new Color(stormCyan.r, stormCyan.g, stormCyan.b, 0.22f);

                float radX = radius * (0.58f + r * 0.11f);
                float radY = radius * (0.44f + (r % 3) * 0.18f);
                float tilt = (r * 36f - 72f) * Mathf.Deg2Rad;
                float ct = Mathf.Cos(tilt);
                float st = Mathf.Sin(tilt);
                float startA = r * 1.15f;
                for (int i = 0; i < segs; i++)
                {
                    float u = (float)i / (segs - 1);
                    float a = startA + u * Mathf.PI * 1.85f;
                    float lx = Mathf.Cos(a) * radX;
                    float ly = Mathf.Sin(a) * radY;
                    lr.SetPosition(i, new Vector3(lx * ct - ly * st, lx * st + ly * ct, 0f));
                }
                allLines[r] = lr;
            }

            // 6 whipping radial needle-thread slashes cutting outward
            for (int s = 0; s < slashCount; s++)
            {
                LineRenderer lr = CreateLine(root.transform, "StormWhip_" + s, 19, false);
                lr.positionCount = 2;
                lr.startWidth = 0.03f;
                lr.endWidth = 0.095f;
                lr.startColor = stormCyan;
                lr.endColor = Color.white;
                float a = (s / (float)slashCount) * Mathf.PI * 2f;
                lr.SetPosition(0, new Vector3(Mathf.Cos(a) * radius * 0.22f, Mathf.Sin(a) * radius * 0.22f, 0f));
                lr.SetPosition(1, new Vector3(Mathf.Cos(a) * radius * 1.04f, Mathf.Sin(a) * radius * 1.04f, 0f));
                allLines[ringCount + s] = lr;
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(allLines, silkWhite, 0.085f, duration, Vector3.zero, 1.16f);
        }

        /// <summary>
        /// Heavy golden-white double crescent + piercing needle burst for fully charged Needle Charge Attack (00:08:52).
        /// </summary>
        public static void SpawnChargeSlashBurst(Vector3 center, int facingDir)
        {
            Color goldWhite = new Color(1.0f, 0.96f, 0.78f, 0.98f);
            SpawnSlashArc(center, facingDir, 1.55f, 1.05f, 132f, 0.22f, Color.white, 0.18f);
            SpawnSlashArc(center + new Vector3(facingDir * 0.22f, 0f, 0f), facingDir, 1.80f, 1.15f, 118f, 0.16f, goldWhite, 0.20f);
            SpawnThrustStreak(center - new Vector3(facingDir * 0.45f, 0f, 0f), facingDir, 2.65f, 0.18f, goldWhite, 0.17f);
        }

        /// <summary>
        /// Blazing horizontal cyan-white silk lance trail left behind by Sharpdart dash-through (00:29:03).
        /// </summary>
        public static void SpawnSharpdartTrail(Vector3 startPos, Vector3 endPos, int facingDir)
        {
            GameObject root = new GameObject("CodeVFX_SharpdartTrail");
            root.transform.position = startPos;
            Vector3 delta = endPos - startPos;
            Color cyan = new Color(0.68f, 0.95f, 1.0f, 0.95f);

            LineRenderer core = CreateLine(root.transform, "DartCore", 21, false);
            core.positionCount = 2;
            core.startWidth = 0.04f;
            core.endWidth = 0.22f;
            core.startColor = cyan;
            core.endColor = Color.white;
            core.SetPosition(0, Vector3.zero);
            core.SetPosition(1, delta);

            LineRenderer upper = CreateLine(root.transform, "DartThreadTop", 20, false);
            upper.positionCount = 2;
            upper.startWidth = 0.03f;
            upper.endWidth = 0.08f;
            upper.startColor = new Color(cyan.r, cyan.g, cyan.b, 0.72f);
            upper.endColor = Color.white;
            upper.SetPosition(0, new Vector3(0f, 0.28f, 0f));
            upper.SetPosition(1, delta + new Vector3(0f, 0.08f, 0f));

            LineRenderer lower = CreateLine(root.transform, "DartThreadBot", 20, false);
            lower.positionCount = 2;
            lower.startWidth = 0.03f;
            lower.endWidth = 0.08f;
            lower.startColor = new Color(cyan.r, cyan.g, cyan.b, 0.72f);
            lower.endColor = Color.white;
            lower.SetPosition(0, new Vector3(0f, -0.28f, 0f));
            lower.SetPosition(1, delta + new Vector3(0f, -0.08f, 0f));

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { core, upper, lower }, Color.white, 0.15f, 0.30f, new Vector3(facingDir * 0.4f, 0f, 0f), 1.04f);
        }

        /// <summary>
        /// Delayed silk-rend X-burst detonating on enemies pierced by Sharpdart (00:29:03).
        /// </summary>
        public static void SpawnSharpdartAfterSlash(Vector3 center)
        {
            GameObject root = new GameObject("CodeVFX_SharpdartAfterSlash");
            root.transform.position = center;
            Color cyan = new Color(0.72f, 0.96f, 1.0f, 0.98f);

            LineRenderer a = CreateLine(root.transform, "AfterA", 22, false);
            a.positionCount = 2;
            a.startWidth = 0.13f;
            a.endWidth = 0.025f;
            a.startColor = Color.white;
            a.endColor = cyan;
            a.SetPosition(0, new Vector3(-0.95f, -0.78f, 0f));
            a.SetPosition(1, new Vector3(0.95f, 0.78f, 0f));

            LineRenderer b = CreateLine(root.transform, "AfterB", 22, false);
            b.positionCount = 2;
            b.startWidth = 0.13f;
            b.endWidth = 0.025f;
            b.startColor = Color.white;
            b.endColor = cyan;
            b.SetPosition(0, new Vector3(-0.95f, 0.78f, 0f));
            b.SetPosition(1, new Vector3(0.95f, -0.78f, 0f));

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { a, b }, Color.white, 0.12f, 0.22f, Vector3.zero, 1.30f);
        }

        /// <summary>
        /// Glowing arcane silk hexagram circle telegraphing a Rune Rage detonation zone (00:29:28).
        /// </summary>
        public static void SpawnRuneRageSeal(Vector3 center, float radius, float telegraphDur)
        {
            GameObject root = new GameObject("CodeVFX_RuneRageSeal");
            root.transform.position = center;
            Color runeGold = new Color(1.0f, 0.92f, 0.62f, 0.92f);
            Color runeCyan = new Color(0.74f, 0.96f, 1.0f, 0.90f);

            LineRenderer outer = CreateLine(root.transform, "RuneCircle", 18, false);
            int segs = 25;
            outer.positionCount = segs;
            outer.startWidth = 0.065f;
            outer.endWidth = 0.065f;
            outer.startColor = runeGold;
            outer.endColor = runeCyan;
            for (int i = 0; i < segs; i++)
            {
                float ang = (float)i / (segs - 1) * Mathf.PI * 2f;
                outer.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, 0f));
            }

            LineRenderer triA = CreateLine(root.transform, "RuneTriA", 19, false);
            triA.positionCount = 4;
            triA.startWidth = 0.045f;
            triA.endWidth = 0.045f;
            triA.startColor = Color.white;
            triA.endColor = runeGold;
            for (int i = 0; i < 4; i++)
            {
                float ang = (i * 120f + 30f) * Mathf.Deg2Rad;
                triA.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius * 0.90f, Mathf.Sin(ang) * radius * 0.90f, 0f));
            }

            LineRenderer triB = CreateLine(root.transform, "RuneTriB", 19, false);
            triB.positionCount = 4;
            triB.startWidth = 0.045f;
            triB.endWidth = 0.045f;
            triB.startColor = runeCyan;
            triB.endColor = Color.white;
            for (int i = 0; i < 4; i++)
            {
                float ang = (i * 120f + 90f) * Mathf.Deg2Rad;
                triB.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius * 0.90f, Mathf.Sin(ang) * radius * 0.90f, 0f));
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { outer, triA, triB }, runeGold, 0.06f, telegraphDur, Vector3.zero, 1.08f);
        }

        /// <summary>
        /// Towering arcane silk explosion pillar & radial starburst for Rune Rage blast (00:29:28).
        /// </summary>
        public static void SpawnRuneRageDetonation(Vector3 center, float radius)
        {
            GameObject root = new GameObject("CodeVFX_RuneRageBlast");
            root.transform.position = center;
            Color runeGold = new Color(1.0f, 0.94f, 0.68f, 0.98f);
            Color runeCyan = new Color(0.76f, 0.97f, 1.0f, 0.96f);

            LineRenderer[] lines = new LineRenderer[7];
            // Central towering vertical silk pillar
            LineRenderer pillar = CreateLine(root.transform, "RunePillar", 22, false);
            pillar.positionCount = 2;
            pillar.startWidth = radius * 0.52f;
            pillar.endWidth = 0.08f;
            pillar.startColor = Color.white;
            pillar.endColor = runeGold;
            pillar.SetPosition(0, new Vector3(0f, -radius * 0.55f, 0f));
            pillar.SetPosition(1, new Vector3(0f, radius * 1.95f, 0f));
            lines[0] = pillar;

            // 6 sharp radial rune-burst spears
            for (int i = 0; i < 6; i++)
            {
                LineRenderer lr = CreateLine(root.transform, "RuneRay_" + i, 21, false);
                lr.positionCount = 2;
                lr.startWidth = 0.14f;
                lr.endWidth = 0.02f;
                lr.startColor = Color.white;
                lr.endColor = (i % 2 == 0) ? runeGold : runeCyan;
                float ang = (i * 60f + 15f) * Mathf.Deg2Rad;
                lr.SetPosition(0, Vector3.zero);
                lr.SetPosition(1, new Vector3(Mathf.Cos(ang) * radius * 1.25f, Mathf.Sin(ang) * radius * 1.25f, 0f));
                lines[1 + i] = lr;
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(lines, Color.white, 0.18f, 0.28f, new Vector3(0f, 0.5f, 0f), 1.24f);
        }

        /// <summary>
        /// Expanding concentric warm-gold sonic voice-ring emitted during Taunt / Garama! (00:07:46).
        /// </summary>
        public static void SpawnTauntVoiceRing(Vector3 origin, int facingDir)
        {
            GameObject root = new GameObject("CodeVFX_TauntVoiceRing");
            root.transform.position = origin;
            Color warmGold = new Color(1.0f, 0.92f, 0.62f, 0.94f);

            LineRenderer[] rings = new LineRenderer[2];
            for (int r = 0; r < 2; r++)
            {
                LineRenderer lr = CreateLine(root.transform, "VoiceRing_" + r, 20, false);
                int segs = 25;
                lr.positionCount = segs;
                lr.startWidth = r == 0 ? 0.085f : 0.048f;
                lr.endWidth = lr.startWidth;
                lr.startColor = r == 0 ? Color.white : warmGold;
                lr.endColor = warmGold;
                float rx = 0.72f + r * 0.42f;
                float ry = 0.52f + r * 0.30f;
                for (int i = 0; i < segs; i++)
                {
                    float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                    lr.SetPosition(i, new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0f));
                }
                rings[r] = lr;
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(rings, warmGold, 0.08f, 0.36f, new Vector3(facingDir * 0.45f, 0.25f, 0f), 2.45f);
        }

        /// <summary>
        /// Creates a live world-space taut glowing silk cord LineRenderer driven by Harpoon / Clawline (00:06:08).
        /// Caller updates positions each frame and destroys the GameObject when the grapple finishes.
        /// </summary>
        public static LineRenderer CreateLiveHarpoonCord(Vector3 startPos)
        {
            GameObject root = new GameObject("CodeVFX_HarpoonCord");
            root.transform.position = startPos;
            LineRenderer lr = CreateLine(root.transform, "SilkCord", 22, true);
            lr.positionCount = 2;
            lr.startWidth = 0.075f;
            lr.endWidth = 0.11f;
            lr.startColor = new Color(0.76f, 0.96f, 1.0f, 0.95f);
            lr.endColor = Color.white;
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, startPos);
            return lr;
        }

        /// <summary>
        /// Coiled upward-spiralling silk filaments at the player's feet before Silksoar launch (00:07:24).
        /// </summary>
        public static void SpawnSilksoarCharge(Vector3 basePos)
        {
            GameObject root = new GameObject("CodeVFX_SilksoarCharge");
            root.transform.position = basePos;
            Color cyan = new Color(0.72f, 0.96f, 1.0f, 0.94f);

            LineRenderer[] lines = new LineRenderer[5];
            LineRenderer ring = CreateLine(root.transform, "ChargeRing", 19, false);
            int segs = 21;
            ring.positionCount = segs;
            ring.startWidth = 0.065f;
            ring.endWidth = 0.065f;
            ring.startColor = Color.white;
            ring.endColor = cyan;
            for (int i = 0; i < segs; i++)
            {
                float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 1.15f, Mathf.Sin(a) * 0.28f + 0.14f, 0f));
            }
            lines[0] = ring;

            for (int k = 0; k < 4; k++)
            {
                LineRenderer streak = CreateLine(root.transform, "RiseStrand_" + k, 20, false);
                streak.positionCount = 2;
                streak.startWidth = 0.07f;
                streak.endWidth = 0.015f;
                streak.startColor = cyan;
                streak.endColor = Color.white;
                float ox = (k - 1.5f) * 0.56f;
                streak.SetPosition(0, new Vector3(ox, 0.08f, 0f));
                streak.SetPosition(1, new Vector3(ox * 0.45f, 1.95f, 0f));
                lines[1 + k] = streak;
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(lines, Color.white, 0.065f, 0.22f, new Vector3(0f, 1.8f, 0f), 1.15f);
        }

        /// <summary>
        /// Towering double-helix silk rocket wake attached to the soaring player during Silksoar (00:07:24).
        /// </summary>
        public static void SpawnSilksoarWake(Transform parent, float duration)
        {
            GameObject root = new GameObject("CodeVFX_SilksoarWake");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
                root.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            }
            Color cyan = new Color(0.74f, 0.97f, 1.0f, 0.94f);

            LineRenderer[] lines = new LineRenderer[3];
            for (int s = 0; s < 2; s++)
            {
                LineRenderer helix = CreateLine(root.transform, "SoarHelix_" + s, 20 + s, false);
                int segs = 18;
                helix.positionCount = segs;
                helix.startWidth = 0.11f;
                helix.endWidth = 0.025f;
                helix.startColor = Color.white;
                helix.endColor = cyan;
                float phase = s * Mathf.PI;
                for (int i = 0; i < segs; i++)
                {
                    float t = (float)i / (segs - 1);
                    float ang = phase + t * Mathf.PI * 3.0f;
                    float y = Mathf.Lerp(1.25f, -2.15f, t);
                    float rx = Mathf.Lerp(0.18f, 0.88f, t);
                    helix.SetPosition(i, new Vector3(Mathf.Cos(ang) * rx, y, 0f));
                }
                lines[s] = helix;
            }

            LineRenderer spearCore = CreateLine(root.transform, "SoarCore", 22, false);
            spearCore.positionCount = 2;
            spearCore.startWidth = 0.20f;
            spearCore.endWidth = 0.03f;
            spearCore.startColor = Color.white;
            spearCore.endColor = cyan;
            spearCore.SetPosition(0, new Vector3(0f, 1.35f, 0f));
            spearCore.SetPosition(1, new Vector3(0f, -1.95f, 0f));
            lines[2] = spearCore;

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(lines, Color.white, 0.14f, duration, Vector3.zero, 1.08f);
        }

        /// <summary>
        /// Expanding crystalline mint-ivory melodic resonance waves emitted while playing Needolin (00:06:29).
        /// </summary>
        public static void SpawnNeedolinWave(Vector3 origin, int pulseIndex)
        {
            GameObject root = new GameObject("CodeVFX_NeedolinWave");
            root.transform.position = origin;
            Color mint = new Color(0.68f, 0.98f, 0.88f, 0.92f);
            Color warmIvory = new Color(1.0f, 0.96f, 0.80f, 0.94f);

            LineRenderer[] rings = new LineRenderer[2];
            for (int r = 0; r < 2; r++)
            {
                LineRenderer lr = CreateLine(root.transform, "MelodyRing_" + r, 19 + r, false);
                int segs = 29;
                lr.positionCount = segs;
                lr.startWidth = r == 0 ? 0.068f : 0.042f;
                lr.endWidth = lr.startWidth;
                lr.startColor = (pulseIndex + r) % 2 == 0 ? mint : warmIvory;
                lr.endColor = Color.white;
                float rx = 0.85f + r * 0.45f;
                float ry = 0.65f + r * 0.34f;
                for (int i = 0; i < segs; i++)
                {
                    float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                    float ripple = 1f + 0.055f * Mathf.Sin(a * 6f + pulseIndex);
                    lr.SetPosition(i, new Vector3(Mathf.Cos(a) * rx * ripple, Mathf.Sin(a) * ry * ripple, 0f));
                }
                rings[r] = lr;
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(rings, mint, 0.06f, 0.56f, new Vector3(0f, 0.25f, 0f), 3.15f);
        }

        /// <summary>
        /// Heavy wide harvest-amber double scythe arc for Reaper Crest melee attacks (00:11:28).
        /// </summary>
        public static void SpawnReaperScytheArc(Vector3 center, int facingDir, float scale = 1.0f)
        {
            Color amber = new Color(1.0f, 0.84f, 0.42f, 0.96f);
            SpawnSlashArc(center, facingDir, 1.75f * scale, 1.38f * scale, 152f, 0.24f, Color.white, 0.20f);
            SpawnSlashArc(center + new Vector3(facingDir * 0.18f, 0f, 0f), facingDir, 2.02f * scale, 1.56f * scale, 146f, 0.16f, amber, 0.22f);
        }

        /// <summary>
        /// Golden silk harvest orb burst pulled from struck enemies while Reaper's Harvest is active (00:10:58).
        /// </summary>
        public static void SpawnReaperHarvestSpark(Vector3 enemyPos, Vector3 playerPos)
        {
            GameObject root = new GameObject("CodeVFX_ReaperHarvest");
            root.transform.position = enemyPos;
            Color harvestGold = new Color(1.0f, 0.88f, 0.46f, 0.96f);
            Vector3 pull = playerPos - enemyPos;

            LineRenderer thread = CreateLine(root.transform, "HarvestThread", 21, false);
            thread.positionCount = 3;
            thread.startWidth = 0.11f;
            thread.endWidth = 0.03f;
            thread.startColor = Color.white;
            thread.endColor = harvestGold;
            Vector3 mid = pull * 0.5f + new Vector3(0f, 0.55f, 0f);
            thread.SetPosition(0, Vector3.zero);
            thread.SetPosition(1, mid);
            thread.SetPosition(2, pull);

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { thread }, harvestGold, 0.10f, 0.26f, Vector3.zero, 1.04f);
        }

        /// <summary>
        /// Long-range thorny emerald-white S-curve whip lash for Witch Crest melee attacks (00:19:56).
        /// </summary>
        public static void SpawnWitchWhipArc(Vector3 origin, int facingDir, float reachScale = 1.0f)
        {
            GameObject root = new GameObject("CodeVFX_WitchWhip");
            root.transform.position = origin;
            Color emerald = new Color(0.46f, 0.96f, 0.68f, 0.96f);
            float dir = facingDir >= 0 ? 1f : -1f;
            float len = 2.85f * reachScale;

            LineRenderer whip = CreateLine(root.transform, "ThornWhip", 21, false);
            int segs = 16;
            whip.positionCount = segs;
            whip.startWidth = 0.14f;
            whip.endWidth = 0.025f;
            whip.startColor = Color.white;
            whip.endColor = emerald;
            for (int i = 0; i < segs; i++)
            {
                float t = (float)i / (segs - 1);
                float x = dir * t * len;
                float y = Mathf.Sin(t * Mathf.PI * 2.1f) * 0.48f * (1f - t * 0.35f);
                whip.SetPosition(i, new Vector3(x, y, 0f));
            }

            LineRenderer echo = CreateLine(root.transform, "ThornWhipEcho", 20, false);
            echo.positionCount = segs;
            echo.startWidth = 0.07f;
            echo.endWidth = 0.015f;
            echo.startColor = emerald;
            echo.endColor = new Color(0.76f, 0.56f, 0.96f, 0.85f);
            for (int i = 0; i < segs; i++)
            {
                float t = (float)i / (segs - 1);
                float x = dir * t * (len * 0.94f);
                float y = -Mathf.Sin(t * Mathf.PI * 1.9f) * 0.34f;
                echo.SetPosition(i, new Vector3(x, y, 0f));
            }

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(new LineRenderer[] { whip, echo }, emerald, 0.12f, 0.22f, new Vector3(dir * 0.6f, 0f, 0f), 1.08f);
        }

        /// <summary>
        /// Violent 360-degree thorny root eruption for Witch Crest Root Leech Bind (00:19:25).
        /// </summary>
        public static void SpawnWitchRootLeechBurst(Vector3 center, float radius)
        {
            GameObject root = new GameObject("CodeVFX_WitchRootLeech");
            root.transform.position = center;
            Color emerald = new Color(0.46f, 0.96f, 0.68f, 0.96f);
            Color violet = new Color(0.74f, 0.54f, 0.96f, 0.92f);

            int rootCount = 8;
            LineRenderer[] lines = new LineRenderer[rootCount + 1];
            for (int r = 0; r < rootCount; r++)
            {
                LineRenderer lr = CreateLine(root.transform, "RootTendril_" + r, 21, false);
                int segs = 10;
                lr.positionCount = segs;
                lr.startWidth = 0.16f;
                lr.endWidth = 0.02f;
                lr.startColor = (r % 2 == 0) ? emerald : Color.white;
                lr.endColor = (r % 2 == 0) ? violet : emerald;
                float baseAng = (r / (float)rootCount) * Mathf.PI * 2f;
                float curveSign = (r % 2 == 0) ? 1f : -1f;
                for (int i = 0; i < segs; i++)
                {
                    float t = (float)i / (segs - 1);
                    float ang = baseAng + Mathf.Sin(t * Mathf.PI) * 0.28f * curveSign;
                    float dist = t * radius;
                    lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * dist, Mathf.Sin(ang) * dist * 0.85f, 0f));
                }
                lines[r] = lr;
            }

            LineRenderer ring = CreateLine(root.transform, "LeechRing", 20, false);
            int rSegs = 25;
            ring.positionCount = rSegs;
            ring.startWidth = 0.08f;
            ring.endWidth = 0.08f;
            ring.startColor = emerald;
            ring.endColor = Color.white;
            for (int i = 0; i < rSegs; i++)
            {
                float a = (float)i / (rSegs - 1) * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius * 0.88f, Mathf.Sin(a) * radius * 0.78f, 0f));
            }
            lines[rootCount] = ring;

            CodeVfxFadeRunner runner = root.AddComponent<CodeVfxFadeRunner>();
            runner.Initialize(lines, emerald, 0.14f, 0.34f, Vector3.zero, 1.18f);
        }
    }
}
