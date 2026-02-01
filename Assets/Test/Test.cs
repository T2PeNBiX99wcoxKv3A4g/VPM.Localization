using io.github.ykysnk.utils;
using io.github.ykysnk.utils.Extensions;
using UnityEngine;

namespace Test
{
    public class Test : MonoBehaviour
    {
        public string? text;

        [ContextMenu("TestRun")]
        private void TestRun()
        {
            Utils.Log(nameof(TestRun), $"{"unity:Position".LastPath("unity:")}");
        }
    }
}