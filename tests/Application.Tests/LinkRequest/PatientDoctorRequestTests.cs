using Domain.Enums;
using Domain.Models;

namespace Application.Tests.LinkRequest;

public class PatientDoctorRequestTests
{
    private static PatientDoctorRequest CreatePending() => new()
    {
        Id = Guid.NewGuid(),
        PatientId = Guid.NewGuid(),
        DoctorId = Guid.NewGuid(),
        Status = RequestStatus.Pending,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public void Accept_WhenPending_TransitionsAndStampsResolvedAt()
    {
        var request = CreatePending();
        var now = DateTime.UtcNow;

        var result = request.Accept(now);

        Assert.True(result);
        Assert.Equal(RequestStatus.Accepted, request.Status);
        Assert.Equal(now, request.ResolvedAt);
    }

    [Fact]
    public void Accept_WhenNotPending_ReturnsFalseWithoutMutating()
    {
        var request = CreatePending();
        request.Status = RequestStatus.Rejected;

        var result = request.Accept(DateTime.UtcNow);

        Assert.False(result);
        Assert.Equal(RequestStatus.Rejected, request.Status);
        Assert.Null(request.ResolvedAt);
    }

    [Fact]
    public void Reject_WhenPending_Transitions()
    {
        var request = CreatePending();

        Assert.True(request.Reject(DateTime.UtcNow));
        Assert.Equal(RequestStatus.Rejected, request.Status);
    }

    [Fact]
    public void Unlink_WhenAccepted_Transitions()
    {
        var request = CreatePending();
        request.Status = RequestStatus.Accepted;

        Assert.True(request.Unlink(DateTime.UtcNow));
        Assert.Equal(RequestStatus.Unlinked, request.Status);
    }

    [Fact]
    public void Revoke_WhenAccepted_Transitions()
    {
        var request = CreatePending();
        request.Status = RequestStatus.Accepted;

        Assert.True(request.Revoke(DateTime.UtcNow));
        Assert.Equal(RequestStatus.Revoked, request.Status);
    }

    [Fact]
    public void Unlink_WhenNotAccepted_ReturnsFalse()
    {
        var request = CreatePending();

        Assert.False(request.Unlink(DateTime.UtcNow));
        Assert.Equal(RequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Accept_WithLocalDateTime_Throws()
    {
        var request = CreatePending();
        var localNow = DateTime.Now;

        var ex = Assert.Throws<ArgumentException>(() => request.Accept(localNow));
        Assert.Contains("must be UTC", ex.Message);
        Assert.Equal(RequestStatus.Pending, request.Status);
        Assert.Null(request.ResolvedAt);
    }

    [Fact]
    public void Accept_WithUnspecifiedDateTime_Throws()
    {
        var request = CreatePending();
        var unspecified = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(() => request.Accept(unspecified));
    }
}
