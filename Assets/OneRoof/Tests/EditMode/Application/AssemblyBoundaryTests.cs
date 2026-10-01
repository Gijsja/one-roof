using System.Linq;
using NUnit.Framework;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class AssemblyBoundaryTests
    {
        [Test]
        public void ApplicationAssemblyDoesNotReferenceUnityEngineModules()
        {
            var references = typeof(ApplicationAssembly).Assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name);
            Assert.That(references.Where(name => name == "UnityEngine" || name.StartsWith("UnityEngine.")),
                Is.Empty, "Application must not reference any Unity engine module.");
        }
    }
}
