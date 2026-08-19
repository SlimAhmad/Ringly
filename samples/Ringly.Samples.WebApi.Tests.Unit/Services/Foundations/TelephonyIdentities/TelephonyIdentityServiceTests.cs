using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Moq;
using Ringly.Asterisk.Brokers;
using Ringly.Samples.WebApi.Brokers.Storages;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyIdentities;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyIdentities;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyIdentities;

public partial class TelephonyIdentityServiceTests
{
    private readonly Mock<IStorageBroker> storageBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly ITelephonyIdentityService telephonyIdentityService;

    public TelephonyIdentityServiceTests()
    {
        this.storageBrokerMock = new Mock<IStorageBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.telephonyIdentityService = new TelephonyIdentityService(
            storageBroker: this.storageBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static SqlException CreateSqlException() =>
        (SqlException)RuntimeHelpers.GetUninitializedObject(typeof(SqlException));

    private static TelephonyIdentity CreateRandomTelephonyIdentity() =>
        CreateTelephonyIdentityFiller().Create();

    private static IQueryable<TelephonyIdentity> CreateRandomTelephonyIdentities() =>
        CreateTelephonyIdentityFiller().Create(count: GetRandomNumber()).AsQueryable();

    private static int GetRandomNumber() => new IntRange(min: 2, max: 10).GetValue();

    private static Filler<TelephonyIdentity> CreateTelephonyIdentityFiller()
    {
        var filler = new Filler<TelephonyIdentity>();
        filler.Setup();

        return filler;
    }
}
