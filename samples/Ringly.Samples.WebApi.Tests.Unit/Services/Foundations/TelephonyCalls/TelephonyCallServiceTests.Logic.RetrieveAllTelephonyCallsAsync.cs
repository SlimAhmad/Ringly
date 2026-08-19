using FluentAssertions;
using Moq;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    [Fact]
    public async Task ShouldRetrieveAllTelephonyCallsAsync()
    {
        // given
        IQueryable<TelephonyCall> storageTelephonyCalls = CreateRandomTelephonyCalls();
        IQueryable<TelephonyCall> expectedTelephonyCalls = storageTelephonyCalls;

        this.storageBrokerMock.Setup(broker =>
            broker.SelectAllTelephonyCallsAsync())
                .ReturnsAsync(storageTelephonyCalls);

        // when
        IQueryable<TelephonyCall> actualTelephonyCalls =
            await this.telephonyCallService.RetrieveAllTelephonyCallsAsync();

        // then
        actualTelephonyCalls.Should().BeEquivalentTo(expectedTelephonyCalls);

        this.storageBrokerMock.Verify(broker =>
            broker.SelectAllTelephonyCallsAsync(),
                Times.Once);

        this.storageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
