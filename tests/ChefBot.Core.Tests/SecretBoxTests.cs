using ChefBot.Core.ResourceAccess;
using Microsoft.AspNetCore.DataProtection;

namespace ChefBot.Core.Tests;

/// <summary>AI providers' API keys are stored encrypted.</summary>
public class SecretBoxTests
{
    [Test]
    public async Task A_sealed_key_opens_again_but_isnt_stored_as_it_was()
    {
        var box = new SecretBox(new EphemeralDataProtectionProvider());

        var stored = box.Seal("sk-secret-key");

        await Assert.That(stored).StartsWith(SecretBox.Prefix);
        await Assert.That(stored).DoesNotContain("sk-secret-key");
        await Assert.That(SecretBox.IsPlain(stored)).IsFalse();
        await Assert.That(box.Open(stored)).IsEqualTo("sk-secret-key");
    }

    [Test]
    public async Task No_key_stays_empty_and_an_old_plain_key_still_reads()
    {
        var box = new SecretBox(new EphemeralDataProtectionProvider());

        await Assert.That(box.Seal(string.Empty)).IsEqualTo(string.Empty);
        await Assert.That(SecretBox.IsPlain(string.Empty)).IsFalse();
        await Assert.That(SecretBox.IsPlain("sk-old")).IsTrue();
        await Assert.That(box.Open("sk-old")).IsEqualTo("sk-old");
    }

    [Test]
    public async Task A_key_sealed_by_another_chefbot_cant_be_read()
    {
        var stored = new SecretBox(new EphemeralDataProtectionProvider()).Seal("sk-secret-key");

        await Assert.That(new SecretBox(new EphemeralDataProtectionProvider()).Open(stored)).IsNull();
    }
}
