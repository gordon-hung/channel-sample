namespace ChannelSample.AppHost;

public interface ISequenceGeneratorUtil
{
	Task<int> GetNextIdForKey(string key, CancellationToken cancellationToken);
}
