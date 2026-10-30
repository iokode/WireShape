using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace IOKode.WireShape.ComponentModel;

/// <summary>
/// What a wire declares, said where whoever reads a value out of text looks for it.
/// </summary>
/// <remarks>
/// A type descriptor is of the whole process, so what is taught here is taught once and of one wire. Whatever
/// else the process ever writes in another form has to read what it is given itself.
/// </remarks>
public static class TypeDescriptors
{
    /// <summary>
    /// The converter each value a wire declares is read out of text with, for the values that can be read out
    /// of text at all.
    /// </summary>
    /// <param name="wire">
    /// The wire those values travel on. Whoever reads a value and whoever writes it therefore cannot disagree
    /// about the form it takes, which is the whole point of reading the forms from where they are declared.
    /// </param>
    public static IReadOnlyDictionary<Type, TypeConverter> GetConverters(Wire wire)
    {
        return wire.GetDeclaredShapes()
            .Select(declaration => (Type: declaration.Key, declaration.Value.Shape.Carrier))
            .Where(declaration => declaration.Carrier != null &&
                                  ShapeTypeConverter.ReadsFromText(declaration.Carrier.Type))
            .ToDictionary(
                declaration => declaration.Type,
                declaration => (TypeConverter) new ShapeTypeConverter(
                    declaration.Type,
                    declaration.Carrier!,
                    wire));
    }

    /// <summary>Teaches the process to read and write this type with this converter.</summary>
    public static void Teach(Type type, TypeConverter converter)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(converter);

        TypeDescriptor.AddProvider(new ConverterTypeDescriptionProvider(type, converter), type);
    }

    private sealed class ConverterTypeDescriptionProvider : TypeDescriptionProvider
    {
        private readonly TypeConverter converter;

        public ConverterTypeDescriptionProvider(Type type, TypeConverter converter)
            : base(TypeDescriptor.GetProvider(type))
        {
            this.converter = converter;
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance)
        {
            return new ConverterTypeDescriptor(base.GetTypeDescriptor(objectType, instance), converter);
        }
    }

    private sealed class ConverterTypeDescriptor(ICustomTypeDescriptor? parent, TypeConverter converter)
        : CustomTypeDescriptor(parent)
    {
        public override TypeConverter GetConverter() => converter;
    }
}
