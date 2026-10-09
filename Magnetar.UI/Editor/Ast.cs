namespace Magnetar.UI.Editor;

// Хранитель позиций для подсветки ошибок и автокомплита
public record TextSpan(int Position, int Length);

public abstract record AstNode(TextSpan? Span = null);

// Корневой узел программы
public record ProfileProgram(List<StatementNode> Statements, TextSpan? Span = null)
    : AstNode(Span);

// Базовый класс для инструкций
public abstract record StatementNode(TextSpan? Span = null)
    : AstNode(Span);

// Инструкции (Statements)
public record ExitStatement(ExpressionNode? Condition, TextSpan? Span = null)
    : StatementNode(Span);

public record TankStatement(ExpressionNode Condition, TextSpan? Span = null)
    : StatementNode(Span);

public record ActionStatement(
    string Type, string TargetName, string? TargetId,
    ExpressionNode? Condition, TextSpan? Span = null)
    : StatementNode(Span);

public record IfStatement(ExpressionNode Condition,
    List<StatementNode> Body,
    TextSpan? Span = null)
    : StatementNode(Span);

// Выражения (Expressions / Условия)
public abstract record ExpressionNode(TextSpan? Span = null)
    : AstNode(Span);

public record BinaryExpression(ExpressionNode Left,
    string Operator,
    ExpressionNode Right,
    TextSpan? Span = null)
    : ExpressionNode(Span);

public record UnaryExpression(string Operator, ExpressionNode Inner, TextSpan? Span = null)
    : ExpressionNode(Span);

public record IdentifierExpression(string Name, TextSpan? Span = null)
    : ExpressionNode(Span);

public record NumberExpression(int Value, TextSpan? Span = null)
    : ExpressionNode(Span);

// Для конструкций вида target.debuff(Name).remains или target.debuff(Name)
public record MemberAccessExpression(ExpressionNode Expression,
    string MemberName,
    List<ExpressionNode>? Arguments = null,
    TextSpan? Span = null)
    : ExpressionNode(Span);

