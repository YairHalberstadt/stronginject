using FluentAssertions;
using FluentAssertions.Primitives;

namespace StrongInject.Generator.Tests.Unit
{
    public static class AssertionExtensions
    {
        //
        // Summary:
        //     Asserts that a string is exactly the same as another string, except for line endings
        //
        // Parameters:
        //   expected:
        //     The expected string.
        //
        //   because:
        //     A formatted phrase as is supported by System.String.Format(System.String,System.Object[])
        //     explaining why the assertion is needed. If the phrase does not start with the
        //     word because, it is prepended automatically.
        //
        //   becauseArgs:
        //     Zero or more objects to format using the placeholders in because.
        public static AndConstraint<StringAssertions> BeIgnoringLineEndings(this StringAssertions stringAssertions, string expected, string because = "", params object[] becauseArgs)
        {
            return stringAssertions.Subject.Replace("\r\n", "\n").Should().Be(expected.Replace("\r\n", "\n"));
        }
    }
}