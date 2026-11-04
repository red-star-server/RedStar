using Robust.Client.UserInterface.RichText;

namespace Content.Client._Trauma.UserInterface.RichText;

public sealed partial class ExamineBorderTag : IMarkupTagHandler
{
    public const string TagName = "examineborder";

    public string Name => TagName;
}
