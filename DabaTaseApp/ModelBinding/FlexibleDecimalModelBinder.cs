using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DabaTaseApp.ModelBinding;

public sealed class FlexibleDecimalModelBinder : IModelBinder
{
    private static readonly Regex StrictDecimalRegex =
        new(@"^\d+([.,]\d{1,2})?$", RegexOptions.Compiled);

    private const string ErrorMessage =
        "Вкажіть коректну суму: додатне число з максимум двома знаками після крапки або коми.";

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
        var rawValue = valueProviderResult.FirstValue;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return Task.CompletedTask;
        }

        var trimmed = rawValue.Trim();

        if (!StrictDecimalRegex.IsMatch(trimmed))
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ErrorMessage);
            return Task.CompletedTask;
        }

        var normalized = trimmed.Replace(',', '.');
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            bindingContext.Result = ModelBindingResult.Success(value);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ErrorMessage);
        }

        return Task.CompletedTask;
    }
}

public sealed class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return type == typeof(decimal) ? new FlexibleDecimalModelBinder() : null;
    }
}
