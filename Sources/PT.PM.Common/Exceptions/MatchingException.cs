﻿using System;

namespace PT.PM.Common.Exceptions
{
    public class MatchingException : PMException
    {
        public override PMExceptionType ExceptionType => PMExceptionType.Matching;

        public TextSpan TextSpan { get; set; }

        public MatchingException()
        {
        }

        public MatchingException(string fileName, Exception ex = null, string message = "", bool isPattern = false)
            : base(ex, message, isPattern)
        {
            FileName = fileName;
        }
    }
}
