using Discord.Commands;
using System.Threading.Tasks;

// Keep in mind your module **must** be public and inherit ModuleBase.
// If it isn't, it will not be discovered by AddModulesAsync!
public class InfoModule : ModuleBase<SocketCommandContext>
{
	[Command("burza")]
	[Summary("Psiakrew")]
	public Task Psiakrew() {
		ReplyAsync("Psiakrew");
		return Task.CompletedTask;
	}
}