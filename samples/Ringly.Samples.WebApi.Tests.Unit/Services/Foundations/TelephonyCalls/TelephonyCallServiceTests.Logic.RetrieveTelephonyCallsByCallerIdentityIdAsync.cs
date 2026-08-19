using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldRetrieveTelephonyCallsByCallerIdentityIdAsync()
    {
        // given
        Guid randomCallerIdentityId = Guid.NewGuid();
        IQueryable<TelephonyCall> storageTelephonyCalls = CreateRandomTelephonyCalls();
        IQueryable<TelephonyCall> expectedTelephonyCalls = storageTelephonyCalls;

        this.storageBrokerMock.Setup(broker =>
            broker.SelectTelephonyCallsByCallerIdentityIdAsync(randomCallerIdentityId))
                .ReturnsAsync(storageTelephonyCalls);

        // when
        IQueryable<TelephonyCall> actualTelephonyCalls =
            await this.telephonyCallService.RetrieveTelephonyCallsByCallerIdentityIdAsync(randomCallerIdentityId);

        // then
        actualTelephonyCalls.Should().BeEquivalentTo(expectedTelephonyCalls);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectTelephonyCallsByCallerIdentityIdAsync(randomCallerIdentityId),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
