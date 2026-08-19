using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Moq;
using Ringly.Asterisk.Brokers;
using Ringly.Samples.WebApi.Brokers.Storages;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyCalls;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyCalls;

public partial class TelephonyCallServiceTests
{
    private readonly Mock<IStorageBroker> storageBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly ITelephonyCallService telephonyCallService;

    public TelephonyCallServiceTests()
    {
        this.storageBrokerMock = new Mock<IStorageBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.telephonyCallService = new TelephonyCallService(
            storageBroker: this.storageBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static SqlException CreateSqlException() =>
        (SqlException)RuntimeHelpers.GetUninitializedObject(typeof(SqlException));

    private static TelephonyCall CreateRandomTelephonyCall() =>
        CreateTelephonyCallFiller().Create();

    private static IQueryable<TelephonyCall> CreateRandomTelephonyCalls() =>
        CreateTelephonyCallFiller().Create(count: GetRandomNumber()).AsQueryable();

    private static int GetRandomNumber() => new IntRange(min: 2, max: 10).GetValue();

    private static Filler<TelephonyCall> CreateTelephonyCallFiller()
    {
        var filler = new Filler<TelephonyCall>();

        filler.Setup()
            .OnProperty(call => call.TripId).Use((Guid?)null)
            .OnProperty(call => call.StartedAt).Use(DateTimeOffset.UtcNow)
            .OnProperty(call => call.EndedAt).Use((DateTimeOffset?)null);

        return filler;
    }
}
