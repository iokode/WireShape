using System;
using System.ComponentModel;
using System.Globalization;

namespace IOKode.WireShape.ComponentModel;

/// <summary>
/// Reads a value out of text, in the form a wire declares that value travels in, and writes it back as that
/// same text.
/// </summary>
/// <remarks>
/// One of these serves every type that travels as a value of its own, instead of one class per type saying again
/// what the converter of that type already decided. The text is read as the declared type and the value is then
/// built by whoever writes it, so that what a caller may put in text and what they may put in a document of
/// their own cannot come apart.
/// </remarks>
/// <param name="valueType">The type the value is declared as.</param>
/// <param name="carrier">The type it travels as.</param>
/// <param name="wire">The wire it travels on, which is what turns one of the two into the other.</param>
public sealed class ShapeTypeConverter(Type valueType, Type carrier, Wire wire)
    : TypeConverter
{
    /// <summary>
    /// Whether a value written as this type can be read out of text at all. It is asked of the type itself, so
    /// that a form nobody can type does not end up declared as one that can.
    /// </summary>
    public static bool ReadsFromText(Type carrier) =>
        TypeDescriptor.GetConverter(carrier).CanConvertFrom(typeof(string));

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string text)
        {
            return base.ConvertFrom(context, culture, value);
        }

        var carried = TypeDescriptor.GetConverter(carrier).ConvertFromInvariantString(text);

        return wire.ReadFromCarrier(valueType, carrier, carried);
    }

    public override object? ConvertTo(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value,
        Type destinationType)
    {
        if (destinationType != typeof(string) || value == null)
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }

        var carried = wire.WriteAsCarrier(valueType, carrier, value);

        return TypeDescriptor.GetConverter(carrier).ConvertToInvariantString(carried);
    }
}
