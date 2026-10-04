using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class QuestionTests
{
    private static string ValidQuestion => new('a', Question.MinLength);

    [Test]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string value = new('a', Question.MaxLength);

        Assert.That(() => new Question(value), Throws.Nothing);
    }

    [Test]
    public void Constructor_AtMinLength_Succeeds()
    {
        Assert.That(() => new Question(ValidQuestion), Throws.Nothing);
    }

    [Test]
    public void Constructor_BelowMinLength_ThrowsInvalidQuestionException()
    {
        string value = new('a', Question.MinLength - 1);

        Assert.Throws<InvalidQuestionException>(() => new Question(value));
    }

    [Test]
    public void Constructor_EmptyString_ThrowsInvalidQuestionException()
    {
        Assert.Throws<InvalidQuestionException>(() => new Question(string.Empty));
    }

    [Test]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidQuestionException()
    {
        string value = new('a', Question.MaxLength + 1);

        Assert.Throws<InvalidQuestionException>(() => new Question(value));
    }

    [Test]
    public void Constructor_WhitespaceOnly_ThrowsInvalidQuestionException()
    {
        Assert.Throws<InvalidQuestionException>(() => new Question("   "));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new Question(new string('a', Question.MinLength));
        var b = new Question(new string('b', Question.MinLength));

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new Question(ValidQuestion);
        var b = new Question(ValidQuestion);

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void ImplicitConversion_ReturnsValue()
    {
        var question = new Question(ValidQuestion);

        string result = question;

        Assert.That(result, Is.EqualTo(ValidQuestion));
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        var question = new Question(ValidQuestion);

        Assert.That(question.ToString(), Is.EqualTo(ValidQuestion));
    }
}