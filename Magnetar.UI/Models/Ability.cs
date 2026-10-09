namespace Magnetar.UI.Models;

public class Ability
{
    public List<Condition> Conditions { get; set; } = [];

    public List<Ability> Children { get; set; } = [];
}
