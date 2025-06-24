using Spectre.Console.Rendering;

namespace MaxBot.TUI;

public interface ITuiCard
{
    IRenderable GetRenderable();
}
