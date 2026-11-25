using System.Collections.Generic;
using UnityEngine;

namespace KingOrDeath
{
    public static class ModelFactory
    {
        public static readonly Color Teal = new Color(.12f, .46f, .48f),
            Red = new Color(.52f, .14f, .12f),
            Wood = new Color(.29f, .19f, .11f),
            Stone = new Color(.53f, .54f, .48f),
            Roof = new Color(.17f, .29f, .3f),
            Gold = new Color(.78f, .58f, .28f);
        static Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

        public static Material Material(Color color)
        {
            if (materials.TryGetValue(color, out var m) && m)
                return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader)
                shader = Shader.Find("Standard");
            m = new Material(shader)
            {
                name = "Palette_" + ColorUtility.ToHtmlStringRGB(color),
                color = color,
            };
            if (m.HasProperty("_Smoothness"))
                m.SetFloat("_Smoothness", .18f);
            materials[color] = m;
            return m;
        }

        public static GameObject Part(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 at,
            Vector3 scale,
            Material material
        )
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var c = go.GetComponent<Collider>();
            if (Application.isPlaying)
                Object.Destroy(c);
            else
                Object.DestroyImmediate(c);
            return go;
        }

        public static GameObject Group(string name, Vector3 p)
        {
            var go = new GameObject(name);
            go.transform.position = p;
            return go;
        }

        public static void BoxHit(GameObject go, Vector3 size, Vector3 center)
        {
            var c = go.AddComponent<BoxCollider>();
            c.size = size;
            c.center = center;
        }

        public static GameObject House(string name, Vector3 p)
        {
            var go = Group(name, p);
            var t = go.transform;
            Part(
                "Stone foundation",
                PrimitiveType.Cube,
                t,
                new Vector3(0, .3f, 0),
                new Vector3(5, .6f, 4),
                Material(Stone)
            );
            Part(
                "Plaster walls",
                PrimitiveType.Cube,
                t,
                new Vector3(0, 1.5f, 0),
                new Vector3(4.6f, 2.4f, 3.6f),
                Material(new Color(.71f, .65f, .49f))
            );
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
                Part(
                    "Oak frame",
                    PrimitiveType.Cube,
                    t,
                    new Vector3(x * 2.3f, 1.6f, z * 1.8f),
                    new Vector3(.22f, 2.7f, .22f),
                    Material(Wood)
                );
            RoofMesh(t, new Vector3(0, 2.7f, 0), new Vector3(5.6f, 2.1f, 4.6f), Roof);
            Part(
                "Door",
                PrimitiveType.Cube,
                t,
                new Vector3(0, 1, -1.83f),
                new Vector3(.9f, 1.9f, .1f),
                Material(Wood)
            );
            Part(
                "Chimney",
                PrimitiveType.Cube,
                t,
                new Vector3(1.4f, 3.8f, .6f),
                new Vector3(.6f, 2, .65f),
                Material(Stone)
            );
            for (int x = -1; x <= 1; x += 2)
                Part(
                    "Window",
                    PrimitiveType.Cube,
                    t,
                    new Vector3(x * 1.3f, 1.8f, -1.84f),
                    new Vector3(.6f, .7f, .1f),
                    Material(Gold)
                );
            BoxHit(go, new Vector3(5, 4, 4), new Vector3(0, 2, 0));
            return go;
        }

        public static void RoofMesh(Transform parent, Vector3 at, Vector3 scale, Color color)
        {
            var go = new GameObject("Gabled roof");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localScale = scale;
            var a = new Vector3(-.5f, 0, -.5f);
            var b = new Vector3(.5f, 0, -.5f);
            var c = new Vector3(0, 1, -.5f);
            var d = new Vector3(-.5f, 0, .5f);
            var e = new Vector3(.5f, 0, .5f);
            var f = new Vector3(0, 1, .5f);
            var mesh = new Mesh { name = "RoofMesh" };
            mesh.vertices = new[] { a, c, b, d, e, f, a, d, f, c, c, f, e, b, a, b, e, d };
            mesh.triangles = new[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                6,
                8,
                9,
                10,
                11,
                12,
                10,
                12,
                13,
                14,
                15,
                16,
                14,
                16,
                17,
            };
            mesh.RecalculateNormals();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Material(color);
        }

        public static GameObject Castle(Vector3 p)
        {
            var go = Group("Crownwatch Keep", p);
            var t = go.transform;
            Part(
                "Citadel",
                PrimitiveType.Cube,
                t,
                new Vector3(0, 3, 0),
                new Vector3(8, 6, 7),
                Material(Stone)
            );
            RoofMesh(t, new Vector3(0, 6, 0), new Vector3(9, 3, 8), Roof);
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var tower = Part(
                    "Corner tower",
                    PrimitiveType.Cylinder,
                    t,
                    new Vector3(x * 5, 3.6f, z * 4.3f),
                    new Vector3(3.7f, 3.6f, 3.7f),
                    Material(Stone)
                );
                for (int k = 0; k < 8; k++)
                {
                    float angle = k * Mathf.PI / 4;
                    Part(
                        "Merlon",
                        PrimitiveType.Cube,
                        t,
                        new Vector3(
                            x * 5 + Mathf.Cos(angle) * 1.5f,
                            7.7f,
                            z * 4.3f + Mathf.Sin(angle) * 1.5f
                        ),
                        new Vector3(.65f, 1.2f, .65f),
                        Material(Stone)
                    );
                }
            }
            Part(
                "Gate",
                PrimitiveType.Cube,
                t,
                new Vector3(0, 1.8f, -3.56f),
                new Vector3(2, 3.6f, .2f),
                Material(Wood)
            );
            for (int i = -1; i <= 1; i += 2)
            {
                Part(
                    "Banner",
                    PrimitiveType.Cube,
                    t,
                    new Vector3(i * 2.7f, 4.3f, -3.6f),
                    new Vector3(.9f, 2.8f, .12f),
                    Material(Teal)
                );
                Part(
                    "Gold sigil",
                    PrimitiveType.Cube,
                    t,
                    new Vector3(i * 2.7f, 4.5f, -3.7f),
                    new Vector3(.4f, .7f, .1f),
                    Material(Gold)
                );
            }
            BoxHit(go, new Vector3(14, 9, 12), Vector3.up * 4.5f);
            var building = go.AddComponent<Building>();
            building.footprint = new Vector2(14, 12);
            building.title = "Crownwatch Keep";
            return go;
        }

        public static GameObject Tree(Vector3 p, float scale, bool resource = false)
        {
            var go = Group("Pine", p);
            var t = go.transform;
            Part(
                "Trunk",
                PrimitiveType.Cylinder,
                t,
                Vector3.up * 1.5f,
                new Vector3(.5f, 1.5f, .5f),
                Material(Wood)
            );
            for (int i = 0; i < 3; i++)
            {
                var leaves = Part(
                    "Needles",
                    PrimitiveType.Sphere,
                    t,
                    Vector3.up * (3 + i * .9f),
                    new Vector3(3 - i * .65f, 2.5f, 3 - i * .65f),
                    Material(new Color(.13f + i * .018f, .24f + i * .027f, .18f))
                );
                leaves.transform.rotation = Quaternion.Euler(0, i * 37, 0);
            }
            t.localScale = Vector3.one * scale;
            if (resource)
            {
                go.AddComponent<ResourceNode>();
                BoxHit(go, new Vector3(2, 5, 2), Vector3.up * 2.5f);
            }
            return go;
        }

        public static GameObject Crate(string name, Vector3 p)
        {
            var go = Group(name, p);
            Part(
                "Crate",
                PrimitiveType.Cube,
                go.transform,
                Vector3.up * .5f,
                Vector3.one,
                Material(Wood)
            );
            BoxHit(go, Vector3.one, Vector3.up * .5f);
            return go;
        }

        public static GameObject UnitModel(UnitKind kind, Faction faction, Vector3 p, string name)
        {
            var go = Group(name, p);
            var t = go.transform;
            Color cloth = faction == Faction.Player ? Teal : Red;
            if (kind == UnitKind.Wagon)
            {
                Part(
                    "Cart bed",
                    PrimitiveType.Cube,
                    t,
                    new Vector3(0, .9f, 0),
                    new Vector3(2.2f, .35f, 3),
                    Material(Wood)
                );
                for (int s = -1; s <= 1; s += 2)
                {
                    Part(
                        "Cart side",
                        PrimitiveType.Cube,
                        t,
                        new Vector3(s * 1, 1.4f, 0),
                        new Vector3(.2f, 1, 3),
                        Material(Wood)
                    );
                    for (int z = -1; z <= 1; z += 2)
                    {
                        var wheel = Part(
                            "Wheel",
                            PrimitiveType.Cylinder,
                            t,
                            new Vector3(s * 1.2f, .65f, z),
                            new Vector3(1.2f, .14f, 1.2f),
                            Material(new Color(.16f, .12f, .09f))
                        );
                        wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    }
                }
                Part(
                    "Canvas canopy",
                    PrimitiveType.Capsule,
                    t,
                    new Vector3(0, 1.7f, .1f),
                    new Vector3(1.9f, 1.2f, 2.5f),
                    Material(new Color(.75f, .68f, .5f))
                );
                Part(
                    "Horse body",
                    PrimitiveType.Capsule,
                    t,
                    new Vector3(0, 1.1f, 3.2f),
                    new Vector3(.9f, 1.1f, 1.6f),
                    Material(new Color(.35f, .24f, .16f))
                );
                Part(
                    "Horse head",
                    PrimitiveType.Cube,
                    t,
                    new Vector3(0, 1.9f, 3.8f),
                    new Vector3(.5f, .7f, .8f),
                    Material(Wood)
                );
                for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Part(
                        "Leg",
                        PrimitiveType.Cube,
                        t,
                        new Vector3(x * .32f, .45f, 3.2f + z * .5f),
                        new Vector3(.2f, .9f, .2f),
                        Material(Wood)
                    );
                BoxHit(go, new Vector3(3, 3, 6), new Vector3(0, 1.5f, 1));
            }
            else if (kind == UnitKind.Wolf)
            {
                var fur = Material(new Color(.34f, .36f, .33f));
                Part(
                    "Wolf body",
                    PrimitiveType.Capsule,
                    t,
                    Vector3.up * .7f,
                    new Vector3(.65f, .65f, 1.6f),
                    fur
                );
                Part(
                    "Head",
                    PrimitiveType.Cube,
                    t,
                    new Vector3(0, 1, 1),
                    new Vector3(.6f, .6f, .7f),
                    fur
                );
                for (int x = -1; x <= 1; x += 2)
                {
                    Part(
                        "Ear",
                        PrimitiveType.Cube,
                        t,
                        new Vector3(x * .22f, 1.4f, 1),
                        new Vector3(.17f, .4f, .2f),
                        fur
                    );
                    for (int z = -1; z <= 1; z += 2)
                        Part(
                            "Leg",
                            PrimitiveType.Cube,
                            t,
                            new Vector3(x * .25f, .3f, z * .55f),
                            new Vector3(.18f, .6f, .2f),
                            fur
                        );
                }
                BoxHit(go, new Vector3(1, 1.8f, 2.6f), Vector3.up * .9f);
            }
            else
            {
                Part(
                    "Tunic",
                    PrimitiveType.Capsule,
                    t,
                    Vector3.up * .9f,
                    new Vector3(.7f, .65f, .5f),
                    Material(kind == UnitKind.Worker ? new Color(.57f, .43f, .23f) : cloth)
                );
                Part(
                    "Head",
                    PrimitiveType.Sphere,
                    t,
                    Vector3.up * 1.7f,
                    Vector3.one * .45f,
                    Material(new Color(.72f, .57f, .4f))
                );
                for (int x = -1; x <= 1; x += 2)
                {
                    Part(
                        "Boot",
                        PrimitiveType.Cube,
                        t,
                        new Vector3(x * .2f, .25f, 0),
                        new Vector3(.23f, .5f, .33f),
                        Material(Wood)
                    );
                    Part(
                        "Arm",
                        PrimitiveType.Capsule,
                        t,
                        new Vector3(x * .48f, 1, 0),
                        new Vector3(.23f, .4f, .25f),
                        Material(cloth)
                    );
                }
                if (kind == UnitKind.Swordsman)
                {
                    Part(
                        "Helmet",
                        PrimitiveType.Sphere,
                        t,
                        Vector3.up * 1.85f,
                        new Vector3(.52f, .35f, .5f),
                        Material(Stone)
                    );
                    Part(
                        "Sword",
                        PrimitiveType.Cube,
                        t,
                        new Vector3(.6f, 1.1f, .45f),
                        new Vector3(.11f, .11f, 1.3f),
                        Material(new Color(.8f, .82f, .8f))
                    );
                    Part(
                        "Shield",
                        PrimitiveType.Cube,
                        t,
                        new Vector3(-.6f, 1, .25f),
                        new Vector3(.15f, .9f, .65f),
                        Material(cloth)
                    );
                }
                else
                    Part(
                        "Axe",
                        PrimitiveType.Cube,
                        t,
                        new Vector3(.6f, 1.3f, .25f),
                        new Vector3(.12f, 1.1f, .16f),
                        Material(Wood)
                    );
                BoxHit(go, new Vector3(1.5f, 2.2f, 1.5f), Vector3.up * 1.1f);
            }
            return go;
        }

        public static LineRenderer Ring(string name, Vector3 at, float radius, Color color)
        {
            var go = Group(name, at + Vector3.up * .08f);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 96;
            line.startWidth = line.endWidth = .13f;
            line.sharedMaterial = Material(color);
            for (int i = 0; i < 96; i++)
            {
                float a = i * Mathf.PI * 2 / 96;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius));
            }
            return line;
        }
    }
}
