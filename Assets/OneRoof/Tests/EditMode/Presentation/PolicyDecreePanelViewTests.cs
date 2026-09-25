using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Application.Tower;
using OneRoof.UI.Management;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class PolicyDecreePanelViewTests
    {
        [Test]
        public void PanelKeyboardSelectionsRemainDraftUntilConfirm()
        {
            var holder = new GameObject("PolicyPanelTest");
            try
            {
                var session = new TowerSimulationSession();
                var modes = new ModeShellSession();
                var panel = holder.AddComponent<PolicyDecreePanelView>();
                panel.Bind(session, modes);
                var original = panel.Draft.Current;
                panel.Step(-1);
                Assert.That(panel.Draft.Draft.RentCapMultiplier, Is.EqualTo(.7f));
                Assert.That(panel.Draft.Current, Is.SameAs(original));
                panel.Confirm();
                Assert.That(panel.Draft.Current.RentCapMultiplier, Is.EqualTo(.7f));
                Assert.That(panel.Message, Does.Contain("enacted"));
                panel.Confirm();
                Assert.That(panel.Message, Does.Contain("policy:no_change"));
            }
            finally { Object.DestroyImmediate(holder); }
        }
    }
}
