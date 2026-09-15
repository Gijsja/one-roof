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

            Assert.That(references, Does.Not.Contain("UnityEngine"));
        }
    }
}
