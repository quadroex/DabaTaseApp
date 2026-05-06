using System.ComponentModel.DataAnnotations;

namespace DabaTaseApp.Validation;

public sealed class DecimalRangeAttribute : ValidationAttribute
{
    private readonly decimal _minimum;
    private readonly decimal _maximum;

    public DecimalRangeAttribute(double minimum, double maximum)
    {
        _minimum = Convert.ToDecimal(minimum);
        _maximum = Convert.ToDecimal(maximum);
    }

    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true;
        }

        return value is decimal amount && amount >= _minimum && amount <= _maximum;
    }
}
