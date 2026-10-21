using Magnetar.UI.Services;

namespace Magnetar.UI.Models;

public class Ability(string name)
{
    public string Name => name;

    public Func<StateService, ActionData, bool>[] Conditions { get; set; } = [];

    //public List<Ability> Children { get; set; } = [];
}
