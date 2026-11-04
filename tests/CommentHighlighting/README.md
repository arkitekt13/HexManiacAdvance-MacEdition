# Comment highlighting equivalence

Run `dotnet run --project tests/CommentHighlighting` or the complete Mac suite.

The group passes 4,652 assertions over 116 deterministic source documents, ten delimiter
configurations and both syntax modes. An independent character-array/ordinal-string oracle
reproduces the original restart-at-zero comment classification. The unchanged tokenizer colors
the remaining source, allowing comparison of all six production layers, exact source and indices.
Fixtures include LF/CRLF/CR, tabs, Unicode, quoted markers, EOF/unclosed/nested/adjacent comments,
empty delimiters, line-before-block precedence and space-containing markers that can reappear
earlier after text is blanked. The latter retain the original full-rescan behavior.

Six additional preformatter cases compare complete frozen layer strings captured before the
optimization at 769825a9, including existing keyword/constant/text/comment formatting inside
comments and retained diagnostics. PreformatterBaseline.json is a before-change golden fixture;
do not regenerate it from the optimized implementation as a substitute for equivalence testing.

No elapsed-time threshold gates these checks. The optimization changes only comment-search
positions; compilation, number parsing, keyword matching, content/caret, history and ROMs are
outside this code change. See [COMMENT_HIGHLIGHT_PERFORMANCE.md](../COMMENT_HIGHLIGHT_PERFORMANCE.md)
for the isolated scan and whole-highlighter measurements.
