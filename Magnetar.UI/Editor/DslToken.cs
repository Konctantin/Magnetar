using Superpower.Display;

namespace Magnetar.UI.Editor;

public enum DslToken
{
    None,
    [Token(Example = "exit")] Exit,
    [Token(Example = "#tank")] TankKeyword,
    [Token(Example = "if")] If,
    [Token(Example = "end")] End,
    [Token(Example = "ident")] Identifier,
    [Token(Example = "123")] Number,
    [Token(Example = ";")] Semicolon,
    [Token(Example = "[")] OpenBracket,
    [Token(Example = "]")] CloseBracket,
    [Token(Example = "(")] OpenParen,
    [Token(Example = ")")] CloseParen,
    [Token(Example = ".")] Dot,
    [Token(Example = ":")] Colon,
    [Token(Example = "&")] And,
    [Token(Example = "!")] Not,
    [Token(Example = "<")] LessThan,
    [Token(Example = ">=")] GreaterEqual,
    // Добавьте другие операторы по необходимости (==, >, <=)
}
