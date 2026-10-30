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
/// what the converter of that type already decided. The text is written and read by whoever writes the wire, and
/// by nothing of its own, so that the text a route carries is the one a body carries and what a caller may put
/// in either cannot come apart.
/// </remarks>
/// <param name="valueType">The type the value is declared as.</param>
/// <param name="carrier">What it travels as, which says whether its text is a text, a number or a truth.</param>
/// <param name="wire">The wire it travels on, whose writer writes and reads the text.</param>
public sealed class ShapeTypeConverter(Type valueType, Carrier carrier, Wire wire)
    : TypeConverter
{
    /// <summary>
    /// Whether a value written as this type can be read out of text at all. It is asked of the type itself, so
    /// that a form nobody can type does not end up declared as one that can.
    /// </summary>
    public static bool ReadsFromText(Type carrier) =>
        TypeDescriptor.GetConverter(carrier).CanConvertFrom(typeof(string));

    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string text)
        {
            return base.ConvertFrom(context, culture, value);
        }

        return wire.ReadFromText(valueType, carrier.Kind, text);
    }

    /// <inheritdoc/>
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

        return wire.WriteAsText(valueType, value);
    }
}
