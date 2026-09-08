using FluentAssertions;
using FluentValidation;
using Maintenance.Application.Validation;
using ValidationException = Maintenance.Application.Exceptions.ValidationException;

namespace Maintenance.UnitTests.Validation;

public class ValidationRunnerTests
{
    private sealed record Sample(string DisplayName, int Mileage);

    private sealed class SampleValidator : AbstractValidator<Sample>
    {
        public SampleValidator()
        {
            RuleFor(sample => sample.DisplayName).NotEmpty().MaximumLength(5);
            RuleFor(sample => sample.Mileage).GreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void Validate_ValidInput_DoesNotThrow()
    {
        var act = () => ValidationRunner.Validate(new SampleValidator(), new Sample("ok", 1));

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_InvalidInput_ThrowsWithCamelCasedFieldErrors()
    {
        var act = () => ValidationRunner.Validate(new SampleValidator(), new Sample("too long", -1));

        var errors = act.Should().Throw<ValidationException>().Which.Errors;

        errors.Should().ContainKeys("displayName", "mileage");
        errors["displayName"].Should().ContainSingle();
    }
}
