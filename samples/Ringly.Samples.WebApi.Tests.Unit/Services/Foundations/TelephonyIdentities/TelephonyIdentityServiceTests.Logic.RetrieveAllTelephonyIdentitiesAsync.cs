using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    [Fact]
    public async Task ShouldRetrieveAllTelephonyIdentitiesAsync()
    {
        // given
        IQueryable<TelephonyIdentity> storageTelephonyIdentities = CreateRandomTelephonyIdentities();
        IQueryable<TelephonyIdentity> expectedTelephonyIdentities = storageTelephonyIdentities;

        this.storageBrokerMock.Setup(broker =>
            broker.SelectAllTelephonyIdentitiesAsync())
                .ReturnsAsync(storageTelephonyIdentities);

        // when
        IQueryable<TelephonyIdentity> actualTelephonyIdentities =
            await this.telephonyIdentityService.RetrieveAllTelephonyIdentitiesAsync();

        // then
        actualTelephonyIdentities.Should().BeEquivalentTo(expectedTelephonyIdentities);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectAllTelephonyIdentitiesAsync(),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
