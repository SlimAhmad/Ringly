using System.Net;
using FluentAssertions;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

namespace Ringly.Asterisk.Tests.Acceptance.Brokers;

public partial class AsteriskBrokerAcceptanceTests
{
    // AddSipEndpointConfigAsync writes aor -> auth (ARI PUT) -> endpoint (direct Postgres INSERT,
    // see AsteriskBroker.InsertSipEndpointObjectAsync). The endpoint step used to fail
    // unconditionally via ARI's PUT — res_config_pgsql never quotes SQL identifiers like the
    // column "100rel" (asterisk/asterisk#1655), a confirmed upstream bug with no
    // application-level ARI-based workaround — fixed by writing directly into the same
    // ps_endpoints table Asterisk's own realtime lookup reads from, bypassing that write path
    // entirely. This test is the real regression guard for that fix: confirms all three objects
    // now genuinely exist after a single Add call.
    [Fact]
    public async Task ShouldAddSipEndpointConfigAsync()
    {
        // given
        string extension = CreateRandomExtension();

        var inputConfig = new SipEndpointConfig
        {
            Extension = extension,
            Password = CreateRandomPassword()
        };

        try
        {
            // when
            await this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(inputConfig);

            // then
            using HttpResponseMessage aorResponse =
                await this.rawAriClient.GetAsync($"ari/asterisk/config/dynamic/res_pjsip/aor/{extension}");

            using HttpResponseMessage authResponse =
                await this.rawAriClient.GetAsync($"ari/asterisk/config/dynamic/res_pjsip/auth/{extension}");

            using HttpResponseMessage endpointResponse =
                await this.rawAriClient.GetAsync($"ari/asterisk/config/dynamic/res_pjsip/endpoint/{extension}");

            aorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            authResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            endpointResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            string authBody = await authResponse.Content.ReadAsStringAsync();
            authBody.Should().Contain(inputConfig.Password);

            string endpointBody = await endpointResponse.Content.ReadAsStringAsync();
            endpointBody.Should().Contain(extension);
        }
        finally
        {
            await this.DeleteSipEndpointConfigAsync(extension);
        }
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnAddIfExtensionAlreadyExistsAsync()
    {
        // given
        string extension = CreateRandomExtension();

        var firstConfig = new SipEndpointConfig
        {
            Extension = extension,
            Password = CreateRandomPassword()
        };

        var secondConfig = new SipEndpointConfig
        {
            Extension = extension,
            Password = CreateRandomPassword()
        };

        try
        {
            await this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(firstConfig);

            // when
            ValueTask secondAddTask =
                this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(secondConfig);

            // then — proves Asterisk's real PUT semantics (silent upsert, no native 409) require
            // our own pre-insert collision check; a naive re-insert would otherwise have silently
            // overwritten the first registrant's password.
            SipEndpointConfigDependencyValidationException actualException =
                await Assert.ThrowsAsync<SipEndpointConfigDependencyValidationException>(
                    secondAddTask.AsTask);

            actualException.InnerException.Should().BeOfType<DuplicateExtensionException>();

            using HttpResponseMessage authResponse =
                await this.rawAriClient.GetAsync($"ari/asterisk/config/dynamic/res_pjsip/auth/{extension}");

            string authBody = await authResponse.Content.ReadAsStringAsync();
            authBody.Should().Contain(firstConfig.Password);
            authBody.Should().NotContain(secondConfig.Password);
        }
        finally
        {
            await this.DeleteSipEndpointConfigAsync(extension);
        }
    }

    // Confirms the real remove path end-to-end: all three objects (now that Add genuinely
    // creates all three, including "endpoint") disappear from Asterisk's realtime config after
    // RemoveSipEndpointConfigAsync.
    [Fact]
    public async Task ShouldRemoveSipEndpointConfigAsync()
    {
        // given
        string extension = CreateRandomExtension();

        var inputConfig = new SipEndpointConfig
        {
            Extension = extension,
            Password = CreateRandomPassword()
        };

        await this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(inputConfig);

        // when
        await this.sipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync(extension);

        // then
        using HttpResponseMessage aorResponse =
            await this.rawAriClient.GetAsync($"ari/asterisk/config/dynamic/res_pjsip/aor/{extension}");

        using HttpResponseMessage authResponse =
            await this.rawAriClient.GetAsync($"ari/asterisk/config/dynamic/res_pjsip/auth/{extension}");

        using HttpResponseMessage endpointResponse =
            await this.rawAriClient.GetAsync($"ari/asterisk/config/dynamic/res_pjsip/endpoint/{extension}");

        aorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        authResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        endpointResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnRemoveIfExtensionNotFoundAsync()
    {
        // given
        string extension = CreateRandomExtension();

        // when
        ValueTask removeTask = this.sipEndpointConfigFoundationService.RemoveSipEndpointConfigAsync(extension);

        // then
        SipEndpointConfigDependencyValidationException actualException =
            await Assert.ThrowsAsync<SipEndpointConfigDependencyValidationException>(removeTask.AsTask);

        actualException.InnerException.Should().BeOfType<ExtensionNotFoundException>();
    }
}
