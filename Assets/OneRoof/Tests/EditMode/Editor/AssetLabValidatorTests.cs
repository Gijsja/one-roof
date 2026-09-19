using NUnit.Framework;
using OneRoof.Editor.AssetLab;

namespace OneRoof.Editor.Tests.EditMode
{
    public sealed class AssetLabValidatorTests
    {
        [Test]
        public void Validate_ReleasedContent_HasNoSeamRigOrAnchorErrors()
        {
            Assert.That(AssetLabValidator.Validate(), Is.Empty);
        }
    }
}
