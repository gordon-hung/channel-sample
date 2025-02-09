namespace ChannelSample.AppHost.Utils;

internal class SequenceGeneratorUtil : ISequenceGeneratorUtil
{
	private static readonly Dictionary<string, int> _KeyToSequence = [];
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public async Task<int> GetNextIdForKey(string key, CancellationToken cancellationToken)
	{
		await _semaphore.WaitAsync(
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		try
		{
			return GetNextIdForKey(key);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	private static int GetNextIdForKey(string key)
	{
		if (!_KeyToSequence.ContainsKey(key))
			_KeyToSequence[key] = 0;  // 初始化該key的編號

		// 對應key的編號加1並返回
		_KeyToSequence[key]++;
		return _KeyToSequence[key];
	}
}
