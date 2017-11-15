﻿using System.IO;

namespace PT.PM.Common
{
    public class SourceCodeFile
    {
        public string RootPath { get; set; } = "";

        public string RelativePath { get; set; } = "";

        public string Name { get; set; } = "";

        public string Code { get; set; } = "";

        public string RelativeName => Path.Combine(RelativePath, Name);

        public string FullName => Path.Combine(RootPath, RelativePath, Name);

        public SourceCodeFile()
        {
        }

        public LineColumnTextSpan GetLineColumnTextSpan(TextSpan textSpan)
        {
            // TODO: replace with fast binary version.
            textSpan.Start.ToLineColumn(Code, out int beginLine, out int beginColumn);
            textSpan.End.ToLineColumn(Code, out int endLine, out int endColumn);
            return new LineColumnTextSpan(beginLine, beginColumn, endLine, endColumn);
        }

        public override string ToString() => RelativeName;
    }
}
