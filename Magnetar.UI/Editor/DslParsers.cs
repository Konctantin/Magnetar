using Superpower;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace Magnetar.UI.Editor;

public static class DslParsers
{
    // Лексер (разбиение строки на токены)
    public static Tokenizer<DslToken> Tokenizer => new TokenizerBuilder<DslToken>()
        .Ignore(Span.WhiteSpace)
        .Match(Span.EqualTo("exit"), DslToken.Exit)
        .Match(Span.EqualTo("#tank"), DslToken.TankKeyword)
        .Match(Span.EqualTo("if"), DslToken.If)
        .Match(Span.EqualTo("end"), DslToken.End)
        .Match(Character.EqualTo(';'), DslToken.Semicolon)
        .Match(Character.EqualTo('['), DslToken.OpenBracket)
        .Match(Character.EqualTo(']'), DslToken.CloseBracket)
        .Match(Character.EqualTo('('), DslToken.OpenParen)
        .Match(Character.EqualTo(')'), DslToken.CloseParen)
        .Match(Character.EqualTo('.'), DslToken.Dot)
        .Match(Character.EqualTo(':'), DslToken.Colon)
        .Match(Character.EqualTo('&'), DslToken.And)
        .Match(Character.EqualTo('!'), DslToken.Not)
        .Match(Span.EqualTo(">="), DslToken.GreaterEqual)
        .Match(Character.EqualTo('<'), DslToken.LessThan)
        .Match(Numerics.IntegerInt32, DslToken.Number)
        // Идентификатор захватывает слова и строки внутри скобок для простоты
        .Match(Span.Regex(@"[a-zA-Z_][a-zA-Z0-9_ ]*"), DslToken.Identifier)
        .Build();

    // Парсер выражений (Условий)
    private static TokenListParser<DslToken, ExpressionNode> Number =>
        Token.EqualTo(DslToken.Number).Select(t => (ExpressionNode)new NumberExpression(int.Parse(t.ToStringValue())));

    private static TokenListParser<DslToken, ExpressionNode> Identifier =>
        Token.EqualTo(DslToken.Identifier).Select(t => (ExpressionNode)new IdentifierExpression(t.ToStringValue().Trim()));

    // Рекурсивный парсер для вызовов функций вроде target.debuff(Some Name).remains
    private static TokenListParser<DslToken, ExpressionNode> Term =>
        MethodCallOrMember.Or(Identifier).Or(Number);

    private static TokenListParser<DslToken, ExpressionNode> MethodCallOrMember =>
        Parse.Ref(() => Identifier).Then(baseExpr =>
            Parse.Ref(() => MemberAccessTail(baseExpr)).Many().Select(tails => {
                var current = baseExpr;
                foreach (var tail in tails) current = tail(current);
                return current;
            }));

    private static TokenListParser<DslToken, Func<ExpressionNode, ExpressionNode>> MemberAccessTail(ExpressionNode expr) =>
        Token.EqualTo(DslToken.Dot)
            .Then(_ => Token.EqualTo(DslToken.Identifier))
            .Then(name =>
                Token.EqualTo(DslToken.OpenParen)
                    .Then(_ => Token.EqualTo(DslToken.Identifier)
                        .ManyDelimitedBy(Token.EqualTo(DslToken.Colon)
                        )
                    ) // упрощено
            .Then(args => Token.EqualTo(DslToken.CloseParen)
                .Select(_ => new Func<ExpressionNode, ExpressionNode>(
                    e => new MemberAccessExpression(e, name.ToStringValue(), null))
                )
            ));
            //.Or(Parse.Return(
            //    new Func<ExpressionNode, ExpressionNode>(e => new MemberAccessExpression(e, name.ToStringValue(), null))))
            //);

    // Операторы сравнения и логики
    private static TokenListParser<DslToken, ExpressionNode> Comparison =>
        Parse.Chain(Token.EqualTo(DslToken.LessThan).Or(Token.EqualTo(DslToken.GreaterEqual)), Term,
            (op, left, right) => new BinaryExpression(left, op.ToStringValue(), right));

    public static TokenListParser<DslToken, ExpressionNode> Condition =>
        Parse.Chain(Token.EqualTo(DslToken.And), Comparison.Or(Term),
            (op, left, right) => new BinaryExpression(left, op.ToStringValue(), right));

    // Парсеры инструкций (Statements)
    //public static TokenListParser<DslToken, StatementNode> ExitStatement =>
    //    Token.EqualTo(DslToken.Exit)
    //        .Then(_ => Condition
    //            .Between(Token.EqualTo(DslToken.OpenBracket), Token.EqualTo(DslToken.CloseBracket))
    //            .Optional())
    //        .Before(Token.EqualTo(DslToken.Semicolon))
    //        .Select(cond => (StatementNode)new ExitStatement(cond.HasValue ? cond.Value : null));

    // Корневой парсер программы
    //public static TokenListParser<DslToken, ProfileProgram> Program =>
    //    ExitStatement.Or(Parse.Zero<DslToken, StatementNode>()) // расширить для остальных стейтментов
    //    .Many()
    //    .Select(st => new ProfileProgram([.. st]));
}
