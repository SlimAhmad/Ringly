using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Moq;
using Ringly.Asterisk.Brokers;
using Ringly.Samples.WebApi.Brokers.Storages;
using Ringly.Samples.WebApi.Models.Foundations.TelephonyDevices;
using Ringly.Samples.WebApi.Services.Foundations.TelephonyDevices;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Ringly.Samples.WebApi.Tests.Unit.Services.Foundations.TelephonyDevices;

public partial class TelephonyDeviceServiceTests
{
    private readonly Mock<IStorageBroker> storageBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly ITelephonyDeviceService telephonyDeviceService;

    public TelephonyDeviceServiceTests()
    {
        this.storageBrokerMock = new Mock<IStorageBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.telephonyDeviceService = new TelephonyDeviceService(
            storageBroker: this.storageBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);

    private static SqlException CreateSqlException() =>
        (SqlException)RuntimeHelpers.GetUninitializedObject(typeof(SqlException));

    private static TelephonyDevice CreateRandomTelephonyDevice() =>
        CreateTelephonyDeviceFiller().Create();

    private static IQueryable<TelephonyDevice> CreateRandomTelephonyDevices() =>
        CreateTelephonyDeviceFiller().Create(count: GetRandomNumber()).AsQueryable();

    private static int GetRandomNumber() => new IntRange(min: 2, max: 10).GetValue();

    private static Filler<TelephonyDevice> CreateTelephonyDeviceFiller()
    {
        var filler = new Filler<TelephonyDevice>();

        filler.Setup()
            .OnProperty(device => device.LastRegisteredAt).Use(DateTimeOffset.UtcNow)
            .OnProperty(device => device.LastUnregisteredAt).Use((DateTimeOffset?)null);

        return filler;
    }
}
