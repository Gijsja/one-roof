using System.Linq;
using NUnit.Framework;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class AssemblyBoundaryTests
    {
        [Test]
        public void DomainAssemblyDoesNotReferenceUnityEngine()
        {
            var references = typeof(DomainAssembly).Assembly
                .GetReferencedAssemblies()
                .Select(reference => reference.Name);

            Assert.That(references.Where(name => name == "UnityEngine" || name.StartsWith("UnityEngine.")),
                Is.Empty, "Domain must not reference any Unity engine module.");
        }
    }
}
