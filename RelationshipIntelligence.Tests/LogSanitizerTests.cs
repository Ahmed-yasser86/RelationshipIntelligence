using FluentAssertions;
using Servicess.Helpers;

namespace CRUDTests
{
    public class LogSanitizerTests
    {
        [Fact]
        public void Sanitize_Null_ReturnsEmpty()
            => LogSanitizer.Sanitize(null).Should().BeEmpty();

        [Fact]
        public void Sanitize_RemovesNewlinesAndControlCharacters()
            => LogSanitizer.Sanitize("hello\r\nfake log line\tend")
                .Should().Be("hello fake log line end");

        [Fact]
        public void Sanitize_TruncatesLongInput()
            => LogSanitizer.Sanitize(new string('x', 500)).Should().HaveLength(LogSanitizer.MaxLength);

        [Fact]
        public void Sanitize_ConvertsNonStringValues()
            => LogSanitizer.Sanitize(42).Should().Be("42");
    }
}
