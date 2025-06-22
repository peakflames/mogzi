using System.Threading.Tasks;

namespace Cli.Commands;

public interface ICommand
{
    Task<int> ExecuteAsync();
}
