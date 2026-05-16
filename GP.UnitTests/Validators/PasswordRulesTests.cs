using System.Linq;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using GP.Application.Validators;
using Xunit;

namespace GP.UnitTests.Validators;

public class PasswordRulesTests
{
    private class PasswordTestModel
    {
        public string Password { get; set; } = string.Empty;
    }

    private class PasswordTestValidator : AbstractValidator<PasswordTestModel>
    {
        public PasswordTestValidator()
        {
            RuleFor(x => x.Password).PasswordRules();
        }
    }

    private static ValidationResult Validate(string password)
    {
        var validator = new PasswordTestValidator();
        return validator.Validate(new PasswordTestModel { Password = password });
    }

    [Theory]
    [InlineData("Aa1", "Password must be at least 8 characters")]
    [InlineData("Abcdefgh", "Password must contain at least one digit")]
    [InlineData("abcdefg1", "Password must contain at least one uppercase letter")]
    [InlineData("ABCDEFG1", "Password must contain at least one lowercase letter")]
    public void PasswordRules_Rejects_Invalid_Passwords(string password, string expectedMessage)
    {
        var result = Validate(password);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain(expectedMessage);
    }

    [Fact]
    public void PasswordRules_Accepts_Valid_Password()
    {
        var result = Validate("Abcdefg1");

        result.IsValid.Should().BeTrue();
    }
}
