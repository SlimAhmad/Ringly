using System.Net;
using FluentAssertions;
using Ringly.Abstractions.Models;
using Ringly.Asterisk.Models.Foundations.SipEndpoints.Exceptions;

namespace Ringly.Asterisk.Tests.Acceptance.Brokers;

public partial class AsteriskBrokerAcceptanceTests
{
    // AddSipEndpointConfigAsync writes aor -> auth -> endpoint (see AsteriskBroker.Credentials.cs).
    // The endpoint step is currently blocked by a confirmed, still-open upstream Asterisk bug:
    // res_config_pgsql never quotes SQL identifiers (https://github.com/asterisk/asterisk/issues/1655),
    // and the "endpoint" sorcery object always includes columns like "100rel" that Postgres rejects
    // as invalid unquoted identifiers — every realtime endpoint creation fails, regardless of what
    // fields we send. There is no application-level workaround (confirmed on the upstream issue).
    //
    // aor and auth creation (few fields, no problematic names) DO work end-to-end once fixed —
    // this test proves that real path (URL routing under /ari/, Newtonsoft-based field-name
    // casing, {"fields": [...]} body wrapping, and "application/json" media type all confirmed
    // against the live server) while documenting the endpoint blocker precisely.
    [Fact]
    public async Task ShouldPersistAorAndAuthButFailOnEndpointDueToKnownAsteriskPgsqlBugAsync()
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
            ValueTask addTask = this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(inputConfig);

            // then
            SipEndpointConfigDependencyException actualException =
                await Assert.ThrowsAsync<SipEndpointConfigDependencyException>(addTask.AsTask);

            actualException.Message.Should().NotBeNullOrEmpty();

            using HttpResponseMessage aorResponse =
                await this.rawAriClient.GetAsync($"ari/asterisk/config/dynamic/res_pjsip/aor/{extension}");

            using HttpResponseMessage authResponse =
                await this.rawAriClient.GetAsync($"ari/asterisk/config/dynamic/res_pjsip/auth/{extension}");

            using HttpResponseMessage endpointResponse =
                await this.rawAriClient.GetAsync($"ari/asterisk/config/dynamic/res_pjsip/endpoint/{extension}");

            aorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            authResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            endpointResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

            string authBody = await authResponse.Content.ReadAsStringAsync();
            authBody.Should().Contain(inputConfig.Password);
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
            // The first add still fails on the endpoint step (see above) but its aor/auth writes
            // land first — enough for the pre-insert existence check (a GET on "aor") to see the
            // extension as already provisioned on the second attempt.
            ValueTask firstAddTask =
                this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(firstConfig);

            await Assert.ThrowsAsync<SipEndpointConfigDependencyException>(firstAddTask.AsTask);

            // when
            ValueTask secondAddTask =
                this.sipEndpointConfigFoundationService.AddSipEndpointConfigAsync(secondConfig);

            // then — proves Asterisk's real PUT semantics (silent upsert, no native 409) require
            // our own pre-insert collision check; a naive re-PUT would otherwise have silently
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
}
