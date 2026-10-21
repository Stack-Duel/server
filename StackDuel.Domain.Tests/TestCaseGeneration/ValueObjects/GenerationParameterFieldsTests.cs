using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Domain.Tests.TestCaseGeneration.ValueObjects;

public class GenerationParameterFieldsTests
{
    private static GenerationParameterSpec Parameter(
        double? min = null,
        double? max = null,
        int? lengthMin = null,
        int? lengthMax = null,
        string? charset = null
    ) => new("p", "any", min, max, lengthMin, lengthMax, charset);

    private static ValueTypeSpec Scalar(ValueTypeKind kind) => new(kind);

    private static ValueTypeSpec ArrayOf(ValueTypeSpec element) => new(ValueTypeKind.Array, element: element);

    private static IEnumerable<string> NamesAcceptedBy(ValueTypeSpec structure) =>
        GenerationParameterFields.AcceptedBy(structure).Select(field => field.Name);

    [Fact]
    public void AcceptedBy_Number_OffersOnlyMinAndMax()
    {
        Assert.Equal(["min", "max"], NamesAcceptedBy(Scalar(ValueTypeKind.Integer)));
    }

    [Fact]
    public void AcceptedBy_String_OffersLengthAndCharsetButNoMinMax()
    {
        Assert.Equal(["lengthMin", "lengthMax", "charset"], NamesAcceptedBy(Scalar(ValueTypeKind.String)));
    }

    [Fact]
    public void AcceptedBy_Boolean_OffersNothing()
    {
        Assert.Empty(NamesAcceptedBy(Scalar(ValueTypeKind.Boolean)));
    }

    [Fact]
    public void AcceptedBy_NumberArray_OffersLengthForItselfAndMinMaxForItsElements()
    {
        Assert.Equal(
            ["min", "max", "lengthMin", "lengthMax"],
            NamesAcceptedBy(ArrayOf(Scalar(ValueTypeKind.Integer)))
        );
    }

    [Fact]
    public void AcceptedBy_StringArray_OffersLengthAndCharsetButNoMinMax()
    {
        Assert.Equal(
            ["lengthMin", "lengthMax", "charset"],
            NamesAcceptedBy(ArrayOf(Scalar(ValueTypeKind.String)))
        );
    }

    [Fact]
    public void AcceptedBy_BooleanArray_OffersOnlyLength()
    {
        Assert.Equal(["lengthMin", "lengthMax"], NamesAcceptedBy(ArrayOf(Scalar(ValueTypeKind.Boolean))));
    }

    [Fact]
    public void AcceptedBy_Matrix_StillOffersMinMaxForItsInnermostNumbers()
    {
        Assert.Equal(
            ["min", "max", "lengthMin", "lengthMax"],
            NamesAcceptedBy(ArrayOf(ArrayOf(Scalar(ValueTypeKind.Integer))))
        );
    }

    [Fact]
    public void AcceptedBy_CarriesTheValueKindOfEachField()
    {
        IReadOnlyList<ValueTypeFieldDescriptor> fields = GenerationParameterFields.AcceptedBy(
            Scalar(ValueTypeKind.String)
        );

        Assert.Equal(ValueTypeFieldValueKind.Integer, fields.Single(f => f.Name == "lengthMin").ValueKind);
        Assert.Equal(ValueTypeFieldValueKind.Text, fields.Single(f => f.Name == "charset").ValueKind);
    }

    [Fact]
    public void UnsupportedOverrides_LengthOnANumber_IsRejected()
    {
        IReadOnlyList<string> unsupported = GenerationParameterFields.UnsupportedOverrides(
            Parameter(lengthMin: 1, lengthMax: 5),
            Scalar(ValueTypeKind.Integer)
        );

        Assert.Equal(["lengthMin", "lengthMax"], unsupported);
    }

    [Fact]
    public void UnsupportedOverrides_CharsetOnANumber_IsRejected()
    {
        Assert.Equal(
            ["charset"],
            GenerationParameterFields.UnsupportedOverrides(Parameter(charset: "ab"), Scalar(ValueTypeKind.Integer))
        );
    }

    [Fact]
    public void UnsupportedOverrides_MinMaxOnAString_IsRejected()
    {
        Assert.Equal(
            ["min", "max"],
            GenerationParameterFields.UnsupportedOverrides(
                Parameter(min: 1, max: 2),
                Scalar(ValueTypeKind.String)
            )
        );
    }

    [Fact]
    public void UnsupportedOverrides_EverythingOnABoolean_IsRejected()
    {
        Assert.Equal(
            ["min", "max", "lengthMin", "lengthMax", "charset"],
            GenerationParameterFields.UnsupportedOverrides(
                Parameter(min: 1, max: 2, lengthMin: 3, lengthMax: 4, charset: "ab"),
                Scalar(ValueTypeKind.Boolean)
            )
        );
    }

    [Fact]
    public void UnsupportedOverrides_MinMaxAndLengthOnANumberArray_AreBothAccepted()
    {
        Assert.Empty(
            GenerationParameterFields.UnsupportedOverrides(
                Parameter(min: 0, max: 9, lengthMin: 1, lengthMax: 4),
                ArrayOf(Scalar(ValueTypeKind.Integer))
            )
        );
    }

    [Fact]
    public void UnsupportedOverrides_CharsetOnANumberArray_IsRejected()
    {
        Assert.Equal(
            ["charset"],
            GenerationParameterFields.UnsupportedOverrides(
                Parameter(charset: "ab"),
                ArrayOf(Scalar(ValueTypeKind.Integer))
            )
        );
    }

    [Fact]
    public void UnsupportedOverrides_NothingSupplied_IsAlwaysAccepted()
    {
        Assert.Empty(
            GenerationParameterFields.UnsupportedOverrides(Parameter(), Scalar(ValueTypeKind.Boolean))
        );
    }

    [Fact]
    public void AcceptedFields_MatchWhatIsNotRejected()
    {
        ValueTypeSpec structure = ArrayOf(Scalar(ValueTypeKind.String));
        GenerationParameterSpec everything = Parameter(
            min: 1,
            max: 2,
            lengthMin: 3,
            lengthMax: 4,
            charset: "ab"
        );

        IEnumerable<string> accepted = NamesAcceptedBy(structure);
        IReadOnlyList<string> rejected = GenerationParameterFields.UnsupportedOverrides(everything, structure);

        Assert.Empty(accepted.Intersect(rejected));
    }
}