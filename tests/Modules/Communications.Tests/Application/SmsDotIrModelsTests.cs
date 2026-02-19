using Vitarish.Communications.Infrastructure.Wrappers.SmsDotIr;

namespace Communications.Tests.Application;

/// <summary>
/// Unit tests for SmsDotIr API model classes.
/// </summary>
public sealed class SmsDotIrModelsTests
{
    #region SmsDotIrSendResult Tests

    [Fact]
    public void SmsDotIrSendResult_PrimaryMessageId_ReturnsFirstPositiveId()
    {
        // Arrange
        SmsDotIrSendResultData data = new(
            Guid.NewGuid(),
            MessageIds: [0, 123, 456],
            Cost: 100m);
        SmsDotIrSendResult result = new(data, Status: 1, Message: "Success");

        // Act
        long primaryId = result.PrimaryMessageId;

        // Assert
        Assert.Equal(123, primaryId);
    }

    [Fact]
    public void SmsDotIrSendResult_PrimaryMessageId_ReturnsZeroWhenNoPositiveIds()
    {
        // Arrange
        SmsDotIrSendResultData data = new(
            Guid.NewGuid(),
            MessageIds: [0, -1, -100],
            Cost: 100m);
        SmsDotIrSendResult result = new(data, Status: 1, Message: "Success");

        // Act
        long primaryId = result.PrimaryMessageId;

        // Assert
        Assert.Equal(0, primaryId);
    }

    [Fact]
    public void SmsDotIrSendResult_PrimaryMessageId_ReturnsZeroWhenMessageIdsNull()
    {
        // Arrange
        SmsDotIrSendResultData data = new(
            Guid.NewGuid(),
            MessageIds: null,
            Cost: 100m);
        SmsDotIrSendResult result = new(data, Status: 1, Message: "Success");

        // Act
        long primaryId = result.PrimaryMessageId;

        // Assert
        Assert.Equal(0, primaryId);
    }

    [Fact]
    public void SmsDotIrSendResult_PrimaryMessageId_ReturnsZeroWhenDataNull()
    {
        // Arrange
        SmsDotIrSendResult result = new(Data: null, Status: 0, Message: "Error");

        // Act
        long primaryId = result.PrimaryMessageId;

        // Assert
        Assert.Equal(0, primaryId);
    }

    [Fact]
    public void SmsDotIrSendResult_PrimaryMessageId_ReturnsZeroWhenMessageIdsEmpty()
    {
        // Arrange
        SmsDotIrSendResultData data = new(
            Guid.NewGuid(),
            MessageIds: Array.Empty<long>(),
            Cost: 100m);
        SmsDotIrSendResult result = new(data, Status: 1, Message: "Success");

        // Act
        long primaryId = result.PrimaryMessageId;

        // Assert
        Assert.Equal(0, primaryId);
    }

    [Fact]
    public void SmsDotIrSendResult_CanBeCreated_WithAllProperties()
    {
        // Arrange
        SmsDotIrSendResultData data = new(
            Guid.NewGuid(),
            MessageIds: [123, 456],
            Cost: 50.5m);

        // Act
        SmsDotIrSendResult result = new(data, Status: 1, Message: "Success");

        // Assert
        Assert.Equal(data, result.Data);
        Assert.Equal(1, result.Status);
        Assert.Equal("Success", result.Message);
    }

    #endregion

    #region SmsDotIrSendResultData Tests

    [Fact]
    public void SmsDotIrSendResultData_CanBeCreated_WithAllProperties()
    {
        // Arrange
        Guid packId = Guid.NewGuid();
        IReadOnlyList<long> messageIds = [123, 456, 789];
        decimal cost = 150.75m;

        // Act
        SmsDotIrSendResultData data = new(packId, messageIds, cost);

        // Assert
        Assert.Equal(packId, data.PackId);
        Assert.Equal(messageIds, data.MessageIds);
        Assert.Equal(cost, data.Cost);
    }

    #endregion

    #region SmsDotIrDeliveryState Tests

    [Fact]
    public void SmsDotIrDeliveryState_CanBeCreated_WithAllProperties()
    {
        // Arrange
        SmsDotIrDeliveryStateData data = new(
            MessageId: 123,
            Mobile: "+15551234567",
            MessageText: "Hello",
            SendDateTime: 1628683626,
            LineNumber: 30004505000017,
            Cost: 50m,
            DeliveryState: 2,
            DeliveryDateTime: 1628683629);

        // Act
        SmsDotIrDeliveryState state = new(data, Status: 1, Message: "موفق");

        // Assert
        Assert.Equal(data, state.Data);
        Assert.Equal(1, state.Status);
        Assert.Equal("موفق", state.Message);
    }

    [Fact]
    public void SmsDotIrDeliveryState_CanBeCreated_WithNullData()
    {
        // Act
        SmsDotIrDeliveryState state = new(Data: null, Status: 0, Message: "Error");

        // Assert
        Assert.Null(state.Data);
        Assert.Equal(0, state.Status);
        Assert.Equal("Error", state.Message);
    }

    #endregion

    #region SmsDotIrDeliveryStateData Tests

    [Fact]
    public void SmsDotIrDeliveryStateData_CanBeCreated_WithAllProperties()
    {
        // Arrange & Act
        SmsDotIrDeliveryStateData data = new(
            MessageId: 123456,
            Mobile: "+15551234567",
            MessageText: "Test message",
            SendDateTime: 1628683626,
            LineNumber: 30004505000017,
            Cost: 75.5m,
            DeliveryState: 2,
            DeliveryDateTime: 1628683700);

        // Assert
        Assert.Equal(123456, data.MessageId);
        Assert.Equal("+15551234567", data.Mobile);
        Assert.Equal("Test message", data.MessageText);
        Assert.Equal(1628683626, data.SendDateTime);
        Assert.Equal(30004505000017, data.LineNumber);
        Assert.Equal(75.5m, data.Cost);
        Assert.Equal(2, data.DeliveryState);
        Assert.Equal(1628683700, data.DeliveryDateTime);
    }

    [Fact]
    public void SmsDotIrDeliveryStateData_CanBeCreated_WithNullablePropertiesAsNull()
    {
        // Arrange & Act
        SmsDotIrDeliveryStateData data = new(
            MessageId: 123,
            Mobile: null,
            MessageText: null,
            SendDateTime: null,
            LineNumber: null,
            Cost: null,
            DeliveryState: null,
            DeliveryDateTime: null);

        // Assert
        Assert.Equal(123, data.MessageId);
        Assert.Null(data.Mobile);
        Assert.Null(data.MessageText);
        Assert.Null(data.SendDateTime);
        Assert.Null(data.LineNumber);
        Assert.Null(data.Cost);
        Assert.Null(data.DeliveryState);
        Assert.Null(data.DeliveryDateTime);
    }

    [Fact]
    public void SmsDotIrDeliveryStateData_RecordEquality_Works()
    {
        // Arrange
        SmsDotIrDeliveryStateData data1 = new(123, "+1555", "Hello", 1000, 30004505, 50m, 2, 1001);
        SmsDotIrDeliveryStateData data2 = new(123, "+1555", "Hello", 1000, 30004505, 50m, 2, 1001);
        SmsDotIrDeliveryStateData data3 = new(456, "+1555", "Hello", 1000, 30004505, 50m, 2, 1001);

        // Assert
        Assert.Equal(data1, data2);
        Assert.NotEqual(data1, data3);
    }

    #endregion
}
