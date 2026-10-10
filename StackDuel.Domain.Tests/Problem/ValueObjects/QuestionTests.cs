using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class QuestionTests
{
    private static string ValidQuestion => new('a', Question.MinLength);

    [Fact]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string value = new('a', Question.MaxLength);

        Assert.Null(Record.Exception(() => new Question(value)));
    }

    [Fact]
    public void Constructor_AtMinLength_Succeeds()
    {
        Assert.Null(Record.Exception(() => new Question(ValidQuestion)));
    }

    [Fact]
    public void Constructor_BelowMinLength_ThrowsInvalidQuestionException()
    {
        string value = new('a', Question.MinLength - 1);

        Assert.Throws<InvalidQuestionException>(() => new Question(value));
    }

    [Fact]
    public void Constructor_EmptyString_ThrowsInvalidQuestionException()
    {
        Assert.Throws<InvalidQuestionException>(() => new Question(string.Empty));
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidQuestionException()
    {
        string value = new('a', Question.MaxLength + 1);

        Assert.Throws<InvalidQuestionException>(() => new Question(value));
    }

    [Fact]
    public void Constructor_WhitespaceOnly_ThrowsInvalidQuestionException()
    {
        Assert.Throws<InvalidQuestionException>(() => new Question("   "));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new Question(new string('a', Question.MinLength));
        var b = new Question(new string('b', Question.MinLength));

        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new Question(ValidQuestion);
        var b = new Question(ValidQuestion);

        Assert.Equal(b, a);
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        var question = new Question(ValidQuestion);

        string result = question;

        Assert.Equal(ValidQuestion, result);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var question = new Question(ValidQuestion);

        Assert.Equal(ValidQuestion, question.ToString());
    }
}