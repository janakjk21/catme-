using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CatMe.Tests
{
    /// <summary>F8: plants lying on their side are stood up and re-grounded; the bed is shown 1.6x bigger.</summary>
    public sealed class HouseRoomTests
    {
        static Type Room => AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("CatMe.Editor.HouseRoomPersonalization", true);

        [Test]
        public void APlantOnItsSideStandsUpOnTheSameFloor()
        {
            var root = new GameObject("Room");
            try
            {
                var plant = new GameObject("monstera_plant"); plant.transform.SetParent(root.transform, false);
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube); body.transform.SetParent(plant.transform, false);
                body.transform.localScale = new Vector3(2f, .4f, .4f);           // lying along X
                plant.transform.position = new Vector3(3f, .2f, 1f);            // resting on the floor at y = 0
                Room.GetMethod("StandPlantsUpright").Invoke(null, new object[] { root.transform });
                var b = body.GetComponent<Renderer>().bounds;
                Assert.That(b.size.y, Is.EqualTo(2f).Within(.01f), "stands up");
                Assert.That(b.min.y, Is.EqualTo(0f).Within(.01f), "still on the floor");
                Assert.That(b.center.x, Is.EqualTo(3f).Within(.01f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void AnUprightPlantIsLeftAlone()
        {
            var root = new GameObject("Room");
            try
            {
                var plant = GameObject.CreatePrimitive(PrimitiveType.Cube); plant.name = "bonsaitree"; plant.transform.SetParent(root.transform, false);
                plant.transform.localScale = new Vector3(.4f, 1.2f, .4f);
                Room.GetMethod("StandPlantsUpright").Invoke(null, new object[] { root.transform });
                Assert.That(plant.GetComponent<Renderer>().bounds.size.y, Is.EqualTo(1.2f).Within(.01f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void BedIsAboutOnePointSixTimesBigger() => Assert.That((float)Room.GetField("BedScale").GetValue(null), Is.EqualTo(1.6f).Within(.01f));
    }
}
