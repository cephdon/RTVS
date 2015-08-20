﻿using Microsoft.Languages.Core.Text;
using Microsoft.Languages.Core.Tokens;
using Microsoft.R.Core.Tokens;

namespace Microsoft.R.Support.RD.Tokens
{
    /// <summary>
    /// Main R tokenizer. Used for colirization and parsing. 
    /// Coloring of variables, function names and parameters
    /// is provided later by AST. Tokenizer only provides candidates.
    /// https://developer.r-project.org/parseRd.pdf
    /// </summary>
    internal class RdTokenizer : BaseTokenizer<RdToken>
    {
        /// <summary>
        /// Main tokenization method. Responsible for adding next token
        /// to the list, if any. Returns if it is at the end of the 
        /// character stream. It is up to base class to terminate tokenization.
        /// </summary>
        public override void AddNextToken()
        {
            SkipWhitespace();

            if (_cs.IsEndOfStream())
                return;

            HandleLatexContent(block: false);
        }

        private void HandleLatexContent(bool block)
        {
            RdBraceCounter<char> braceCounter = block ? new RdBraceCounter<char>('{', '}', '[', ']') : null;

            while (!_cs.IsEndOfStream())
            {
                bool handled = false;

                // Regular content is Latex-like
                switch (_cs.CurrentChar)
                {
                    case '%':
                        // RD Comments are from # to the end of the line
                        handled = HandleComment();
                        break;

                    case '\\':
                        if (IsEscape())
                        {
                            _cs.Advance(2);
                            handled = true;
                        }
                        else
                        {
                            handled = HandleKeyword();
                        }
                        break;

                    case '#':
                        handled = HandlePragma();
                        break;

                    default:
                        if (braceCounter != null && braceCounter.CountBrace(_cs.CurrentChar))
                        {
                            handled = AddBraceToken();

                            if (braceCounter.Count == 0)
                                return;
                        }
                        break;
                }

                if (!handled)
                {
                    _cs.MoveToNextChar();
                }
            }
        }

        private bool HandleKeyword()
        {
            int start = _cs.Position;

            if (MoveToKeywordEnd())
            {
                AddToken(RdTokenType.Keyword, start, _cs.Position - start);
                SkipWhitespace();

                if (_cs.CurrentChar == '{' || _cs.CurrentChar == '[')
                {
                    string keyword = _cs.Text.GetText(TextRange.FromBounds(start, _cs.Position)).Trim();
                    BlockContentType contentType = RdBlockContentType.GetBlockContentType(keyword);

                    // Handle argument sequence like \latex[0]{foo} or \item{}{}
                    while (_cs.CurrentChar == '{' || _cs.CurrentChar == '[')
                    {
                        HandleKeywordArguments(contentType);
                    }
                    return true;
                }
            }

            return false;
        }

        private bool MoveToKeywordEnd()
        {
            int start = _cs.Position;

            _cs.MoveToNextChar();
            SkipKeyword();

            if (_cs.Position - start > 1)
            {
                return true;
            }

            _cs.Position = start;
            return false;
        }

        private void HandleKeywordArguments(BlockContentType contentType)
        {
            char closeBrace = GetMatchingBrace(_cs.CurrentChar);

            // Content type table can be found in 
            // https://developer.r-project.org/parseRd.pdf

            switch (contentType)
            {
                case BlockContentType.R:
                    HandleRContent(closeBrace);
                    break;

                case BlockContentType.Verbatim:
                    HandleVerbatimContent();
                    break;

                default:
                    HandleLatexContent(block: true);
                    break;
            }
        }

        /// <summary>
        /// Handles R-like content in RD. This includes handling # and ##
        /// as comments, counting brace nesting, handling "..." as string
        /// (not true in plain RD LaTeX-like content) and colorizing numbers
        /// by using actual R tokenizer.
        /// </summary>
        /// <param name="closeBrace"></param>
        private void HandleRContent(char closeBrace)
        {
            RdBraceCounter<char> braceCounter = new RdBraceCounter<char>('{', '}', '[', ']');
            int start = _cs.Position;

            while (!_cs.IsEndOfStream())
            {
                bool handled = false;
                switch (_cs.CurrentChar)
                {
                    case '\"':
                    case '\'':
                        handled = HandleString(_cs.CurrentChar);
                        break;

                    case '\\':
                        handled = IsEscape();
                        if (handled)
                        {
                            _cs.Advance(2);
                        }
                        else
                        {
                            handled = HandleKeyword();
                        }
                        break;

                    case '#':
                        handled = HandlePragma();
                        if (!handled)
                        {
                            if (_cs.NextChar == '#')
                            {
                                // ## is always comment in R-like content
                                handled = HandleComment();
                            }
                            else
                            {
                                // With a sinle # it may or may not be comment.
                                // For example, there are statements like \code{#}.
                                // Heuristic is to treat text that contains {} or \
                                // as NOT a comment.
                                int commentStart = _cs.Position;
                                SkipToEol();

                                string commentText = _cs.Text.GetText(TextRange.FromBounds(commentStart, _cs.Position));
                                _cs.Position = commentStart;

                                if (commentText.IndexOfAny(new char[] { '{', '\\', '}' }) < 0)
                                {
                                    handled = HandleComment();
                                }
                            }
                        }
                        break;

                    default:
                        if (braceCounter.CountBrace(_cs.CurrentChar))
                        {
                            handled = AddBraceToken();

                            if (braceCounter.Count == 0)
                                return;
                        }
                        else
                        {
                            // Check if sequence is a candidate for a number.
                            // The check is not perfect but numbers in R-like content
                            // are typically very simple as R blocks are usually
                            // code examples and do not contain exotic sequences.

                            if ((_cs.IsDecimal() || _cs.CurrentChar == '-' || _cs.CurrentChar == '.'))
                            {
                                int sequenceStart = _cs.Position;
                                SkipToWhitespace();

                                if (_cs.Position > sequenceStart)
                                {
                                    RTokenizer rt = new RTokenizer();

                                    string candidate = _cs.Text.GetText(TextRange.FromBounds(sequenceStart, _cs.Position));
                                    var rTokens = rt.Tokenize(candidate);

                                    if (rTokens.Count > 0 && rTokens[0].TokenType == RTokenType.Number)
                                    {
                                        AddToken(RdTokenType.Number, sequenceStart + rTokens[0].Start, rTokens[0].Length);

                                        _cs.Position = sequenceStart + rTokens[0].End;
                                        continue;
                                    }
                                }

                                _cs.Position = sequenceStart;
                            }
                        }
                        break;
                }

                if (!handled)
                {
                    _cs.MoveToNextChar();
                }
            }
        }

