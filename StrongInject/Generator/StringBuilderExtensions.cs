using System;
using System.Collections.Generic;
using System.Text;

namespace StrongInject.Generator
{
    internal static class StringBuilderExtensions
    {
        public static RevertableSection BeginRevertableSection(this StringBuilder stringBuilder) => new RevertableSection(stringBuilder);

        public struct RevertableSection
        {
            private readonly StringBuilder _stringBuilder;
            private readonly int _index;
            private int _length;
            public RevertableSection(StringBuilder stringBuilder)
            {
                _stringBuilder = stringBuilder;
                _index = stringBuilder.Length;
                _length = 0;
            }

            public void EndSection()
            {
                _length = _stringBuilder.Length - _index;
            }

            public void Revert()
            {
                _stringBuilder.Remove(_index, _length);
            }
        }
    }
}
