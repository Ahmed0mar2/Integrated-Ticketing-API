using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GP.Application.DTOs.Auth;
using GP.Application.Interfaces;
using GP.Application.Services;
using GP.Application.Settings;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using GP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GP.UnitTests.Services;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task RegisterAsync_Fails_When_Email_Duplicate()
    {
        var userManagerMock = CreateUserManagerMock();
        var contextMock = CreateDbContextMock(out var connection);
        using var _ = connection;

        userManagerMock
            .Setup(m => m.FindByEmailAsync("dup@example.com"))
            .ReturnsAsync(new ApplicationUser());

        var service = CreateService(userManagerMock, contextMock);
        var request = BuildRequest("dup@example.com", "0123456789");

        var result = await service.RegisterAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email already registered");
    }

    [Fact]
    public async Task RegisterAsync_Fails_When_Phone_Duplicate()
    {
        var userManagerMock = CreateUserManagerMock();
        var contextMock = CreateDbContextMock(out var connection);
        using var _ = connection;

        userManagerMock
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var users = new List<ApplicationUser>
        {
            new ApplicationUser { PhoneNumber = "0123456789" }
        };

        userManagerMock
            .SetupGet(m => m.Users)
            .Returns(new TestAsyncEnumerable<ApplicationUser>(users));

        var service = CreateService(userManagerMock, contextMock);
        var request = BuildRequest("unique@example.com", "0123456789");

        var result = await service.RegisterAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Phone number already registered");
    }

    private static RegisterRequest BuildRequest(string email, string phone)
    {
        return new RegisterRequest
        {
            Email = email,
            Password = "Abcdefg1",
            ConfirmPassword = "Abcdefg1",
            PhoneNumber = phone,
            FirstName = "Test",
            LastName = "User",
            FamilyName = "Family",
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(2000, 1, 1),
            CountryCode = "EG"
        };
    }

    private static AuthenticationService CreateService(
        Mock<UserManager<ApplicationUser>> userManagerMock,
        Mock<ApplicationDbContext> contextMock)
    {
        var jwtOptions = Options.Create(new JwtSettings
        {
            SecretKey = "test-secret-key-1234567890",
            Issuer = "test",
            Audience = "test",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        });

        var emailServiceMock = new Mock<IEmailService>();
        var configurationMock = new Mock<IConfiguration>();

        return new AuthenticationService(
            userManagerMock.Object,
            contextMock.Object,
            jwtOptions,
            emailServiceMock.Object,
            configurationMock.Object);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static Mock<ApplicationDbContext> CreateDbContextMock(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var contextMock = new Mock<ApplicationDbContext>(options) { CallBase = true };
        contextMock.Object.Database.EnsureCreated();

        return contextMock;
    }
}

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression)!;
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
    {
        return new TestAsyncEnumerable<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var result = Execute(expression);

        if (typeof(TResult).IsGenericType
            && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerType = typeof(TResult).GetGenericArguments()[0];
            var task = typeof(Task)
                .GetMethod(nameof(Task.FromResult))
                ?.MakeGenericMethod(innerType)
                .Invoke(null, new[] { result });

            return (TResult)task!;
        }

        return (TResult)result!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    {
    }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    {
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_inner.MoveNext());
    }
}
