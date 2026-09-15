using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class PlayModeTestRunnerSmokeTests
    {
        [UnityTest]
        public IEnumerator PlayModeTestRunnerCanAdvanceOneFrame()
        {
            yield return null;

            Assert.That(true, Is.True);
        }
    }
}