        /// <summary>
        /// Handles verbatim text content where there are no 
        /// special characters apart from braces and pragmas. 
        /// It does count brace nesting though so it can properly
        /// determine where the verbatim content ends.
        /// </summary>
        private void HandleVerbatimContent()
        {
            RdBraceCounter<char> braceCounter = new RdBraceCounter<char>('{', '}', '[', ']');

            while (!_cs.IsEndOfStream())
            {
                if (braceCounter.CountBrace(_cs.CurrentChar))
                {
                    if (braceCounter.Count == 0)
                    {
                        AddBraceToken();
                        break;
                    }
                }
                else if (_cs.CurrentChar == '#' && HandlePragma())
                {
                    continue;
                }

                _cs.MoveToNextChar();
            }
        }

        /// <summary>
        /// Handles RD conditional pragmas (C-like).
        /// </summary>
        /// <returns></returns>
        private bool HandlePragma()
        {
            if (_cs.Position == 0 || _cs.PrevChar == '\r' || _cs.PrevChar == '\n')
            {
                int start = _cs.Position;
                SkipUnknown();

                int length = _cs.Position - start;
                if (length > 1)
                {
                    string pragma = _cs.Text.GetText(TextRange.FromBounds(start, _cs.Position)).Trim();
                    if (pragma == "#ifdef" || pragma == "#ifndef" || pragma == "#endif")
                    {
                        AddToken(RdTokenType.Pragma, start, length);
                        return true;
                    }
                }

                _cs.Position = start;
            }

            return false;
        }

        private char GetMatchingBrace(char brace)
        {
            if (brace == '{')
                return '}';

            if (brace == '[')
                return ']';

            return char.MinValue;
        }

        private bool AddBraceToken()
        {
            RdTokenType tokenType = RdTokenType.Unknown;

            switch (_cs.CurrentChar)
            {
                case '{':
                    tokenType = RdTokenType.OpenCurlyBrace;
                    break;

                case '[':
                    tokenType = RdTokenType.OpenSquareBracket;
                    break;

                case '}':
                    tokenType = RdTokenType.CloseCurlyBrace;
                    break;

                case ']':
                    tokenType = RdTokenType.CloseSquareBracket;
                    break;
            }

            if (tokenType != RdTokenType.Unknown)
            {
                AddToken(tokenType, _cs.Position, 1);
                _cs.MoveToNextChar();
                return true;

            }

            return false;
        }

        private bool IsEscape()
        {
            return _cs.NextChar == '%' || _cs.NextChar == '\\' || _cs.NextChar == '{' || _cs.NextChar == '}' || _cs.NextChar == 'R';
        }

        /// <summary>
        /// Handle RD comment. Comment starts with %
        /// and goes to the end of the line.
        /// </summary>
        private bool HandleComment()
        {
            Tokenizer.HandleEolComment(_cs, (start, length) => AddToken(RdTokenType.Comment, start, length));
            return true;
        }

        /// <summary>
        /// Adds a token that represent a string
        /// </summary>
        /// <param name="openQuote"></param>
        private bool HandleString(char openQuote)
        {
            Tokenizer.HandleString(openQuote, _cs, (start, length) => AddToken(RdTokenType.String, start, length));
            return true;
        }

        private void AddToken(RdTokenType type, int start, int length)
        {
            var token = new RdToken(type, new TextRange(start, length));
            _tokens.Add(token);
        }

        internal void SkipKeyword()
        {
            Tokenizer.SkipIdentifier(
                _cs,
                (CharacterStream cs) => { return _cs.IsAnsiLetter(); },
                (CharacterStream cs) => { return (_cs.IsAnsiLetter() || _cs.IsDecimal()); });
        }

        /// <summary>
        /// Skips content until the nearest whitespace
        /// or a RD comment that starts with %.
        /// </summary>
        internal void SkipUnknown()
        {
            while (!_cs.IsEndOfStream() && !_cs.IsWhiteSpace())
            {
                if (_cs.CurrentChar == '%')
                    break;

                _cs.MoveToNextChar();
            }
        }

        /// <summary>
        /// Skips content until the nearest whitespace
        /// </summary>
        internal void SkipToWhitespace()
        {
            while (!_cs.IsEndOfStream() && !_cs.IsWhiteSpace())
            {
                _cs.MoveToNextChar();
            }
        }

        /// <summary>
        /// Skips everything until the end of the line
        /// </summary>
        internal void SkipToEol()
        {
            while (!_cs.IsEndOfStream() && !_cs.IsAtNewLine())
            {
                _cs.MoveToNextChar();
            }
        }

        private bool IsIdentifierCharacter()
        {
            return (_cs.IsAnsiLetter() || _cs.IsDecimal());
        }
    }
}
